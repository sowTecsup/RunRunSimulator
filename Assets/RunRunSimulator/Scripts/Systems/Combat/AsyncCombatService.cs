using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using UnityEngine;
namespace MoriMonchiSimulator
{

// Orchestrates async combat: enqueues creatures via Cloud Code, polls Cloud Save for results.
// Cloud Code scripts no longer simulate combat — they only match players and
// write a match blob (seed + both DNA snapshots) to combat_results. Each client
// runs the same deterministic CombatService.SimulateCore with that seed, so both
// sides land on an identical result without trusting server-side formulas, then
// applies the consequences to the live DNA in the registry.
// Cloud Code scripts:
//   - "enqueue-combat"      → called from here, adds creature to the global pool
//   - "process-matchmaking" → scheduled trigger in Dashboard, pairs the pool and
//                              writes the seeded match blob to both sides
// Attach to same GameObject as GameManager and CombatController.
// Resolves its registry from GameManager.Instance in Awake — no serialized
// cross-references needed.
public class AsyncCombatService : MonoBehaviour
{
    private const string RESULTS_KEY            = "combat_results";
    private const string CLOUD_CODE_INSTANT     = "run-combat";        // immediate match if pool has someone
    private const string CLOUD_CODE_SCHEDULED   = "enqueue-combat";    // always waits for the next tick
    private const string CLOUD_CODE_QUEUE_STATUS = "get-queue-status"; // which of our creatures are really in the pool
    private const float  MIN_QUEUE_DELAY_SEC    = 5f;

    // ── Private Fields ────────────────────────────────────────────

    // Creatures mid-enqueue right now (the 5s window before the Cloud Code call
    // lands them in the pool). They are QueuedForCombat locally but legitimately
    // absent from the server pool, so ghost-reconciliation must skip them.
    private readonly HashSet<string> inFlightEnqueues = new HashSet<string>();

    // Serializes the cloud-mutating section of every enqueue. Both the matchmaking
    // pool and each player's combat_results are read-modify-write on Cloud Save with
    // no atomicity, so two enqueues firing at once (e.g. two Instant battles back to
    // back) would double-match the same opponent and lose-update one result. The
    // gate forces this client's enqueues through one at a time → one combat at a time.
    private readonly SemaphoreSlim enqueueGate = new SemaphoreSlim(1, 1);

    // ── Cached References ─────────────────────────────────────────

    private CreatureRegistrySO registry;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake() => registry = GameManager.Instance.Registry;

    // ── Private Fields ────────────────────────────────────────────

    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private string status = "Idle";

    // ── Public Methods ────────────────────────────────────────────

    // Instant flow — calls run-combat. If another player is already in the pool,
    // matches and simulates immediately. Otherwise leaves the creature waiting
    // until another player calls run-combat too.
    public async Task EnqueueInstantAsync(CreatureDNA dna) =>
        await EnqueueInternal(dna, CLOUD_CODE_INSTANT, isScheduled: false);

    // Scheduled flow — calls enqueue-combat which just appends to the pool.
    // The actual matching happens server-side on the cron-scheduled
    // process-matchmaking trigger (configured in Unity Dashboard).
    public async Task EnqueueScheduledAsync(CreatureDNA dna) =>
        await EnqueueInternal(dna, CLOUD_CODE_SCHEDULED, isScheduled: true);

    private async Task EnqueueInternal(CreatureDNA dna, string endpoint, bool isScheduled)
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogError("[AsyncCombat] Not signed in — cannot enqueue.");
            return;
        }

        dna.BusyState = BusyReason.QueuedForCombat;
        dna.QueuedAt  = DateTime.UtcNow;
        var cfg = CombatController.Instance != null ? CombatController.Instance.Config : null;
        if (cfg != null)
            dna.Needs.SpendEnergy(cfg.EnergyCostToQueue);
        inFlightEnqueues.Add(dna.UniqueID);
        GameEvents.RegistryChanged(registry);

        status = $"\"{dna.CustomName}\" — waiting {MIN_QUEUE_DELAY_SEC}s before matchmaking ({endpoint})...";
        Debug.Log($"[AsyncCombat] {status}");

        await Task.Delay(TimeSpan.FromSeconds(MIN_QUEUE_DELAY_SEC));

