using System.Collections.Generic;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class CombatController : MonoBehaviour
{
    public static CombatController Instance { get; private set; }

    private CreatureRegistrySO registry;
    private CreatureDatabaseSO database;

    [BoxGroup("Setup")]
    [SerializeField] private CombatManagerSO config;

    [BoxGroup("Setup")]
    [SerializeField] private AsyncCombatService asyncCombatService;

    private void Awake()
    {
        Instance = this;
        var gm = GameManager.Instance;
        registry = gm.Registry;
        database = gm.Database;
    }

    // ── Public Properties ─────────────────────────────────────────

    private void OnDestroy() { if (Instance == this) Instance = null; }

    public CombatManagerSO Config => config;

    // ── Public Methods ────────────────────────────────────────────

    public CombatResult SimulateLocal(string aID, string bID)
    {
        if (config == null) { Debug.LogError("[CombatController] No CombatManager assigned."); return null; }
        var result = CombatService.Simulate(aID, bID, registry, database, config, GameManager.Instance?.EquipmentDatabase);
        if (result == null) return null;
        GameEvents.CombatCompleted(result);
        GameEvents.RegistryChanged(registry);
        return result;
    }

    public async Task EnqueueForAsyncCombat(string uniqueID, bool scheduled)
    {
        if (!registry.TryGet(uniqueID, out var dna))
        {
            Debug.LogError($"[CombatController] Creature '{uniqueID}' not found.");
            return;
        }
        if (dna.IsDead || dna.IsBusy)
        {
            Debug.LogWarning($"[CombatController] \"{dna.CustomName}\" cannot be queued (dead or busy: {dna.BusyState}).");
            return;
        }
        if (asyncCombatService == null) { Debug.LogError("[CombatController] AsyncCombatService not assigned."); return; }

        if (scheduled) await asyncCombatService.EnqueueScheduledAsync(dna);
        else           await asyncCombatService.EnqueueInstantAsync(dna);
    }

    // ── Async Service Wrappers ────────────────────────────────────

    public Task DequeueAsync(CreatureDNA dna) =>
        asyncCombatService != null ? asyncCombatService.DequeueAsync(dna) : Task.CompletedTask;

    public Task PollResultsAsync() =>
        asyncCombatService != null ? asyncCombatService.PollResultsAsync() : Task.CompletedTask;

    public Task<HashSet<string>> FetchQueuedIdsAsync() =>
        asyncCombatService != null ? asyncCombatService.FetchQueuedIdsAsync() : Task.FromResult<HashSet<string>>(null);

    public Task<HashSet<string>> FetchPendingResultIdsAsync() =>
        asyncCombatService != null ? asyncCombatService.FetchPendingResultIdsAsync() : Task.FromResult(new HashSet<string>());
}
}
