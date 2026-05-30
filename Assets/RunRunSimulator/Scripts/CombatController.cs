using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

// Local combat UI + simulation flow. Attach to the same GameObject as GameManager.
// Resolves its assets from GameManager.Instance in Awake — no serialized
// cross-references needed.
public class CombatController : MonoBehaviour
{
    // ── Cached References ─────────────────────────────────────────

    private CreatureRegistrySO registry;
    private CreatureDatabaseSO database;
    private CombatManagerSO    config;

    // ── Private Fields ────────────────────────────────────────────

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

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        var gm = GameManager.Instance;
        registry = gm.Registry;
        database = gm.Database;
        config   = gm.CombatConfig ?? CombatManagerSO.Current;
    }

    // ── Private Methods ───────────────────────────────────────────

    [Button("Fill Random Fighters"), GUIColor(1f, 0.65f, 0.5f), BoxGroup("Combat")]
    private void FillRandomFighters()
    {
        if (config == null) { Debug.LogError("[CombatController] No CombatManager assigned."); return; }

        var eligible = registry.GetAll().Values
            .Where(d => !d.IsDead && !d.IsBusy && d.FightCount < config.MaxFightCount)
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
        RefreshCombatInfo();
        Debug.Log($"[CombatController] Random fighters — A: {Clip(combatAID)} | B: {Clip(combatBID)}");
    }

    [Button("Simulate Combat", ButtonSizes.Large), GUIColor(1f, 0.45f, 0.45f), BoxGroup("Combat")]
    private void SimulateCombatButton()
    {
        if (config == null) { Debug.LogError("[CombatController] No CombatManager assigned."); return; }

        var result = CombatService.Simulate(combatAID, combatBID, registry, database, config);
        if (result == null) return;

        foreach (var line in result.Log)
            Debug.Log($"[Combat] {line}");

        GameEvents.CombatCompleted(result);
        GameEvents.RegistryChanged(registry);
        lastCombatResult = result.Summary;
        RefreshCombatInfo();
    }

    private void RefreshCombatInfo()
    {
        fighterAInfo = BuildFightInfo(combatAID);
        fighterBInfo = BuildFightInfo(combatBID);
    }

    private string BuildFightInfo(string id)
    {
        if (string.IsNullOrEmpty(id) || !registry.TryGet(id, out var dna)) return "---";
        if (dna.IsDead) return "DEAD — cannot fight";
        int remaining = config.MaxFightCount - dna.FightCount;
        return $"\"{dna.CustomName}\"  Fights left: {remaining}/{config.MaxFightCount}  (used: {dna.FightCount})";
    }

    private static string Clip(string id) => id.Length > 14 ? id[..14] + "…" : id;

    // ══════════════════════════════════════════════════════════════
    // ASYNC COMBAT — Etapa 2.3
    // ══════════════════════════════════════════════════════════════

    [BoxGroup("Async Combat")]
    [InfoBox("Pon criaturas en cola y cierra el juego. Cloud Code empareja y simula server-side. Vuelve cuando quieras y revisa los resultados.")]
    [SerializeField, LabelText("Creature to Queue")]
    private string asyncCreatureID = "";

    [ShowInInspector, ReadOnly, LabelText("Creature Info"), BoxGroup("Async Combat")]
    private string asyncCreatureInfo = "---";

    [ShowInInspector, ReadOnly, LabelText("In Queue"), BoxGroup("Async Combat")]
    private string queuedCreaturesInfo = "---";

    [BoxGroup("Async Combat"), SerializeField, LabelText("Dequeue Index")]
    private int dequeueIndex = 0;

    [Button("Pick Random for Queue"), GUIColor(0.9f, 0.75f, 0.3f), BoxGroup("Async Combat")]
    private void PickRandomForQueue()
    {
        var eligible = registry.GetAll().Values
            .Where(d => !d.IsDead && !d.IsBusy && d.FightCount < (config?.MaxFightCount ?? 5))
            .ToList();

        if (eligible.Count == 0)
        {
            Debug.LogError("[CombatController] No eligible creature for async queue.");
            return;
        }

        var picked        = eligible[Random.Range(0, eligible.Count)];
        asyncCreatureID   = picked.UniqueID;
        asyncCreatureInfo = $"\"{picked.CustomName}\"  Fights left: {(config?.MaxFightCount ?? 5) - picked.FightCount}";
        RefreshQueueDisplay();
    }

    [Button("Enqueue for Combat (Instant)", ButtonSizes.Large), GUIColor(1f, 0.55f, 0.2f), BoxGroup("Async Combat")]
    private void EnqueueInstantButton()
    {
        if (string.IsNullOrEmpty(asyncCreatureID))
        {
            Debug.LogWarning("[CombatController] No creature selected for async queue.");
            return;
        }
        EnqueueForAsyncCombat(asyncCreatureID, scheduled: false);
        asyncCreatureID   = "";
        asyncCreatureInfo = "---";
    }

    [Button("Enqueue for Combat (Timer)", ButtonSizes.Large), GUIColor(0.85f, 0.4f, 1f), BoxGroup("Async Combat")]
    private void EnqueueScheduledButton()
    {
        if (string.IsNullOrEmpty(asyncCreatureID))
        {
            Debug.LogWarning("[CombatController] No creature selected for async queue.");
            return;
        }
        EnqueueForAsyncCombat(asyncCreatureID, scheduled: true);
        asyncCreatureID   = "";
        asyncCreatureInfo = "---";
    }

    [Button("Dequeue from Combat", ButtonSizes.Medium), GUIColor(1f, 0.4f, 0.4f), BoxGroup("Async Combat")]
    private void DequeueButton()
    {
        var queued = registry.GetAll().Values
            .Where(d => d.BusyState == BusyReason.QueuedForCombat)
            .OrderBy(d => d.UniqueID)
            .ToList();

        if (queued.Count == 0)
        {
            Debug.LogWarning("[CombatController] No MoriMonchis are currently queued.");
            return;
        }
        if (dequeueIndex < 0 || dequeueIndex >= queued.Count)
        {
            Debug.LogError($"[CombatController] Index {dequeueIndex} out of range — queue has {queued.Count} creature(s) (0–{queued.Count - 1}). Press 'Show Queued MoriMonchis' to see the list.");
            return;
        }
        if (asyncCombatService == null) { Debug.LogError("[CombatController] AsyncCombatService not assigned."); return; }

        var dna = queued[dequeueIndex];
        _ = asyncCombatService.DequeueAsync(dna);
        RefreshQueueDisplay();
    }

    [Button("Show Queued MoriMonchis"), GUIColor(0.5f, 0.9f, 0.65f), BoxGroup("Async Combat")]
    private void ShowQueuedButton()
    {
        var queued = registry.GetAll().Values
            .Where(d => d.BusyState == BusyReason.QueuedForCombat)
            .OrderBy(d => d.UniqueID)
            .ToList();
        queuedCreaturesInfo = queued.Count == 0
            ? "None"
            : string.Join(", ", queued.Select((d, i) => $"[{i}] \"{d.CustomName}\""));

        if (queued.Count == 0)
        {
            Debug.Log("[CombatController] No MoriMonchis are currently queued for combat.");
            return;
        }
        Debug.Log($"[CombatController] {queued.Count} MoriMochi(s) in async queue:");
        for (int i = 0; i < queued.Count; i++)
        {
            var d = queued[i];
            Debug.Log($"  [{i}] \"{d.CustomName}\"  [{Clip(d.UniqueID)}]  Fights used: {d.FightCount}/{config?.MaxFightCount ?? 5}");
        }
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
        var queued = registry.GetAll().Values
            .Where(d => d.BusyState == BusyReason.QueuedForCombat)
            .OrderBy(d => d.UniqueID)
            .ToList();
        queuedCreaturesInfo = queued.Count == 0
            ? "None"
            : string.Join(", ", queued.Select((d, i) => $"[{i}] \"{d.CustomName}\""));
    }

    // ── Public Methods ────────────────────────────────────────────

    public async void EnqueueForAsyncCombat(string uniqueID, bool scheduled = false)
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

        RefreshQueueDisplay();
        if (scheduled) await asyncCombatService.EnqueueScheduledAsync(dna);
        else           await asyncCombatService.EnqueueInstantAsync(dna);
        RefreshQueueDisplay();
    }
}
