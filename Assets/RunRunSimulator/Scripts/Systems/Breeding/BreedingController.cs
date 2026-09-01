using System;
using System.Linq;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class BreedingController : MonoBehaviour
{
    public static BreedingController Instance { get; private set; }

    private CreatureRegistrySO  registry;
    private CreatureDatabaseSO  database;

    [BoxGroup("Setup")]
    [SerializeField] private InheritanceOddsTableSO inheritanceOddsTable;
    public InheritanceOddsTableSO InheritanceOdds => inheritanceOddsTable;

    [BoxGroup("Setup")]
    [SerializeField] private AsyncBreedingService asyncBreedingService;

    [BoxGroup("Setup")]
    [SerializeField] private BreedingAffinityTableSO affinityTable;

    [BoxGroup("Setup")]
    [Tooltip("Life stage thresholds (age in days → stage). Read by the NameTag for the age line.")]
    [SerializeField] private CreatureLifeStageTableSO lifeStageTable;

    public CreatureLifeStageTableSO LifeStageTable => lifeStageTable;

    private void Awake()
    {
        Instance = this;

        var gm   = GameManager.Instance;
        registry = gm.Registry;
        database = gm.Database;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public float GetAffinity(Role a, Role b) =>
        affinityTable?.GetAffinity(a, b) ?? 0.5f;

    public Task StartBreedingAsync(string motherID, string fatherID) =>
        asyncBreedingService != null
            ? asyncBreedingService.StartBreedingAsync(motherID, fatherID)
            : Fail("AsyncBreedingService not assigned on BreedingController.");

    public Task HatchAsync(string motherID, string fatherID) =>
        asyncBreedingService != null
            ? asyncBreedingService.HatchAsync(motherID, fatherID)
            : Fail("AsyncBreedingService not assigned on BreedingController.");

    public Task CancelBreedingAsync(string motherID, string fatherID) =>
        asyncBreedingService != null
            ? asyncBreedingService.CancelBreedingAsync(motherID, fatherID)
            : Fail("AsyncBreedingService not assigned on BreedingController.");

    public Task CancelAllBreedingAsync() =>
        asyncBreedingService != null
            ? asyncBreedingService.CancelAllBreedingAsync()
            : Fail("AsyncBreedingService not assigned on BreedingController.");

    private static Task Fail(string message)
    {
        Debug.LogError($"[BreedingController] {message}");
        return Task.CompletedTask;
    }

    public string BreedCreatures(string motherID, string fatherID)
    {
        var odds = inheritanceOddsTable;
        if (odds == null) { Debug.LogError("[BreedingController] No InheritanceOddsTable assigned."); return null; }

        var child = BreedingService.Breed(motherID, fatherID, registry, database, odds);
        if (child == null) return null;

        child.CustomName = CreatureNameBank.GetRandomName();
        child.Stamp();
        if (!registry.Register(child)) return null;

        if (registry.TryGet(motherID, out var mother)) mother.ChildrenIDs.Add(child.UniqueID);
        if (registry.TryGet(fatherID, out var father)) father.ChildrenIDs.Add(child.UniqueID);

        GameEvents.BreedingCompleted(mother, father, child);
        GameEvents.RegistryChanged(registry);
        Debug.Log($"[BreedingController] Bred child: \"{child.CustomName}\"  {child.UniqueID}  ({child.Gender})");
        return child.UniqueID;
    }
}
}
