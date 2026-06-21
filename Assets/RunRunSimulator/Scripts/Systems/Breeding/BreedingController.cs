using System;
using System.Linq;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

// Domain owner of the breeding services in the scene (dev tooling lives in BreedingDevConsole).
// Attach to the same GameObject as GameManager. Resolves its assets from
// GameManager.Instance in Awake — no serialized cross-references needed.
// Pens (BreedingContainer) don't hold their own AsyncBreedingService / affinity table:
// they ask BreedingController.Instance for them, so there's a single source of truth.
public class BreedingController : MonoBehaviour
{
    public static BreedingController Instance { get; private set; }

    // ── Cached References ─────────────────────────────────────────

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

    // ── Lifecycle ─────────────────────────────────────────────────

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

    // ── Breeding services (requested by pens) ─────────────────────

    public float GetAffinity(Personality a, Personality b) =>
        affinityTable?.GetAffinity(a, b) ?? 0.5f;

    // Async server-side breeding: a pen requests these instead of owning the service.
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

    // ── Public Methods ────────────────────────────────────────────

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
