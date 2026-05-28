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
// Cloud Code module: "run-combat"  (see cloud_code/run_combat.js)
// Attach to same GameObject as GameManager and CombatController.
public class AsyncCombatService : MonoBehaviour
{
    private const string RESULTS_KEY           = "combat_results";
    private const string CLOUD_CODE_MODULE     = "run-combat";
    private const float  MIN_QUEUE_DELAY_SEC   = 5f;

    // ── Private Fields ────────────────────────────────────────────

    [Required, AssetsOnly, BoxGroup("Setup")]
    [SerializeField] private CreatureRegistrySO registry;

    [Required, AssetsOnly, BoxGroup("Setup")]
    [SerializeField] private CreatureDatabaseSO database;

    [AssetsOnly, BoxGroup("Setup")]
    [SerializeField] private CombatManagerSO combatConfig;

    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private string status = "Idle";

    // ── Public Methods ────────────────────────────────────────────

    // Marks creature as busy, waits MIN_QUEUE_DELAY_SEC, then calls Cloud Code matchmaking.
    public async Task EnqueueAsync(CreatureDNA dna)
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogError("[AsyncCombat] Not signed in — cannot enqueue.");
            return;
        }

        dna.BusyState = BusyReason.QueuedForCombat;
        SaveSystem.SaveDatabase(registry);

        status = $"\"{dna.CustomName}\" — waiting {MIN_QUEUE_DELAY_SEC}s before matchmaking...";
        Debug.Log($"[AsyncCombat] {status}");

        await Task.Delay(TimeSpan.FromSeconds(MIN_QUEUE_DELAY_SEC));

        try
        {
            status = $"\"{dna.CustomName}\" — calling matchmaking...";

            var payload = new Dictionary<string, object>
            {
                { "creatureId",   dna.UniqueID },
                { "customName",   dna.CustomName },
                { "creatureJson", JsonConvert.SerializeObject(dna) },
            };

            var raw      = await CloudCodeService.Instance.CallEndpointAsync<string>(CLOUD_CODE_MODULE, payload);
            var response = JsonConvert.DeserializeObject<CloudMatchResponse>(raw);

            if (response.Status == "waiting")
            {
                status = $"\"{dna.CustomName}\" is waiting for an opponent.";
                Debug.Log($"[AsyncCombat] \"{dna.CustomName}\" queued — no opponent yet.");
            }
            else if (response.Status == "matched")
            {
                status = $"\"{dna.CustomName}\" was matched! Applying result...";
                Debug.Log($"[AsyncCombat] \"{dna.CustomName}\" matched immediately.");
                await PollResultsAsync();
            }
        }
        catch (Exception e)
        {
            // Rollback busy state so the creature is usable again
            dna.BusyState = BusyReason.None;
            SaveSystem.SaveDatabase(registry);

            status = $"Enqueue error: {e.Message}";
            Debug.LogError($"[AsyncCombat] EnqueueAsync failed: {e}");
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

            int applied = 0;
            foreach (var r in results)
                if (ApplyResult(r)) applied++;

            // Clear the results key so they're not applied twice
            await CloudSaveService.Instance.Data.Player.SaveAsync(new Dictionary<string, object>
            {
                { RESULTS_KEY, "[]" }
            });

            SaveSystem.SaveDatabase(registry);
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

    private bool ApplyResult(CloudCombatResult r)
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

        Debug.Log($"[AsyncCombat] \"{dna.CustomName}\" — " +
                  $"{(r.Won ? "WON" : "LOST")} vs \"{r.OpponentName}\"" +
                  $"{(r.Won && r.EvolvedSlot != null ? $" | Evolved: {r.EvolvedSlot}" : "")}" +
                  $"{(r.Died ? " | DIED" : "")}");

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

    // ── Cloud Code response contracts ─────────────────────────────

    [Serializable]
    private class CloudMatchResponse
    {
        public string Status;  // "waiting" | "matched"
    }

    [Serializable]
    private class CloudCombatResult
    {
        public string       CreatureId;
        public bool         Won;
        public bool         Died;
        public string       EvolvedSlot;
        public string       OpponentName;
        public List<string> Log = new List<string>();
    }
}
