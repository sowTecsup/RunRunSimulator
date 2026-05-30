using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using UnityEngine;

// Orchestrates timed (async) breeding. The start timestamp and the hatch check
// are server-authoritative (Cloud Code + Custom Data) — the client cannot forge
// the timer. The cría itself is still minted locally and pushed (design checkpoint:
// move generation server-side in a later stage).
// Cloud Code scripts:
//   - "start-breeding" → stamps server time, marks the egg in Game Data
//   - "hatch-breeding" → server clock check; authorizes the hatch when ready
// Attach to the same GameObject as GameManager. Resolves its assets via
// GameManager.Instance in Awake — no serialized cross-references needed.
public class AsyncBreedingService : MonoBehaviour
{
    private const string CLOUD_CODE_START = "start-breeding";
    private const string CLOUD_CODE_HATCH = "hatch-breeding";

    // ── Cached References ─────────────────────────────────────────

    private CreatureRegistrySO     registry;
    private CreatureDatabaseSO     database;
    private InheritanceOddsTableSO inheritanceOdds;

    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private string status = "Idle";

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        var gm          = GameManager.Instance;
        registry        = gm.Registry;
        database        = gm.Database;
        inheritanceOdds = gm.InheritanceOddsTable;
    }

    // ── Public Methods ────────────────────────────────────────────

    // Validates locally, stamps the timer server-side, marks both parents Breeding.
    public async Task StartBreedingAsync(string motherID, string fatherID)
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogError("[AsyncBreeding] Not signed in — cannot start breeding.");
            return;
        }

        if (!ValidateParents(motherID, fatherID, out var mother, out var father)) return;

        status = $"Starting breeding \"{mother.CustomName}\" x \"{father.CustomName}\"...";
        try
        {
            var payload = new Dictionary<string, object>
            {
                { "motherId", motherID },
                { "fatherId", fatherID },
            };

            var raw      = await CloudCodeService.Instance.CallEndpointAsync<string>(CLOUD_CODE_START, payload);
            var response = JsonConvert.DeserializeObject<StartResponse>(raw);

            if (response.Status == "already_breeding")
            {
                status = "You already have an egg incubating — hatch it first.";
                Debug.LogWarning($"[AsyncBreeding] {status}");
                return;
            }

            // Mark both parents busy + cache readyAt locally for display.
            mother.BusyState    = BusyReason.Breeding;
            father.BusyState    = BusyReason.Breeding;
            mother.BreedReadyAt = response.ReadyAt;
            father.BreedReadyAt = response.ReadyAt;
            mother.BreedPartnerID = fatherID;
            father.BreedPartnerID = motherID;

            SaveSystem.SaveDatabase(registry);
            GameManager.Instance.PushToCloud();

            var ready = DateTimeOffset.FromUnixTimeMilliseconds(response.ReadyAt).LocalDateTime;
            status = $"Breeding started — egg ready at {ready:HH:mm:ss}.";
            Debug.Log($"[AsyncBreeding] {status}");
        }
        catch (Exception e)
        {
            status = $"Start breeding error: {e.Message}";
            Debug.LogError($"[AsyncBreeding] StartBreedingAsync failed: {e}");
        }
    }

    // Server clock check for a specific egg (the mother+father pair).
    // On "ready", mints the cría locally and pushes.
    public async Task HatchAsync(string motherID, string fatherID)
    {
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogError("[AsyncBreeding] Not signed in — cannot hatch.");
            return;
        }

        status = "Checking egg with server...";
        try
        {
            var payload = new Dictionary<string, object>
            {
                { "motherId", motherID },
                { "fatherId", fatherID },
            };
            var raw      = await CloudCodeService.Instance.CallEndpointAsync<string>(CLOUD_CODE_HATCH, payload);
            var response = JsonConvert.DeserializeObject<HatchResponse>(raw);

            switch (response.Status)
            {
                case "no_egg":
                    status = "That egg is no longer on the server.";
                    Debug.LogWarning($"[AsyncBreeding] {status}");
                    ClearLocalEggState(motherID, fatherID);   // local cache out of sync — clean up just this pair
                    break;

                case "not_ready":
                    var ready = DateTimeOffset.FromUnixTimeMilliseconds(response.ReadyAt).LocalDateTime;
                    var remaining = TimeSpan.FromMilliseconds(Math.Max(0, response.RemainingMs));
                    status = $"Not ready yet — {remaining:mm\\:ss} left (ready at {ready:HH:mm:ss}).";
                    Debug.Log($"[AsyncBreeding] {status}");
                    break;

                case "ready":
                    HatchLocally(response.MotherId, response.FatherId);
                    break;

                default:
                    status = $"Unexpected hatch response: {raw}";
                    Debug.LogWarning($"[AsyncBreeding] {status}");
                    break;
            }
        }
        catch (Exception e)
        {
            status = $"Hatch error: {e.Message}";
            Debug.LogError($"[AsyncBreeding] HatchAsync failed: {e}");
        }
    }

    // ── Private Methods ───────────────────────────────────────────

    private bool ValidateParents(string motherID, string fatherID, out CreatureDNA mother, out CreatureDNA father)
    {
        mother = father = null;
        if (!registry.TryGet(motherID, out mother)) { Debug.LogError($"[AsyncBreeding] Mother '{motherID}' not found."); return false; }
        if (!registry.TryGet(fatherID, out father)) { Debug.LogError($"[AsyncBreeding] Father '{fatherID}' not found."); return false; }
        if (mother.IsDead || father.IsDead)         { Debug.LogError("[AsyncBreeding] Cannot breed: a parent is dead."); return false; }
        if (mother.IsBusy || father.IsBusy)         { Debug.LogError("[AsyncBreeding] Cannot breed: a parent is busy."); return false; }
        if (mother.Gender != CreatureGender.Female || father.Gender != CreatureGender.Male)
        {
            Debug.LogError("[AsyncBreeding] Breeding requires one Female (mother) and one Male (father).");
            return false;
        }
        if (mother.BreedCount >= BreedingService.MaxBreedCount || father.BreedCount >= BreedingService.MaxBreedCount)
        {
            Debug.LogError($"[AsyncBreeding] A parent has reached max breeds ({BreedingService.MaxBreedCount}).");
            return false;
        }
        return true;
    }

    // Server authorized the hatch — clear busy on both parents, then mint the cría
    // locally (BreedingService re-validates + increments BreedCount) and push.
    private void HatchLocally(string motherID, string fatherID)
    {
        // Clear busy BEFORE Breed() — its own validation rejects busy parents.
        if (registry.TryGet(motherID, out var mother)) ClearBreedState(mother);
        if (registry.TryGet(fatherID, out var father)) ClearBreedState(father);

        var odds = inheritanceOdds ?? InheritanceOddsTableSO.Current;
        if (odds == null) { Debug.LogError("[AsyncBreeding] No InheritanceOddsTable available."); return; }

        var child = BreedingService.Breed(motherID, fatherID, registry, database, odds);
        if (child == null)
        {
            status = "Hatch failed during local mint — see errors.";
            SaveSystem.SaveDatabase(registry);
            GameManager.Instance.PushToCloud();
            return;
        }

        child.CustomName = CreatureNameBank.GetRandomName();
        child.Stamp();
        if (!registry.Register(child)) return;

        if (registry.TryGet(motherID, out var m)) m.ChildrenIDs.Add(child.UniqueID);
        if (registry.TryGet(fatherID, out var f)) f.ChildrenIDs.Add(child.UniqueID);

        SaveSystem.SaveDatabase(registry);
        GameManager.Instance.PushToCloud();

        status = $"Egg hatched! \"{child.CustomName}\" ({child.Gender}) was born.";
        Debug.Log($"[AsyncBreeding] {status}  {child.UniqueID}");
    }

    private static void ClearBreedState(CreatureDNA dna)
    {
        dna.BusyState      = BusyReason.None;
        dna.BreedReadyAt   = 0;
        dna.BreedPartnerID = "";
    }

    // Clears the local egg cache for a specific pair when the server says no_egg.
    private void ClearLocalEggState(string motherID, string fatherID)
    {
        if (registry.TryGet(motherID, out var mother)) ClearBreedState(mother);
        if (registry.TryGet(fatherID, out var father)) ClearBreedState(father);
        SaveSystem.SaveDatabase(registry);
        GameManager.Instance.PushToCloud();
    }

    // ── Cloud Code response contracts ─────────────────────────────

    [Serializable]
    private class StartResponse
    {
        public string Status;     // "breeding" | "already_breeding"
        public long   StartedAt;
        public long   ReadyAt;
    }

    [Serializable]
    private class HatchResponse
    {
        public string Status;       // "ready" | "not_ready" | "no_egg"
        public long   ReadyAt;
        public long   RemainingMs;
        public string MotherId;
        public string FatherId;
    }
}
