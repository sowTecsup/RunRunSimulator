using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class CombatVisualizerService : MonoBehaviour
{
    public static CombatVisualizerService Instance { get; private set; }

    [Title("Scene Refs")]
    [Required, SerializeField] private MoriMonchiVisualizer visualizerPrefab;

    [Title("Slots")]
    [Required, SerializeField] private Transform slotA;
    [Required, SerializeField] private Transform slotB;

    [Title("Timing (seconds)")]
    [SerializeField, MinValue(0f)] private float windupSeconds       = 0.35f;
    [SerializeField, MinValue(0f)] private float impactSeconds       = 0.35f;
    [SerializeField, MinValue(0f)] private float betweenTurnsSeconds = 0.55f;
    [SerializeField, MinValue(0f)] private float endHoldSeconds      = 1.5f;

    private MoriMonchiVisualizer instanceA;
    private MoriMonchiVisualizer instanceB;
    private Coroutine            playRoutine;

    public bool IsPlaying => playRoutine != null;

    private CreatureDatabaseSO Db       => GameManager.Instance != null ? GameManager.Instance.Database : null;
    private PartVisualBankSO   PartBank => GameManager.Instance != null ? GameManager.Instance.PartVisualBank : null;
    private FurTypeDatabaseSO  FurDb    => GameManager.Instance != null ? GameManager.Instance.FurTypeDatabase : null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    [Button("Stop"), DisableInEditorMode]
    public void Stop()
    {
        if (playRoutine != null) { StopCoroutine(playRoutine); playRoutine = null; }
        DespawnFighters();
    }

    public void Play(CreatureDNA dnaA, CreatureDNA dnaB, CombatRecord record)
    {
        if (dnaA == null || dnaB == null || record == null)
        {
            Debug.LogError("[CombatVisualizer] Play called with null DNA or record.");
            return;
        }
        if (GameManager.Instance == null)
        {
            Debug.LogError("[CombatVisualizer] No GameManager in scene — cannot resolve visual databases.");
            return;
        }
        if (IsPlaying) Stop();
        playRoutine = StartCoroutine(PlayRoutine(dnaA, dnaB, record));
    }

    private IEnumerator PlayRoutine(CreatureDNA dnaA, CreatureDNA dnaB, CombatRecord record)
    {
        SpawnFighters(dnaA, dnaB);

        var statsA = CombatService.GetEffectiveStats(dnaA, Db);
        var statsB = CombatService.GetEffectiveStats(dnaB, Db);
        float hpMaxA = statsA.HP;
        float hpMaxB = statsB.HP;
        float hpA    = hpMaxA;
        float hpB    = hpMaxB;

        var ctx = new CombatVisualContext
        {
            DnaA = dnaA, DnaB = dnaB,
            HpMaxA = hpMaxA, HpMaxB = hpMaxB,
            SlotA = slotA, SlotB = slotB,
            TotalTurns = record.Turns?.Count ?? 0,
        };

        CombatVisualEvents.VisualCombatStart(ctx);
        CombatVisualEvents.HpChanged(CombatVisualSide.A, hpA, hpMaxA);
        CombatVisualEvents.HpChanged(CombatVisualSide.B, hpB, hpMaxB);
        CombatVisualEvents.Log($"VS: {dnaA.CustomName} vs {dnaB.CustomName}");

        bool aDead = false;
        bool bDead = false;

        if (record.Turns != null)
        {
            foreach (var turn in record.Turns)
            {
                var attacker = turn.AttackerIsA ? CombatVisualSide.A : CombatVisualSide.B;
                var defender = turn.AttackerIsA ? CombatVisualSide.B : CombatVisualSide.A;

                CombatVisualEvents.TurnStart(turn);
                CombatVisualEvents.Log($"Turno {turn.TurnNumber} · {turn.AttackerName} → {turn.DefenderName}");
                CombatVisualEvents.Attack(attacker);
                yield return new WaitForSeconds(windupSeconds);

                var hit = new CombatVisualHit
                {
                    Attacker = attacker,
                    Defender = defender,
                    Damage   = turn.Damage,
                    Crit     = turn.WasCrit,
                };
                CombatVisualEvents.Hit(hit);
                if (turn.WasCrit)
                {
                    CombatVisualEvents.Crit(hit);
                    CombatVisualEvents.Log($"¡Crítico! {turn.Damage:F1} de daño");
                }
                else
                {
                    CombatVisualEvents.Log($"Daño: {turn.Damage:F1}");
                }

                if (turn.AttackerIsA)
                {
                    hpB = turn.DefenderHpAfter;
                    CombatVisualEvents.HpChanged(CombatVisualSide.B, hpB, hpMaxB);
                    if (hpB <= 0f && !bDead)
                    {
                        bDead = true;
                        CombatVisualEvents.Dead(CombatVisualSide.B);
                        CombatVisualEvents.Log($"{turn.DefenderName} cae derrotado.");
                    }
                }
                else
                {
                    hpA = turn.DefenderHpAfter;
                    CombatVisualEvents.HpChanged(CombatVisualSide.A, hpA, hpMaxA);
                    if (hpA <= 0f && !aDead)
                    {
                        aDead = true;
                        CombatVisualEvents.Dead(CombatVisualSide.A);
                        CombatVisualEvents.Log($"{turn.DefenderName} cae derrotado.");
                    }
                }

                yield return new WaitForSeconds(impactSeconds);
                CombatVisualEvents.TurnEnd(turn);

                if (aDead || bDead) break;
                yield return new WaitForSeconds(betweenTurnsSeconds);
            }
        }

        bool isDraw = !aDead && !bDead;
        var  winner = aDead ? CombatVisualSide.B : CombatVisualSide.A;
        CombatVisualEvents.Log(isDraw
            ? "Empate."
            : $"Ganador: {(winner == CombatVisualSide.A ? dnaA.CustomName : dnaB.CustomName)}");
        CombatVisualEvents.VisualCombatEnd(winner, isDraw);

        yield return new WaitForSeconds(endHoldSeconds);
        DespawnFighters();
        playRoutine = null;
    }

    private void SpawnFighters(CreatureDNA dnaA, CreatureDNA dnaB)
    {
        DespawnFighters();
        instanceA = SpawnOne(dnaA, slotA, CombatVisualSide.A);
        instanceB = SpawnOne(dnaB, slotB, CombatVisualSide.B);
    }

    private MoriMonchiVisualizer SpawnOne(CreatureDNA dna, Transform slot, CombatVisualSide side)
    {
        var inst = Instantiate(visualizerPrefab, slot.position, slot.rotation, slot);
        var ui   = inst.GetComponentInChildren<MoriMonchiCombatVisualizerUITK>(true);
        if (ui != null) ui.SetSide(side);
        inst.SetFurDatabase(FurDb);
        inst.Assemble(dna, PartBank);
        return inst;
    }

    private void DespawnFighters()
    {
        if (instanceA != null) Destroy(instanceA.gameObject);
        if (instanceB != null) Destroy(instanceB.gameObject);
        instanceA = null;
        instanceB = null;
    }

    // ── DEV — Test Harness ────────────────────────────────────────
    // Selección de una pelea ya persistida (CreatureDNA.CombatHistory) para dispararla
    // a mano. Los dropdowns leen GameManager.Instance.Registry, así que sólo se pueblan
    // en Play Mode (en Editor el registro está vacío).

    [FoldoutGroup("DEV — Test Harness"), ShowInInspector]
    [ValueDropdown(nameof(FighterOptions)), LabelText("Combatiente A")]
    private string devCreatureA;

    [FoldoutGroup("DEV — Test Harness"), ShowInInspector]
    [ValueDropdown(nameof(FightOptions)), LabelText("Pelea (slot)")]
    private int devFightIndex;

    [FoldoutGroup("DEV — Test Harness"), ShowInInspector]
    [ValueDropdown(nameof(OpponentOptions)), LabelText("Rival B (vacío = auto)")]
    private string devCreatureB;

    [FoldoutGroup("DEV — Test Harness")]
    [Button("🎲 MM al azar con pelea", ButtonSizes.Medium), DisableInEditorMode]
    private void DevPickRandom()
    {
        var reg = GameManager.Instance != null ? GameManager.Instance.Registry : null;
        if (reg == null) { Debug.LogWarning("[CombatVisualizer] No GameManager/Registry."); return; }

        var withFights = reg.GetAll().Values
            .Where(d => d != null && d.CombatHistory != null && d.CombatHistory.Any(HasTurns))
            .ToList();
        if (withFights.Count == 0) { Debug.LogWarning("[CombatVisualizer] Ningún MM tiene peleas con turnos."); return; }

        var pick = withFights[Random.Range(0, withFights.Count)];
        devCreatureA  = pick.UniqueID;
        devFightIndex = pick.CombatHistory.FindIndex(HasTurns);
        if (devFightIndex < 0) devFightIndex = 0;
        devCreatureB  = "";
        Debug.Log($"[CombatVisualizer] Seleccionado \"{pick.CustomName}\" · pelea #{devFightIndex}.");
    }

    [FoldoutGroup("DEV — Test Harness")]
    [Button("▶ Simular", ButtonSizes.Large), GUIColor(0.55f, 1f, 0.7f), DisableInEditorMode]
    private void DevSimulate()
    {
        var dnaA = ResolveDna(devCreatureA);
        if (dnaA == null) { Debug.LogWarning("[CombatVisualizer] Elegí un Combatiente A válido."); return; }
        if (dnaA.CombatHistory == null || devFightIndex < 0 || devFightIndex >= dnaA.CombatHistory.Count)
        { Debug.LogWarning("[CombatVisualizer] Índice de pelea inválido."); return; }

        var record = dnaA.CombatHistory[devFightIndex];
        var dnaB   = ResolveOpponent(devCreatureB, record, dnaA);
        if (dnaB == null) { Debug.LogWarning("[CombatVisualizer] No pude resolver el rival B. Elegilo manualmente."); return; }

        Play(dnaA, dnaB, record);
    }

    private static bool HasTurns(CombatRecord r) => r != null && r.Turns != null && r.Turns.Count > 0;

    private CreatureDNA ResolveDna(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        var reg = GameManager.Instance != null ? GameManager.Instance.Registry : null;
        return reg != null && reg.TryGet(id, out var dna) ? dna : null;
    }

    private CreatureDNA ResolveOpponent(string overrideId, CombatRecord record, CreatureDNA self)
    {
        var explicitB = ResolveDna(overrideId);
        if (explicitB != null) return explicitB;

        var reg = GameManager.Instance != null ? GameManager.Instance.Registry : null;
        if (reg == null) return null;

        foreach (var dna in reg.GetAll().Values)
            if (dna != null && dna != self && dna.CustomName == record.OpponentName) return dna;
        foreach (var dna in reg.GetAll().Values)
            if (dna != null && dna != self) return dna;
        return null;
    }

    private IEnumerable<ValueDropdownItem<string>> FighterOptions()
    {
        var list = new List<ValueDropdownItem<string>>();
        var reg  = GameManager.Instance != null ? GameManager.Instance.Registry : null;
        if (reg == null) return list;
        foreach (var dna in reg.GetAll().Values)
        {
            if (dna == null || dna.CombatHistory == null || dna.CombatHistory.Count == 0) continue;
            list.Add(new ValueDropdownItem<string>($"{dna.CustomName} ({dna.CombatHistory.Count} peleas)", dna.UniqueID));
        }
        return list;
    }

    private IEnumerable<ValueDropdownItem<int>> FightOptions()
    {
        var list = new List<ValueDropdownItem<int>>();
        var dna  = ResolveDna(devCreatureA);
        if (dna == null || dna.CombatHistory == null) return list;
        for (int i = 0; i < dna.CombatHistory.Count; i++)
        {
            var r = dna.CombatHistory[i];
            list.Add(new ValueDropdownItem<int>($"#{i} vs {r.OpponentName} ({r.Turns?.Count ?? 0} turnos)", i));
        }
        return list;
    }

    private IEnumerable<ValueDropdownItem<string>> OpponentOptions()
    {
        var list = new List<ValueDropdownItem<string>> { new ValueDropdownItem<string>("(auto desde la pelea)", "") };
        var reg  = GameManager.Instance != null ? GameManager.Instance.Registry : null;
        if (reg == null) return list;
        foreach (var dna in reg.GetAll().Values)
            if (dna != null) list.Add(new ValueDropdownItem<string>(dna.CustomName, dna.UniqueID));
        return list;
    }
}
}
