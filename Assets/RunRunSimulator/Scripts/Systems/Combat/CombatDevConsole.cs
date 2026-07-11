using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class CombatDevConsole : MonoBehaviour
{
    private static readonly List<int> DefaultLineup = new List<int> { (int)CombatRow.Front, (int)CombatRow.Front, (int)CombatRow.Mid };

    // ── Setup ─────────────────────────────────────────────────────

    [BoxGroup("Setup"), Required]
    [SerializeField] private GameManager gameManager;

    [BoxGroup("Setup"), Required]
    [SerializeField] private CombatController combatController;

    // ── Combat Fields ─────────────────────────────────────────────

    [BoxGroup("Combat")]
    [SerializeField, LabelText("Team A — UniqueIDs (3)")]
    private List<string> teamAIds = new List<string>();

    [BoxGroup("Combat")]
    [SerializeField, LabelText("Team B — UniqueIDs (3)")]
    private List<string> teamBIds = new List<string>();

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

    [Button("Fill Random Teams (3v3)"), GUIColor(1f, 0.65f, 0.5f), BoxGroup("Combat")]
    private void FillRandomFighters()
    {
        if (combatController == null) { Debug.LogError("[CombatDevConsole] CombatController not assigned."); return; }
        var cfg = combatController.Config;
        if (cfg == null) { Debug.LogError("[CombatDevConsole] No CombatManager assigned."); return; }

        var eligible = gameManager.Registry.GetAll().Values
            .Where(d => !d.IsDead && !d.IsBusy && d.FightCount < cfg.MaxFightCount)
            .ToList();

        if (eligible.Count < 6)
        {
            Debug.LogError("[CombatDevConsole] Not enough valid fighters — need at least 6 alive creatures under the fight limit.");
            return;
        }

        var shuffled = eligible.OrderBy(_ => Random.value).ToList();
        teamAIds = shuffled.Take(3).Select(d => d.UniqueID).ToList();
        teamBIds = shuffled.Skip(3).Take(3).Select(d => d.UniqueID).ToList();
        RefreshCombatInfo();
        Debug.Log($"[CombatDevConsole] Random teams — A: [{string.Join(", ", teamAIds.Select(Clip))}] | B: [{string.Join(", ", teamBIds.Select(Clip))}]");
    }

    [Button("Simulate Combat", ButtonSizes.Large), GUIColor(1f, 0.45f, 0.45f), BoxGroup("Combat")]
    private void SimulateCombatButton()
    {
        if (combatController == null) { Debug.LogError("[CombatDevConsole] CombatController not assigned."); return; }

        var result = combatController.SimulateLocal(teamAIds, teamBIds);
        if (result == null) return;

        foreach (var line in result.Log)
            Debug.Log($"[Combat] {line}");

        lastCombatResult = result.Summary;
        RefreshCombatInfo();
    }

    [Button("Verify Determinism (seed)"), GUIColor(0.4f, 1f, 0.6f), BoxGroup("Combat")]
    private void VerifyDeterminismButton()
    {
        if (gameManager == null) { Debug.LogError("[CombatDevConsole] GameManager not assigned."); return; }
        if (combatController == null) { Debug.LogError("[CombatDevConsole] CombatController not assigned."); return; }
        var cfg = combatController.Config;
        if (cfg == null) { Debug.LogError("[CombatDevConsole] No CombatManager assigned."); return; }

        if (teamAIds == null || teamBIds == null || teamAIds.Count < 1 || teamAIds.Count > 3 || teamBIds.Count < 1 || teamBIds.Count > 3)
        {
            Debug.LogError("[CombatDevConsole] Team A/B must each have 1-3 UniqueIDs — fill both before verifying.");
            return;
        }

        var dnasA = new List<CreatureDNA>();
        foreach (var id in teamAIds)
        {
            if (!gameManager.Registry.TryGet(id, out var d)) { Debug.LogError($"[CombatDevConsole] Team A id '{Clip(id)}' not found in registry."); return; }
            dnasA.Add(d);
        }
        var dnasB = new List<CreatureDNA>();
        foreach (var id in teamBIds)
        {
            if (!gameManager.Registry.TryGet(id, out var d)) { Debug.LogError($"[CombatDevConsole] Team B id '{Clip(id)}' not found in registry."); return; }
            dnasB.Add(d);
        }

        int seed = System.Guid.NewGuid().GetHashCode();

        string Fingerprint(CombatResult r) => JsonConvert.SerializeObject(new { r.TeamAWon, r.IsDraw, r.EvolvedUnitId, r.EvolvedSlot, r.DiedUnitId, r.Turns });

        List<int> RowsFor(int count) => DefaultLineup.Take(count).ToList();

        var cloneA1 = dnasA.Select(d => SaveSystem.DeserializeCreature(SaveSystem.Serialize(d))).ToList();
        var cloneB1 = dnasB.Select(d => SaveSystem.DeserializeCreature(SaveSystem.Serialize(d))).ToList();
        var r1 = CombatService.SimulateCore(cloneA1, cloneB1, RowsFor(cloneA1.Count), RowsFor(cloneB1.Count), gameManager.Database, cfg, gameManager.EquipmentDatabase, new CombatRng(seed));

        var cloneA2 = dnasA.Select(d => SaveSystem.DeserializeCreature(SaveSystem.Serialize(d))).ToList();
        var cloneB2 = dnasB.Select(d => SaveSystem.DeserializeCreature(SaveSystem.Serialize(d))).ToList();
        var r2 = CombatService.SimulateCore(cloneA2, cloneB2, RowsFor(cloneA2.Count), RowsFor(cloneB2.Count), gameManager.Database, cfg, gameManager.EquipmentDatabase, new CombatRng(seed));

        string fp1 = Fingerprint(r1);
        string fp2 = Fingerprint(r2);

        if (fp1 == fp2)
        {
            Debug.Log($"[CombatDevConsole] DETERMINISM OK — seed {seed}: two runs produced identical records ({r1.Turns.Count} turns).");
            lastCombatResult = "Determinism OK (seed " + seed + ")";
        }
        else
        {
            Debug.LogError($"[CombatDevConsole] DETERMINISM BROKEN — seed {seed}\nRun 1: {fp1}\nRun 2: {fp2}");
            lastCombatResult = "DETERMINISM BROKEN — check log";
        }
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
        fighterAInfo = BuildFightInfo(teamAIds);
        fighterBInfo = BuildFightInfo(teamBIds);
    }

    private string BuildFightInfo(List<string> ids)
    {
        if (gameManager == null || ids == null || ids.Count == 0) return "---";
        var cfg = combatController != null ? combatController.Config : null;
        int maxFights = cfg?.MaxFightCount ?? 5;

        var parts = new List<string>();
        foreach (var id in ids)
        {
            if (string.IsNullOrEmpty(id) || !gameManager.Registry.TryGet(id, out var dna)) { parts.Add("---"); continue; }
            parts.Add(dna.IsDead
                ? $"\"{dna.CustomName}\" DEAD"
                : $"\"{dna.CustomName}\" ({dna.Role}, {maxFights - dna.FightCount} left)");
        }
        return parts.Count == 0 ? "---" : string.Join("  |  ", parts);
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
