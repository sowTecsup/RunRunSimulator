using System.Collections;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class CombatVisualizerService : MonoBehaviour
{
    public static CombatVisualizerService Instance { get; private set; }

    [Title("References")]
    [Required, SerializeField] private CreatureDatabaseSO    database;
    [Required, SerializeField] private PartVisualBankSO      partVisualBank;
    [Required, SerializeField] private FurTypeDatabaseSO     furDatabase;
    [Required, SerializeField] private MoriMonchiVisualizer  visualizerPrefab;

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
        if (IsPlaying) Stop();
        playRoutine = StartCoroutine(PlayRoutine(dnaA, dnaB, record));
    }

    private IEnumerator PlayRoutine(CreatureDNA dnaA, CreatureDNA dnaB, CombatRecord record)
    {
        SpawnFighters(dnaA, dnaB);

        var statsA = CombatService.GetEffectiveStats(dnaA, database);
        var statsB = CombatService.GetEffectiveStats(dnaB, database);
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
        instanceA = Instantiate(visualizerPrefab, slotA.position, slotA.rotation, slotA);
        instanceB = Instantiate(visualizerPrefab, slotB.position, slotB.rotation, slotB);
        instanceA.SetFurDatabase(furDatabase);
        instanceB.SetFurDatabase(furDatabase);
        instanceA.Assemble(dnaA, partVisualBank);
        instanceB.Assemble(dnaB, partVisualBank);
    }

    private void DespawnFighters()
    {
        if (instanceA != null) Destroy(instanceA.gameObject);
        if (instanceB != null) Destroy(instanceB.gameObject);
        instanceA = null;
        instanceB = null;
    }
}
}