        // One cloud op at a time: a second enqueue waits here until this one has
        // matched, written both players' results, and (Instant) polled them back.
        await enqueueGate.WaitAsync();
        try
        {
            status = $"\"{dna.CustomName}\" — calling {endpoint}...";

            string playerName = "Anonymous";
            try { playerName = await AuthenticationService.Instance.GetPlayerNameAsync() ?? "Anonymous"; }
            catch { /* keep default */ }

            var payload = new Dictionary<string, object>
            {
                { "creatureId",   dna.UniqueID },
                { "customName",   dna.CustomName },
                { "creatureJson", SaveSystem.Serialize(dna) },
                { "playerName",   playerName },
            };

            var response = await CloudEndpoint.CallAsync<CloudMatchResponse>(endpoint, payload);

            switch (response.Status)
            {
                case "queued":
                    status = $"\"{dna.CustomName}\" queued — pool size {response.PoolSize}. Waiting for next matchmaking tick.";
                    Debug.Log($"[AsyncCombat] {status}");
                    break;

                case "already_queued":
                    status = $"\"{dna.CustomName}\" already in queue.";
                    Debug.Log($"[AsyncCombat] {status}");
                    break;

                case "waiting":
                    status = $"\"{dna.CustomName}\" is waiting for an opponent (instant mode).";
                    Debug.Log($"[AsyncCombat] {status}");
                    break;

                case "matched":
                    status = $"\"{dna.CustomName}\" was matched! Applying result...";
                    Debug.Log($"[AsyncCombat] {status}");
                    await PollResultsAsync();
                    break;

                default:
                    status = $"Unexpected response status: {response.Status}";
                    Debug.LogWarning($"[AsyncCombat] {status}");
                    break;
            }

            // Persist BusyState to Cloud Save so it survives logout/login
            GameEvents.RegistryChanged(registry);
        }
        catch (Exception e)
        {
            // Rollback busy state so the creature is usable again
            dna.BusyState = BusyReason.None;
            GameEvents.RegistryChanged(registry);

            status = $"Enqueue error: {e.Message}";
            Debug.LogError($"[AsyncCombat] EnqueueInternal failed: {e}");
        }
        finally
        {
            inFlightEnqueues.Remove(dna.UniqueID);
            enqueueGate.Release();
        }
    }

    // Pulls combat_results and applies them, THEN reconciles ghost queue states.
    // Both steps run even if there are no results — a ghost has no result, so the
    // reconcile is exactly what heals it (no Dequeue needed anymore).
    public async Task PollResultsAsync()
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogError("[AsyncCombat] Not signed in — cannot poll results.");
            return;
        }

        try
        {
            status = "Checking results...";

            int applied = await ApplyPendingResultsAsync();
            int ghosts  = await ReconcileGhostsAsync();

            status =
                applied == 0 && ghosts == 0 ? "No pending results." :
                $"Applied {applied} result(s)" + (ghosts > 0 ? $", cleared {ghosts} ghost(s)." : ".");
            Debug.Log($"[AsyncCombat] {status}");
        }
        catch (Exception e)
        {
            status = $"Poll error: {e.Message}";
            Debug.LogError($"[AsyncCombat] PollResultsAsync failed: {e}");
        }
    }

    // ── Public Query Helpers (for the queue UI) ───────────────────

    // Which of OUR creatures the server pool actually holds right now. Returns
    // null if the server couldn't be reached — callers must treat null as
    // "unknown" and NOT reconcile (otherwise a network blip wipes real queues).
    public async Task<HashSet<string>> FetchQueuedIdsAsync()
    {
        if (!AuthenticationService.Instance.IsSignedIn) return new HashSet<string>();
        try
        {
            var resp = await CloudEndpoint.CallAsync<CloudQueueStatusResponse>(
                CLOUD_CODE_QUEUE_STATUS, new Dictionary<string, object>());

            // ok=false means a pool read failed server-side → we only have a partial
            // view. Treat exactly like unreachable (null) so reconcile is skipped and
            // legitimately-queued creatures aren't wrongly cleared.
            if (resp == null || !resp.Ok) return null;
            return new HashSet<string>(resp.InPool ?? new List<string>());
        }
        catch (Exception e)
        {
            Debug.LogError($"[AsyncCombat] FetchQueuedIdsAsync failed: {e}");
            return null;
        }
    }

    // Creature ids that currently have an unapplied combat result waiting.
    public async Task<HashSet<string>> FetchPendingResultIdsAsync()
    {
        if (!AuthenticationService.Instance.IsSignedIn) return new HashSet<string>();
        try
        {
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { RESULTS_KEY });

            if (!data.ContainsKey(RESULTS_KEY)) return new HashSet<string>();

            var results = JsonConvert.DeserializeObject<List<CloudMatchBlob>>(
                data[RESULTS_KEY].Value.GetAs<string>());

            return new HashSet<string>(
                (results ?? new List<CloudMatchBlob>()).ConvertAll(r => r.CreatureId));
        }
        catch (Exception e)
        {
            Debug.LogError($"[AsyncCombat] FetchPendingResultIdsAsync failed: {e}");
            return new HashSet<string>();
        }
    }

    // ── Private Methods ───────────────────────────────────────────

    // Applies (and clears) every pending combat_result. Returns how many applied.
    private async Task<int> ApplyPendingResultsAsync()
    {
        var data = await CloudSaveService.Instance.Data.Player.LoadAsync(
            new HashSet<string> { RESULTS_KEY });

        if (!data.ContainsKey(RESULTS_KEY)) return 0;

        var results = JsonConvert.DeserializeObject<List<CloudMatchBlob>>(
            data[RESULTS_KEY].Value.GetAs<string>());

        if (results == null || results.Count == 0) return 0;

        string myPlayerName = "Anonymous";
        try { myPlayerName = await AuthenticationService.Instance.GetPlayerNameAsync() ?? "Anonymous"; }
        catch { }

        int applied = 0;
        foreach (var r in results)
            if (ApplyResult(r, myPlayerName)) applied++;

        // Clear the results key so they're not applied twice
        await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object>
        {
            { RESULTS_KEY, "[]" }
        });

        GameEvents.RegistryChanged(registry);
        return applied;
    }

    // Clears "ghost" QueuedForCombat states: creatures the local registry thinks
    // are queued but that the server pool does NOT hold (and that aren't
    // mid-enqueue). Run after applying results, so creatures whose result just
    // landed are already cleared and won't be double-touched. Returns # cleared.
    private async Task<int> ReconcileGhostsAsync()
    {
        var inPool = await FetchQueuedIdsAsync();
        if (inPool == null) return 0;   // server unreachable — leave local state alone

        int cleared = 0;
        foreach (var dna in registry.GetAll().Values)
        {
            if (dna.BusyState != BusyReason.QueuedForCombat) continue;
            if (inPool.Contains(dna.UniqueID))               continue;   // genuinely queued
            if (inFlightEnqueues.Contains(dna.UniqueID))     continue;   // still being enqueued

            dna.BusyState = BusyReason.None;
            cleared++;
            Debug.LogWarning($"[AsyncCombat] Ghost cleared: \"{dna.CustomName}\" was Queued locally but absent from the server pool.");
        }

        if (cleared > 0) GameEvents.RegistryChanged(registry);
        return cleared;
    }

    private bool ApplyResult(CloudMatchBlob r, string myPlayerName)
    {
        if (!registry.TryGet(r.CreatureId, out var dna))
        {
            Debug.LogWarning($"[AsyncCombat] Creature '{r.CreatureId}' not found — skipping result.");
            return false;
        }

        var dnaA = SaveSystem.DeserializeCreature(r.CreatureJsonA);
        var dnaB = SaveSystem.DeserializeCreature(r.CreatureJsonB);
        if (dnaA == null || dnaB == null)
        {
            Debug.LogWarning($"[AsyncCombat] Malformed match blob for '{r.CreatureId}' (missing snapshots) — skipping.");
            return false;
        }

        var gm      = GameManager.Instance;
        var config  = CombatController.Instance != null ? CombatController.Instance.Config : null;
        var db      = gm != null ? gm.Database : null;
        var equipDb = gm != null ? gm.EquipmentDatabase : null;
        if (config == null || db == null)
        {
            Debug.LogError("[AsyncCombat] Missing config/database — cannot simulate match.");
            return false;
        }

        var result = CombatService.SimulateCore(dnaA, dnaB, db, config, equipDb, new CombatRng(r.Seed));
        if (result == null) return false;

        var self = r.SelfWasA ? dnaA : dnaB;
        var opp  = r.SelfWasA ? dnaB : dnaA;
        bool won  = !result.IsDraw && result.WinnerID == self.UniqueID;
        bool died = !won && !result.IsDraw && result.LoserDied;

        dna.BusyState = BusyReason.None;
        dna.FightCount++;
        if (won)
        {
            dna.WinCount++;
            if (!string.IsNullOrEmpty(result.EvolvedSlot)) CombatEvolution.AdvanceTier(dna, result.EvolvedSlot);
        }
        if (died) dna.IsDead = true;

        // Persistent, replayable record (both clients derive it from the same
        // seeded simulation — we only store).
        dna.CombatHistory ??= new List<CombatRecord>();
        dna.CombatHistory.Add(CombatService.BuildRecord(result, self, opp, r.SelfWasA,
            r.OpponentPlayerName ?? "", r.OpponentPlayerId ?? "", r.Seed, ParseUtcOrNow(r.Date)));

        foreach (var line in result.Log)
            Debug.Log($"[AsyncCombat] {line}");

        string myLabel       = $"{myPlayerName} => \"{dna.CustomName}\"";
        string opponentLabel = string.IsNullOrEmpty(r.OpponentPlayerName)
            ? $"\"{r.OpponentName}\""
            : $"\"{r.OpponentName}\" <= {r.OpponentPlayerName}";
        string outcome  = result.IsDraw ? "Empate" : won ? "¡Ganaste!" : "¡Perdiste!";
        string evolved  = won && !string.IsNullOrEmpty(result.EvolvedSlot) ? $"  |  Evolved: {result.EvolvedSlot}" : "";
        string diedLine = died ? "  |  ¡Tu MoriMochi ha muerto permanentemente!" : "";

        Debug.Log($"[AsyncCombat]  {myLabel}  vs  {opponentLabel}  ——  {outcome}{evolved}{diedLine}");

        // Surface a viewable log for the combat panel's Results tab (the cloud copy
        // is cleared right after this), from this creature's perspective.
        GameEvents.CombatLogged(new CombatLogEntry
        {
            CreatureId    = dna.UniqueID,
            CreatureName  = dna.CustomName,
            OpponentLabel = string.IsNullOrEmpty(r.OpponentPlayerName)
                ? r.OpponentName
                : $"{r.OpponentName} ({r.OpponentPlayerName})",
            Lines = new List<string>(result.Log),
            Won   = won,
            Died  = died,
        });

        return true;
    }

    // Server sends the fight time as ISO-8601 UTC; fall back to now for older
    // results (or a malformed string) so the record always has a usable date.
    private static DateTime ParseUtcOrNow(string iso)
    {
        if (!string.IsNullOrEmpty(iso) &&
            DateTime.TryParse(iso, System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.AdjustToUniversal |
                System.Globalization.DateTimeStyles.AssumeUniversal, out var dt))
            return dt;
        return DateTime.UtcNow;
    }

    // Removes a creature from the matchmaking pool and clears its BusyState.
    // Safe to call even if the creature was already matched server-side — in that
    // case the local BusyState is still cleared and PollResultsAsync can be used
    // to collect the result later.
    public async Task DequeueAsync(CreatureDNA dna)
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogError("[AsyncCombat] Not signed in — cannot dequeue.");
            return;
        }

        status = $"Dequeueing \"{dna.CustomName}\"...";
        try
        {
            var payload  = new Dictionary<string, object> { { "creatureId", dna.UniqueID } };
            var response = await CloudEndpoint.CallAsync<CloudDequeueResponse>("dequeue-combat", payload);

            dna.BusyState = BusyReason.None;
            GameEvents.RegistryChanged(registry);

            if (response.Status == "dequeued")
            {
                status = $"\"{dna.CustomName}\" removed from queue.";
                Debug.Log($"[AsyncCombat] {status}");
            }
            else
            {
                status = $"\"{dna.CustomName}\" was not in the pool (may already be matched). Check pending results.";
                Debug.LogWarning($"[AsyncCombat] {status}");
            }
        }
        catch (Exception e)
        {
            status = $"Dequeue error: {e.Message}";
            Debug.LogError($"[AsyncCombat] DequeueAsync failed: {e}");
        }
    }

    // ── Cloud Code response contracts ─────────────────────────────

    [Serializable]
    private class CloudMatchResponse
    {
        public string Status;    // "queued" | "already_queued"
        public int    PoolSize;
    }

    [Serializable]
    private class CloudDequeueResponse
    {
        public string Status;    // "dequeued" | "not_found"
        public int    PoolSize;
    }

    [Serializable]
    private class CloudQueueStatusResponse
    {
        public List<string> InPool = new List<string>();
        public bool         Ok     = true;   // false → server had a partial read; don't reconcile
    }

    [Serializable]
    private class CloudMatchBlob
    {
        public string CreatureId;
        public int    Seed;
        public bool   SelfWasA;
        public string CreatureJsonA;
        public string CreatureJsonB;
        public string OpponentName;
        public string OpponentPlayerId;
        public string OpponentPlayerName;
        public string Date;
    }
}
}
