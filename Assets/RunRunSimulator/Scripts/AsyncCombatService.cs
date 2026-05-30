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
    private const string RESULTS_KEY           = "combat_results";
    private const string CLOUD_CODE_INSTANT   = "run-combat";        // immediate match if pool has someone
    private const string CLOUD_CODE_SCHEDULED = "enqueue-combat";    // always waits for the next tick
    private const float  MIN_QUEUE_DELAY_SEC   = 5f;

    // ── Private Fields ────────────────────────────────────────────

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
    }

    // Pulls combat_results from Cloud Save and applies each one to the local registry.
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

            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(
                new HashSet<string> { RESULTS_KEY });

            if (!data.ContainsKey(RESULTS_KEY))
            {
                status = "No pending results.";
                return;
            }

            var results = JsonConvert.DeserializeObject<List<CloudCombatResult>>(
                data[RESULTS_KEY].Value.GetAs<string>());

            if (results == null || results.Count == 0)
            {
                status = "No pending results.";
                return;
            }

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
            status = $"Applied {applied} combat result(s).";
            Debug.Log($"[AsyncCombat] Applied {applied} pending result(s).");
        }
        catch (Exception e)
        {
            status = $"Poll error: {e.Message}";
            Debug.LogError($"[AsyncCombat] PollResultsAsync failed: {e}");
        }
    }

    // ── Private Methods ───────────────────────────────────────────

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
