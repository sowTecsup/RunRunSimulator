using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.CloudSave;
using UnityEngine;

// Orchestrates async combat: enqueues creatures via Cloud Code, polls Cloud Save for results.
// Cloud Code scripts:
//   - "enqueue-combat"      → called from here, adds creature to the global pool
//   - "process-matchmaking" → scheduled trigger in Dashboard, pairs & simulates pool
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
        inFlightEnqueues.Add(dna.UniqueID);
        GameEvents.RegistryChanged(registry);

        status = $"\"{dna.CustomName}\" — waiting {MIN_QUEUE_DELAY_SEC}s before matchmaking ({endpoint})...";
        Debug.Log($"[AsyncCombat] {status}");

        await Task.Delay(TimeSpan.FromSeconds(MIN_QUEUE_DELAY_SEC));

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

            var raw      = await CloudCodeService.Instance.CallEndpointAsync<string>(endpoint, payload);
            var response = JsonConvert.DeserializeObject<CloudMatchResponse>(raw);

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
                    status = $"Unexpected response: {raw}";
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
            var raw  = await CloudCodeService.Instance.CallEndpointAsync<string>(
                CLOUD_CODE_QUEUE_STATUS, new Dictionary<string, object>());
            var resp = JsonConvert.DeserializeObject<CloudQueueStatusResponse>(raw);
            return new HashSet<string>(resp?.InPool ?? new List<string>());
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

            var results = JsonConvert.DeserializeObject<List<CloudCombatResult>>(
                data[RESULTS_KEY].Value.GetAs<string>());

            return new HashSet<string>(
                (results ?? new List<CloudCombatResult>()).ConvertAll(r => r.CreatureId));
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

        var results = JsonConvert.DeserializeObject<List<CloudCombatResult>>(
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

    private bool ApplyResult(CloudCombatResult r, string myPlayerName)
    {
        if (!registry.TryGet(r.CreatureId, out var dna))
        {
            Debug.LogWarning($"[AsyncCombat] Creature '{r.CreatureId}' not found — skipping result.");
            return false;
        }

        dna.BusyState  = BusyReason.None;
        dna.FightCount++;

        if (r.Won)
        {
            dna.WinCount++;
            if (!string.IsNullOrEmpty(r.EvolvedSlot))
                AdvanceTier(dna, r.EvolvedSlot);
        }

        if (r.Died) dna.IsDead = true;

        foreach (var line in r.Log)
            Debug.Log($"[AsyncCombat] {line}");

        string myLabel       = $"{myPlayerName} => \"{dna.CustomName}\"";
        string opponentLabel = string.IsNullOrEmpty(r.OpponentPlayerName)
            ? $"\"{r.OpponentName}\""
            : $"\"{r.OpponentName}\" <= {r.OpponentPlayerName}";
        string outcome = r.Won ? "¡Ganaste!" : "¡Perdiste!";
        string evolved = r.Won && !string.IsNullOrEmpty(r.EvolvedSlot) ? $"  |  Evolved: {r.EvolvedSlot}" : "";
        string died    = r.Died ? "  |  ¡Tu MoriMochi ha muerto permanentemente!" : "";

        Debug.Log($"[AsyncCombat]  {myLabel}  vs  {opponentLabel}  ——  {outcome}{evolved}{died}");

        // Surface a viewable log for the combat panel's Results tab (the cloud copy
        // is cleared right after this), from this creature's perspective.
        GameEvents.CombatLogged(new CombatLogEntry
        {
            CreatureId    = dna.UniqueID,
            CreatureName  = dna.CustomName,
            OpponentLabel = string.IsNullOrEmpty(r.OpponentPlayerName)
                ? r.OpponentName
                : $"{r.OpponentName} ({r.OpponentPlayerName})",
            Lines = new List<string>(r.Log),
            Won   = r.Won,
            Died  = r.Died,
        });

        return true;
    }

    private static void AdvanceTier(CreatureDNA dna, string slot)
    {
        switch (slot)
        {
            case "Body":  if (dna.BodyTier  < Tier.Tier3) dna.BodyTier  = (Tier)((int)dna.BodyTier  + 1); break;
            case "Arm":   if (dna.ArmTier   < Tier.Tier3) dna.ArmTier   = (Tier)((int)dna.ArmTier   + 1); break;
            case "Eye":   if (dna.EyeTier   < Tier.Tier3) dna.EyeTier   = (Tier)((int)dna.EyeTier   + 1); break;
            case "Mouth": if (dna.MouthTier < Tier.Tier3) dna.MouthTier = (Tier)((int)dna.MouthTier + 1); break;
        }
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
            var payload = new Dictionary<string, object> { { "creatureId", dna.UniqueID } };
            var raw      = await CloudCodeService.Instance.CallEndpointAsync<string>("dequeue-combat", payload);
            var response = JsonConvert.DeserializeObject<CloudDequeueResponse>(raw);

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
    }

    [Serializable]
    private class CloudCombatResult
    {
        public string       CreatureId;
        public bool         Won;
        public bool         Died;
        public string       EvolvedSlot;
        public string       OpponentName;
        public string       OpponentPlayerId;
        public string       OpponentPlayerName;
        public List<string> Log = new List<string>();
    }
}
