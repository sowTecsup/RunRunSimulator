using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

// Local combat UI + simulation flow. Attach to the same GameObject as GameManager.
// Async combat flow will extend this controller in Etapa 2.3.
public class CombatController : MonoBehaviour
{
    // ── Private Fields ────────────────────────────────────────────

    [Required, AssetsOnly, BoxGroup("Setup")]
    [SerializeField] private CreatureRegistrySO creatureRegistry;

    [Required, AssetsOnly, BoxGroup("Setup")]
    [SerializeField] private CreatureDatabaseSO database;

    [AssetsOnly, BoxGroup("Setup")]
    [SerializeField] private CombatManagerSO combatConfig;

    [BoxGroup("Setup")]
    [SerializeField] private AsyncCombatService asyncCombatService;

    [BoxGroup("Combat")]
    [SerializeField, LabelText("Fighter A — UniqueID")]
    private string combatAID = "";

    [BoxGroup("Combat")]
    [SerializeField, LabelText("Fighter B — UniqueID")]
    private string combatBID = "";

    [ShowInInspector, ReadOnly, LabelText("Fighter A Info"), BoxGroup("Combat")]
    private string fighterAInfo = "---";

    [ShowInInspector, ReadOnly, LabelText("Fighter B Info"), BoxGroup("Combat")]
    private string fighterBInfo = "---";

    [ShowInInspector, ReadOnly, LabelText("Last Result"), BoxGroup("Combat")]
    private string lastCombatResult = "---";

    // ── Private Methods ───────────────────────────────────────────

    [Button("Fill Random Fighters"), GUIColor(1f, 0.65f, 0.5f), BoxGroup("Combat")]
    private void FillRandomFighters()
    {
        var config = combatConfig ?? CombatManagerSO.Current;
        if (config == null) { Debug.LogError("[CombatController] No CombatManager assigned."); return; }

        var eligible = creatureRegistry.GetAll().Values
            .Where(d => !d.IsDead && d.FightCount < config.MaxFightCount)
            .ToList();

        if (eligible.Count < 2)
        {
            Debug.LogError("[CombatController] Not enough valid fighters — need at least 2 alive creatures under the fight limit.");
            return;
        }

        int idxA = Random.Range(0, eligible.Count);
        int idxB;
        do { idxB = Random.Range(0, eligible.Count); } while (idxB == idxA);

        combatAID = eligible[idxA].UniqueID;
        combatBID = eligible[idxB].UniqueID;
        RefreshCombatInfo(config);
        Debug.Log($"[CombatController] Random fighters — A: {Clip(combatAID)} | B: {Clip(combatBID)}");
    }

    [Button("Simulate Combat", ButtonSizes.Large), GUIColor(1f, 0.45f, 0.45f), BoxGroup("Combat")]
    private void SimulateCombatButton()
    {
        var config = combatConfig ?? CombatManagerSO.Current;
        if (config == null) { Debug.LogError("[CombatController] No CombatManager assigned."); return; }

        var result = CombatService.Simulate(combatAID, combatBID, creatureRegistry, database, config);
        if (result == null) return;

        foreach (var line in result.Log)
            Debug.Log($"[Combat] {line}");

        SaveSystem.SaveDatabase(creatureRegistry);
        lastCombatResult = result.Summary;
        RefreshCombatInfo(config);
    }

    private void RefreshCombatInfo(CombatManagerSO config)
    {
        fighterAInfo = BuildFightInfo(combatAID, config);
        fighterBInfo = BuildFightInfo(combatBID, config);
    }

    private string BuildFightInfo(string id, CombatManagerSO config)
    {
        if (string.IsNullOrEmpty(id) || !creatureRegistry.TryGet(id, out var dna)) return "---";
        if (dna.IsDead) return "DEAD — cannot fight";
        int remaining = config.MaxFightCount - dna.FightCount;
        return $"\"{dna.CustomName}\"  Fights left: {remaining}/{config.MaxFightCount}  (used: {dna.FightCount})";
    }

    private static string Clip(string id) => id.Length > 14 ? id[..14] + "…" : id;

    // ══════════════════════════════════════════════════════════════
    // ASYNC COMBAT — Etapa 2.3
    // Requiere: AsyncCombatService + Cloud Code (run_combat.js)
    // ══════════════════════════════════════════════════════════════

    [BoxGroup("Async Combat")]
    [InfoBox("Pon criaturas en cola y cierra el juego. Cloud Code empareja y simula server-side. Vuelve cuando quieras y revisa los resultados.")]
    [SerializeField, LabelText("Creature to Queue")]
    private string asyncCreatureID = "";

    [ShowInInspector, ReadOnly, LabelText("Creature Info"), BoxGroup("Async Combat")]
    private string asyncCreatureInfo = "---";

    [ShowInInspector, ReadOnly, LabelText("In Queue"), BoxGroup("Async Combat")]
    private string queuedCreaturesInfo = "---";

    [Button("Pick Random for Queue"), GUIColor(0.9f, 0.75f, 0.3f), BoxGroup("Async Combat")]
    private void PickRandomForQueue()
    {
        var config = combatConfig ?? CombatManagerSO.Current;

        var eligible = creatureRegistry.GetAll().Values
            .Where(d => !d.IsDead && !d.IsBusy && d.FightCount < (config?.MaxFightCount ?? 5))
            .ToList();

        if (eligible.Count == 0)
        {
            Debug.LogError("[CombatController] No eligible creature for async queue.");
            return;
        }

        var picked       = eligible[Random.Range(0, eligible.Count)];
        asyncCreatureID  = picked.UniqueID;
        asyncCreatureInfo = $"\"{picked.CustomName}\"  Fights left: {(config?.MaxFightCount ?? 5) - picked.FightCount}";
        RefreshQueueDisplay();
    }

    [Button("Enqueue for Combat", ButtonSizes.Large), GUIColor(1f, 0.55f, 0.2f), BoxGroup("Async Combat")]
    private void EnqueueButton()
    {
        if (string.IsNullOrEmpty(asyncCreatureID))
        {
            Debug.LogWarning("[CombatController] No creature selected for async queue.");
            return;
        }
        EnqueueForAsyncCombat(asyncCreatureID);
        asyncCreatureID   = "";
        asyncCreatureInfo = "---";
    }

    [Button("Check Pending Results", ButtonSizes.Medium), GUIColor(0.4f, 0.85f, 1f), BoxGroup("Async Combat")]
    private async void CheckResultsButton()
    {
        if (asyncCombatService == null) { Debug.LogError("[CombatController] AsyncCombatService not assigned."); return; }
        await asyncCombatService.PollResultsAsync();
        RefreshQueueDisplay();
    }

    private void RefreshQueueDisplay()
    {
        var busy = creatureRegistry.GetAll().Values
            .Where(d => d.BusyState == BusyReason.QueuedForCombat).ToList();
        queuedCreaturesInfo = busy.Count == 0
            ? "None"
            : string.Join(", ", busy.Select(d => $"\"{d.CustomName}\""));
    }

    // ── Public Methods ────────────────────────────────────────────

    public async void EnqueueForAsyncCombat(string uniqueID)
    {
        if (!creatureRegistry.TryGet(uniqueID, out var dna))
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

        RefreshQueueDisplay();
        await asyncCombatService.EnqueueAsync(dna);
        RefreshQueueDisplay();
    }
}
