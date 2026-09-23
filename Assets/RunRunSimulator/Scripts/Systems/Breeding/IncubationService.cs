using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class IncubationService : MonoBehaviour
{
    private CreatureRegistrySO registry;
    private CreatureDatabaseSO database;

    [Tooltip("Energy each parent spends when breeding starts (NeedsState).")]
    [SerializeField, Min(0f)] private float energyCostPerParent = 20f;

    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private string status = "Idle";

    private void Awake()
    {
        var gm   = GameManager.Instance;
        registry = gm.Registry;
        database = gm.Database;
    }

    public bool StartBreeding(string motherID, string fatherID)
    {
        if (GameClock.Instance == null || !GameClock.Instance.Loaded)
        {
            status = "Game clock not loaded — cannot start breeding.";
            Debug.LogError($"[Incubation] {status}");
            return false;
        }

        if (!ValidateParents(motherID, fatherID, out var mother, out var father)) return false;

        var odds = BreedingController.Instance != null ? BreedingController.Instance.InheritanceOdds : null;
        if (odds == null)
        {
            Debug.LogError("[Incubation] No InheritanceOddsTable available.");
            return false;
        }

        mother.BusyState      = BusyReason.Breeding;
        father.BusyState      = BusyReason.Breeding;
        mother.Needs.SpendEnergy(energyCostPerParent);
        father.Needs.SpendEnergy(energyCostPerParent);
        mother.BreedReadyAt   = GameClock.Instance.TotalMinutes + odds.BreedDurationMinutes;
        father.BreedReadyAt   = mother.BreedReadyAt;
        mother.BreedPartnerID = fatherID;
        father.BreedPartnerID = motherID;

        GameEvents.RegistryChanged(registry);

        status = $"Breeding started — \"{mother.CustomName}\" x \"{father.CustomName}\".";
        Debug.Log($"[Incubation] {status}");
        return true;
    }

    public void CancelBreeding(string motherID, string fatherID)
    {
        if (registry.TryGet(motherID, out var mother)) ClearBreedState(mother);
        if (registry.TryGet(fatherID, out var father)) ClearBreedState(father);
        GameEvents.RegistryChanged(registry);

        status = $"Breeding cancelled ({motherID} x {fatherID}).";
        Debug.Log($"[Incubation] {status}");
    }

    public void CancelAllBreeding()
    {
        foreach (var dna in registry.GetAll().Values)
            if (dna.BusyState == BusyReason.Breeding) ClearBreedState(dna);
        GameEvents.RegistryChanged(registry);

        status = "All eggs cancelled.";
        Debug.Log($"[Incubation] {status}");
    }

    public bool IsReady(CreatureDNA mother) =>
        mother != null && mother.BreedReadyAt > 0 &&
        GameClock.Instance != null && GameClock.Instance.TotalMinutes >= mother.BreedReadyAt;

    public int HatchCostFor(string motherID, string fatherID)
    {
        var odds = BreedingController.Instance != null ? BreedingController.Instance.InheritanceOdds : null;
        if (odds == null) return 0;
        registry.TryGet(motherID, out var mother);
        registry.TryGet(fatherID, out var father);
        return odds.HatchCost(mother, father);
    }

    public HatchResult TryHatch(string motherID, string fatherID)
    {
        if (!registry.TryGet(motherID, out var mother) || !registry.TryGet(fatherID, out var father))
        {
            Debug.LogError("[Incubation] TryHatch: parent(s) not found.");
            return HatchResult.Invalid;
        }

        if (!IsReady(mother))
        {
            status = $"Egg of \"{mother.CustomName}\" not ready yet.";
            return HatchResult.NotReady;
        }

        var odds = BreedingController.Instance != null ? BreedingController.Instance.InheritanceOdds : null;
        if (odds == null)
        {
            Debug.LogError("[Incubation] No InheritanceOddsTable available.");
            return HatchResult.Invalid;
        }

        int cost = odds.HatchCost(mother, father);
        if (!Wallet.TrySpend(Currency.Minerita, cost, "hatch"))
        {
            status = $"Not enough Minerita to hatch ({cost}).";
            return HatchResult.InsufficientMinerita;
        }

        HatchLocally(motherID, fatherID);
        return HatchResult.Hatched;
    }

    private bool ValidateParents(string motherID, string fatherID, out CreatureDNA mother, out CreatureDNA father)
    {
        mother = father = null;
        if (!registry.TryGet(motherID, out mother)) { Debug.LogError($"[Incubation] Mother '{motherID}' not found."); return false; }
        if (!registry.TryGet(fatherID, out father)) { Debug.LogError($"[Incubation] Father '{fatherID}' not found."); return false; }
        if (!CreatureAvailability.IsFree(mother) || !CreatureAvailability.IsFree(father))
        {
            Debug.LogError("[Incubation] Cannot breed: a parent is not free.");
            return false;
        }
        if (mother.Gender != CreatureGender.Female || father.Gender != CreatureGender.Male)
        {
            Debug.LogError("[Incubation] Breeding requires one Female (mother) and one Male (father).");
            return false;
        }
        if (mother.BreedCount >= BreedingService.MaxBreedCount || father.BreedCount >= BreedingService.MaxBreedCount)
        {
            Debug.LogError($"[Incubation] A parent has reached max breeds ({BreedingService.MaxBreedCount}).");
            return false;
        }
        return true;
    }

    private void HatchLocally(string motherID, string fatherID)
    {
        if (registry.TryGet(motherID, out var mother)) ClearBreedState(mother);
        if (registry.TryGet(fatherID, out var father)) ClearBreedState(father);

        var odds = BreedingController.Instance != null ? BreedingController.Instance.InheritanceOdds : null;
        if (odds == null) { Debug.LogError("[Incubation] No InheritanceOddsTable available."); return; }

        var child = BreedingService.Breed(motherID, fatherID, registry, database, odds);
        if (child == null)
        {
            status = "Hatch failed during local mint — see errors.";
            GameEvents.RegistryChanged(registry);
            return;
        }

        child.CustomName = CreatureNameBank.GetRandomName();
        child.Stamp();
        child.BirthDay = GameClock.Instance != null ? GameClock.Instance.Day : 1;
        if (!registry.Register(child)) return;

        if (registry.TryGet(motherID, out var m)) m.ChildrenIDs.Add(child.UniqueID);
        if (registry.TryGet(fatherID, out var f)) f.ChildrenIDs.Add(child.UniqueID);

        GameEvents.BreedingCompleted(m, f, child);
        GameEvents.RegistryChanged(registry);

        status = $"Egg hatched! \"{child.CustomName}\" ({child.Gender}) was born.";
        Debug.Log($"[Incubation] {status}  {child.UniqueID}");
    }

    private static void ClearBreedState(CreatureDNA dna)
    {
        dna.BusyState      = BusyReason.None;
        dna.BreedReadyAt   = 0;
        dna.BreedPartnerID = "";
    }
}
}
