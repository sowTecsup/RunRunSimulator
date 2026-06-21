using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class CombatDevConsole : MonoBehaviour
{
    // ── Setup ─────────────────────────────────────────────────────

    [BoxGroup("Setup"), Required]
    [SerializeField] private GameManager gameManager;

    [BoxGroup("Setup"), Required]
    [SerializeField] private CombatController combatController;

    // ── Combat Fields ─────────────────────────────────────────────

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

    // ── Async Combat Fields ───────────────────────────────────────

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

    // ── Combat Buttons ────────────────────────────────────────────

    [Button("Fill Random Fighters"), GUIColor(1f, 0.65f, 0.5f), BoxGroup("Combat")]
    private void FillRandomFighters()
    {
        if (combatController == null) { Debug.LogError("[CombatDevConsole] CombatController not assigned."); return; }
        var cfg = combatController.Config;
        if (cfg == null) { Debug.LogError("[CombatDevConsole] No CombatManager assigned."); return; }

        var eligible = gameManager.Registry.GetAll().Values
            .Where(d => !d.IsDead && !d.IsBusy && d.FightCount < cfg.MaxFightCount)
            .ToList();

        if (eligible.Count < 2)
        {
            Debug.LogError("[CombatDevConsole] Not enough valid fighters — need at least 2 alive creatures under the fight limit.");
            return;
        }

        int idxA = Random.Range(0, eligible.Count);
        int idxB;
        do { idxB = Random.Range(0, eligible.Count); } while (idxB == idxA);

        combatAID = eligible[idxA].UniqueID;
        combatBID = eligible[idxB].UniqueID;
        RefreshCombatInfo();
        Debug.Log($"[CombatDevConsole] Random fighters — A: {Clip(combatAID)} | B: {Clip(combatBID)}");
    }

    [Button("Simulate Combat", ButtonSizes.Large), GUIColor(1f, 0.45f, 0.45f), BoxGroup("Combat")]
    private void SimulateCombatButton()
    {
        if (combatController == null) { Debug.LogError("[CombatDevConsole] CombatController not assigned."); return; }

        var result = combatController.SimulateLocal(combatAID, combatBID);
        if (result == null) return;

        foreach (var line in result.Log)
            Debug.Log($"[Combat] {line}");

        lastCombatResult = result.Summary;
        RefreshCombatInfo();
    }

    // ── Async Combat Buttons ──────────────────────────────────────

    [Button("Pick Random for Queue"), GUIColor(0.9f, 0.75f, 0.3f), BoxGroup("Async Combat")]
    private void PickRandomForQueue()
    {
        if (gameManager == null) { Debug.LogError("[CombatDevConsole] GameManager not assigned."); return; }
        var cfg = combatController != null ? combatController.Config : null;

        var eligible = gameManager.Registry.GetAll().Values
            .Where(d => !d.IsDead && !d.IsBusy && d.FightCount < (cfg?.MaxFightCount ?? 5))
            .ToList();

        if (eligible.Count == 0)
        {
            Debug.LogError("[CombatDevConsole] No eligible creature for async queue.");
            return;
        }

        var picked        = eligible[Random.Range(0, eligible.Count)];
        asyncCreatureID   = picked.UniqueID;
        asyncCreatureInfo = $"\"{picked.CustomName}\"  Fights left: {(cfg?.MaxFightCount ?? 5) - picked.FightCount}";
        RefreshQueueDisplay();
    }

    [Button("Enqueue for Combat (Instant)", ButtonSizes.Large), GUIColor(1f, 0.55f, 0.2f), BoxGroup("Async Combat")]
    private async void EnqueueInstantButton()
    {
        if (combatController == null) { Debug.LogError("[CombatDevConsole] CombatController not assigned."); return; }
        if (string.IsNullOrEmpty(asyncCreatureID))
        {
            Debug.LogWarning("[CombatDevConsole] No creature selected for async queue.");
            return;
        }
        await combatController.EnqueueForAsyncCombat(asyncCreatureID, false);
        asyncCreatureID   = "";
        asyncCreatureInfo = "---";
        RefreshQueueDisplay();
    }

    [Button("Enqueue for Combat (Timer)", ButtonSizes.Large), GUIColor(0.85f, 0.4f, 1f), BoxGroup("Async Combat")]
    private async void EnqueueScheduledButton()
    {
        if (combatController == null) { Debug.LogError("[CombatDevConsole] CombatController not assigned."); return; }
        if (string.IsNullOrEmpty(asyncCreatureID))
        {
            Debug.LogWarning("[CombatDevConsole] No creature selected for async queue.");
            return;
        }
        await combatController.EnqueueForAsyncCombat(asyncCreatureID, true);
        asyncCreatureID   = "";
        asyncCreatureInfo = "---";
        RefreshQueueDisplay();
    }

    [Button("Dequeue from Combat", ButtonSizes.Medium), GUIColor(1f, 0.4f, 0.4f), BoxGroup("Async Combat")]
    private void DequeueButton()
    {
        if (gameManager == null) { Debug.LogError("[CombatDevConsole] GameManager not assigned."); return; }
        if (combatController == null) { Debug.LogError("[CombatDevConsole] CombatController not assigned."); return; }

        var queued = gameManager.Registry.GetAll().Values
            .Where(d => d.BusyState == BusyReason.QueuedForCombat)
            .OrderBy(d => d.UniqueID)
            .ToList();

        if (queued.Count == 0)
        {
            Debug.LogWarning("[CombatDevConsole] No MoriMonchis are currently queued.");
            return;
        }
        if (dequeueIndex < 0 || dequeueIndex >= queued.Count)
        {
            Debug.LogError($"[CombatDevConsole] Index {dequeueIndex} out of range — queue has {queued.Count} creature(s) (0–{queued.Count - 1}). Press 'Show Queued MoriMonchis' to see the list.");
            return;
        }

        var dna = queued[dequeueIndex];
        _ = combatController.DequeueAsync(dna);
        RefreshQueueDisplay();
    }

    [Button("Show Queued MoriMonchis"), GUIColor(0.5f, 0.9f, 0.65f), BoxGroup("Async Combat")]
    private async void ShowQueuedButton()
    {
        if (gameManager == null) { Debug.LogError("[CombatDevConsole] GameManager not assigned."); return; }
        if (combatController == null) { Debug.LogError("[CombatDevConsole] CombatController not assigned."); return; }
        var cfg = combatController.Config;

        var queued = gameManager.Registry.GetAll().Values
            .Where(d => d.BusyState == BusyReason.QueuedForCombat)
            .OrderBy(d => d.UniqueID)
            .ToList();

        if (queued.Count == 0)
        {
            queuedCreaturesInfo = "None";
            Debug.Log("[CombatDevConsole] No MoriMonchis are currently queued for combat.");
            return;
        }

        HashSet<string> inPool  = null;
        HashSet<string> pending = new HashSet<string>();
        inPool  = await combatController.FetchQueuedIdsAsync();
        pending = await combatController.FetchPendingResultIdsAsync();

        string Status(CreatureDNA d) =>
            inPool == null               ? "?(offline)"   :
            inPool.Contains(d.UniqueID)  ? "In Queue"     :
            pending.Contains(d.UniqueID) ? "Result Ready" :
                                           "GHOST";

        queuedCreaturesInfo = string.Join(", ",
            queued.Select((d, i) => $"[{i}] \"{d.CustomName}\" — {Status(d)}"));

        Debug.Log($"[CombatDevConsole] {queued.Count} MoriMochi(s) flagged queued (press 'Check Pending Results' to apply results & clear ghosts):");
        for (int i = 0; i < queued.Count; i++)
        {
            var d = queued[i];
            Debug.Log($"  [{i}] \"{d.CustomName}\"  [{Clip(d.UniqueID)}]  status: {Status(d)}  Fights used: {d.FightCount}/{cfg?.MaxFightCount ?? 5}");
        }
    }

    [Button("Check Pending Results", ButtonSizes.Medium), GUIColor(0.4f, 0.85f, 1f), BoxGroup("Async Combat")]
    private async void CheckResultsButton()
    {
        if (combatController == null) { Debug.LogError("[CombatDevConsole] CombatController not assigned."); return; }
        await combatController.PollResultsAsync();
        RefreshQueueDisplay();
    }

    // ── Private Methods ───────────────────────────────────────────

    private void RefreshCombatInfo()
    {
        fighterAInfo = BuildFightInfo(combatAID);
        fighterBInfo = BuildFightInfo(combatBID);
    }

    private string BuildFightInfo(string id)
    {
        if (gameManager == null) return "---";
        var cfg = combatController != null ? combatController.Config : null;
        if (string.IsNullOrEmpty(id) || !gameManager.Registry.TryGet(id, out var dna)) return "---";
        if (dna.IsDead) return "DEAD — cannot fight";
        int maxFights = cfg?.MaxFightCount ?? 5;
        int remaining = maxFights - dna.FightCount;
        return $"\"{dna.CustomName}\"  Fights left: {remaining}/{maxFights}  (used: {dna.FightCount})";
    }

    private void RefreshQueueDisplay()
    {
        if (gameManager == null) return;
        var queued = gameManager.Registry.GetAll().Values
            .Where(d => d.BusyState == BusyReason.QueuedForCombat)
            .OrderBy(d => d.UniqueID)
            .ToList();
        queuedCreaturesInfo = queued.Count == 0
            ? "None"
            : string.Join(", ", queued.Select((d, i) => $"[{i}] \"{d.CustomName}\""));
    }

    private static string Clip(string id) => id.Length > 14 ? id[..14] + "…" : id;
}
}
