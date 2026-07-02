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

    [Title("Timing (seconds, divided by speed)")]
    [SerializeField, MinValue(0f)] private float windupSeconds       = 0.35f;
    [SerializeField, MinValue(0f)] private float impactSeconds       = 0.35f;
    [SerializeField, MinValue(0f)] private float betweenTurnsSeconds = 0.55f;
    [SerializeField, MinValue(0f)] private float deathPauseSeconds   = 0.6f;

    [Title("Playback")]
    [SerializeField, Range(0.25f, 4f)] private float playbackSpeed = 1f;

    private const string SelfColor   = "5AA0FF";
    private const string OppColor    = "FF6B6B";
    private const string DamageColor = "FF3B3B";

    private class CombatNode
    {
        public bool                      HasTurn;
        public CombatTurn                Turn;
        public float                     HpA;
        public float                     HpB;
        public bool                      ADead;
        public bool                      BDead;
        public bool                      ADiedHere;
        public bool                      BDiedHere;
        public int                       TurnNumber;
        public CombatVisualSide          Attacker;
        public CombatVisualSide          Defender;
        public bool                      Crit;
        public List<CombatVisualLogLine> Log;
        public CombatNode                Prev;
        public CombatNode                Next;
        public bool IsEnd => Next == null;

        private static MoriMonchiCombatVisualizer Pick(CombatVisualSide side, MoriMonchiCombatVisualizer a, MoriMonchiCombatVisualizer b)
            => side == CombatVisualSide.A ? a : b;

        public void FireWindup(MoriMonchiCombatVisualizer a, MoriMonchiCombatVisualizer b)
        {
            if (Turn == null || Turn.NoAttack) return;
            Pick(Attacker, a, b)?.PlayAttack();
        }

        public void FireImpact(MoriMonchiCombatVisualizer a, MoriMonchiCombatVisualizer b, float maxA, float maxB)
        {
            if (Turn == null || Turn.NoAttack) return;
            var atk = Pick(Attacker, a, b);
            var def = Pick(Defender, a, b);
            if (Crit) { atk?.PlayCritDealt(); def?.PlayCritTaken(); }
            else      { atk?.PlayHitDealt();  def?.PlayHitTaken(); }
            float defMax = Defender == CombatVisualSide.A ? maxA : maxB;
            def?.PlayHpChanged(Turn.DefenderHpAfter, defMax);
        }

        public void FireDeath(MoriMonchiCombatVisualizer a, MoriMonchiCombatVisualizer b)
        {
            if (ADiedHere) a?.PlayDead();
            if (BDiedHere) b?.PlayDead();
        }
    }

    private MoriMonchiVisualizer instanceA;
    private MoriMonchiVisualizer instanceB;
    private MoriMonchiCombatVisualizer hooksA;
    private MoriMonchiCombatVisualizer hooksB;
    private MoriMonchiCombatVisualizerUITK barA;
    private MoriMonchiCombatVisualizerUITK barB;
    private MoriMonchiProceduralAnimator   animA;
    private MoriMonchiProceduralAnimator   animB;

    private CreatureDNA  selfDna;
    private CreatureDNA  oppDna;
    private CombatRecord activeRecord;
    private CombatNode   head;
    private CombatNode   current;
    private int          totalTurns;
    private float        hpMaxA;
    private float        hpMaxB;
    private float        shownHpA;
    private float        shownHpB;
    private EffectiveStats statsA;
    private EffectiveStats statsB;
    private bool         endIsDraw;
    private CombatVisualSide endWinner;

    private bool isAuto;
    private bool busy;

    private Coroutine beginRoutine;
    private Coroutine autoRoutine;
    private Coroutine fwdRoutine;

    public bool  IsPlaying => head != null;
    public bool  IsAuto    => isAuto;
    public float Speed     => Mathf.Max(0.01f, playbackSpeed);

    private bool CanForward => current != null && current.Next != null && !busy;
    private bool CanBack    => current != null && current.Prev != null && !busy;

    private CreatureDatabaseSO Db => GameManager.Instance != null ? GameManager.Instance.Database : null;
    private EquipmentDatabaseSO EquipDb => GameManager.Instance != null ? GameManager.Instance.EquipmentDatabase : null;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Play(CreatureDNA self, CreatureDNA opponent, CombatRecord record)
    {
        if (self == null || opponent == null || record == null)
        {
            Debug.LogError("[CombatVisualizer] Play called with null DNA or record.");
            return;
        }
        if (GameManager.Instance == null)
        {
            Debug.LogError("[CombatVisualizer] No GameManager in scene — cannot resolve visual databases.");
            return;
        }
        Stop();
        selfDna      = self;
        oppDna       = opponent;
        activeRecord = record;
        BuildStates();
        if (head == null) { Debug.LogWarning("[CombatVisualizer] No states built."); return; }
        beginRoutine = StartCoroutine(BeginRoutine());
    }

    [Button("Stop"), DisableInEditorMode]
    public void Stop()
    {
        StopAllCoroutines();
        beginRoutine = null;
        autoRoutine  = null;
        fwdRoutine   = null;
        isAuto       = false;
        busy         = false;
        head         = null;
        current      = null;
        DespawnFighters();
    }

    public void TogglePlay() => SetAuto(!isAuto);

    public void SetAuto(bool value)
    {
        if (head == null) return;
        if (value && current != null && current.IsEnd) Restore(head);
        isAuto = value;
        if (autoRoutine != null) { StopCoroutine(autoRoutine); autoRoutine = null; }
        if (isAuto && !busy) autoRoutine = StartCoroutine(AutoRoutine());
        Publish();
    }

    public void Next()
    {
        if (!CanForward) return;
        SetAuto(false);
        fwdRoutine = StartCoroutine(ForwardRoutine());
    }

    public void Back()
    {
        if (!CanBack) return;
        SetAuto(false);
        Restore(current.Prev);
    }

    public void SetSpeed(float value)
    {
        playbackSpeed = Mathf.Clamp(value, 0.25f, 4f);
        Publish();
    }

    private void BuildStates()
    {
        statsA = EquipmentStats.Apply(CombatStats.GetEffectiveStats(selfDna, Db), selfDna, EquipDb);
        statsB = EquipmentStats.Apply(CombatStats.GetEffectiveStats(oppDna, Db), oppDna, EquipDb);
        hpMaxA = statsA.Constitution * CombatStats.BaseHpCombatMultiplier;
        hpMaxB = statsB.Constitution * CombatStats.BaseHpCombatMultiplier;

        var log = new List<CombatVisualLogLine>
        {
            Line(CombatVisualLogKind.Versus,
                $"{Colored(selfDna.CustomName, SelfColor)}   VS   {Colored(activeRecord.OpponentName, OppColor)}"),
        };
        float hpA = hpMaxA, hpB = hpMaxB;
        bool aDead = false, bDead = false;

        head = new CombatNode { HasTurn = false, HpA = hpA, HpB = hpB, TurnNumber = 0, Log = new List<CombatVisualLogLine>(log) };
        var node = head;
        totalTurns = 0;

        if (activeRecord.Turns != null)
        {
            foreach (var t in activeRecord.Turns)
            {
                bool attackerIsSelf = t.AttackerIsA == activeRecord.SelfWasA;
                void ApplyProc(CombatProcEvent pe)
                {
                    bool isA = pe.TargetIsA == activeRecord.SelfWasA;
                    float before = isA ? hpA : hpB;
                    float after  = pe.TargetHpAfter;
                    if (isA) hpA = after; else hpB = after;

                    string who = Colored(isA ? selfDna.CustomName : activeRecord.OpponentName, isA ? SelfColor : OppColor);
                    log.Add(Line(CombatVisualLogKind.Proc, ProcText(pe, who, after - before)));
                }

                if (t.Procs != null)
                    foreach (var pe in t.Procs)
                        if (pe.BeforeStrike) ApplyProc(pe);

                if (!t.NoAttack)
                {
                    string atk = Colored(t.AttackerName, attackerIsSelf ? SelfColor : OppColor);
                    string def = Colored(t.DefenderName, attackerIsSelf ? OppColor : SelfColor);
                    string dmg = Colored($"{t.Damage:F0}", DamageColor);
                    log.Add(t.WasCrit
                        ? Line(CombatVisualLogKind.Crit, $"¡CRÍTICO! {atk} → {def} · {dmg} de daño")
                        : Line(CombatVisualLogKind.Hit,  $"{atk} golpea a {def} · {dmg} de daño"));

                    if (attackerIsSelf) hpB = t.DefenderHpAfter;
                    else                hpA = t.DefenderHpAfter;
                }

                if (t.Procs != null)
                    foreach (var pe in t.Procs)
                        if (!pe.BeforeStrike) ApplyProc(pe);

                bool aDiedHere = false, bDiedHere = false;
                if (hpA <= 0f && !aDead) { aDead = true; aDiedHere = true; log.Add(Line(CombatVisualLogKind.Death, $"{Colored(selfDna.CustomName, SelfColor)} cae derrotado")); }
                if (hpB <= 0f && !bDead) { bDead = true; bDiedHere = true; log.Add(Line(CombatVisualLogKind.Death, $"{Colored(activeRecord.OpponentName, OppColor)} cae derrotado")); }

                var next = new CombatNode
                {
                    HasTurn = true, Turn = t,
                    HpA = hpA, HpB = hpB, ADead = aDead, BDead = bDead,
                    ADiedHere = aDiedHere, BDiedHere = bDiedHere,
                    TurnNumber = t.TurnNumber, Log = new List<CombatVisualLogLine>(log),
                    Attacker = attackerIsSelf ? CombatVisualSide.A : CombatVisualSide.B,
                    Defender = attackerIsSelf ? CombatVisualSide.B : CombatVisualSide.A,
                    Crit     = t.WasCrit,
                };
                node.Next = next; next.Prev = node; node = next;
                totalTurns++;
                if (aDead || bDead) break;
            }
        }

        endIsDraw = !aDead && !bDead;
        endWinner = aDead ? CombatVisualSide.B : CombatVisualSide.A;
        string winnerName = endWinner == CombatVisualSide.A ? selfDna.CustomName : activeRecord.OpponentName;
        node.Log.Add(endIsDraw
            ? Line(CombatVisualLogKind.Result, "Empate")
            : Line(CombatVisualLogKind.Result, $"Ganador: {Colored(winnerName, endWinner == CombatVisualSide.A ? SelfColor : OppColor)}"));

        current = head;
    }

    private static CombatVisualLogLine Line(CombatVisualLogKind kind, string text)
        => new CombatVisualLogLine { Kind = kind, Text = text };

    private static string Colored(string text, string hex) => $"<color=#{hex}>{text}</color>";

    private IEnumerator BeginRoutine()
    {
        SpawnFighters(selfDna, oppDna);
        yield return null;
        yield return null;

        var ctx = new CombatVisualContext
        {
            DnaA = selfDna, DnaB = oppDna,
            HpMaxA = hpMaxA, HpMaxB = hpMaxB,
            SlotA = slotA, SlotB = slotB,
            TotalTurns = totalTurns,
        };
        CombatVisualEvents.VisualCombatStart(ctx);
        barA?.Bind(selfDna.CustomName, statsA.Attack, statsA.Speed);
        barB?.Bind(activeRecord.OpponentName, statsB.Attack, statsB.Speed);
        hooksA?.PlayCombatStart();
        hooksB?.PlayCombatStart();

        isAuto  = false;
        current = head;
        Restore(head);
        beginRoutine = null;
    }

    private IEnumerator AutoRoutine()
    {
        while (isAuto && current != null && current.Next != null)
        {
            yield return StartCoroutine(ForwardRoutine());
            if (!isAuto) break;
            yield return new WaitForSeconds(betweenTurnsSeconds / Speed);
        }
        isAuto      = false;
        autoRoutine = null;
        Publish();
    }

    private IEnumerator ForwardRoutine()
    {
        if (current == null || current.Next == null) yield break;

        busy = true;
        Publish();

        var target = current.Next;
        var t      = target.Turn;
        bool attackerIsSelf = t.AttackerIsA == activeRecord.SelfWasA;
        var attacker = attackerIsSelf ? CombatVisualSide.A : CombatVisualSide.B;
        var defender = attackerIsSelf ? CombatVisualSide.B : CombatVisualSide.A;

        CombatVisualEvents.TurnStart(t);

        if (t.Procs != null)
            foreach (var pe in t.Procs)
                if (pe.BeforeStrike) yield return StartCoroutine(PlayProc(pe));

        if (!t.NoAttack)
        {
            CombatVisualEvents.Attack(attacker);
            target.FireWindup(hooksA, hooksB);
            yield return new WaitForSeconds(windupSeconds / Speed);

            var hit = new CombatVisualHit { Attacker = attacker, Defender = defender, Damage = t.Damage, Crit = t.WasCrit };
            CombatVisualEvents.Hit(hit);
            target.FireImpact(hooksA, hooksB, hpMaxA, hpMaxB);
            if (t.WasCrit)
            {
                CombatVisualEvents.Crit(hit);
                CombatVisualEvents.Log($"¡CRÍTICO! {t.AttackerName} → {t.DefenderName} · {t.Damage:F0} de daño");
            }
            else
            {
                CombatVisualEvents.Log($"{t.AttackerName} golpea a {t.DefenderName} · {t.Damage:F0} de daño");
            }

            float defMax = defender == CombatVisualSide.A ? hpMaxA : hpMaxB;
            PushHp(defender, t.DefenderHpAfter, defMax);
            if (t.Damage >= 0.5f)
                CombatVisualEvents.Popup(new CombatVisualPopup
                {
                    Side = defender, Position = FighterPos(defender),
                    Kind = t.WasCrit ? CombatPopupKind.Crit : CombatPopupKind.Hit, Amount = t.Damage,
                });
            yield return new WaitForSeconds(impactSeconds / Speed);
        }

        if (t.Procs != null)
            foreach (var pe in t.Procs)
                if (!pe.BeforeStrike) yield return StartCoroutine(PlayProc(pe));

        current = target;
        Publish();

        if (target.ADiedHere || target.BDiedHere)
        {
            target.FireDeath(hooksA, hooksB);
            if (target.ADiedHere) CombatVisualEvents.Dead(CombatVisualSide.A);
            if (target.BDiedHere) CombatVisualEvents.Dead(CombatVisualSide.B);
            yield return new WaitForSeconds(deathPauseSeconds / Speed);
            if (target.ADiedHere) SetFighterActive(CombatVisualSide.A, false);
            if (target.BDiedHere) SetFighterActive(CombatVisualSide.B, false);
        }

        CombatVisualEvents.TurnEnd(t);

        if (current.IsEnd)
        {
            CombatVisualEvents.VisualCombatEnd(endWinner, endIsDraw);
            if (!endIsDraw) (endWinner == CombatVisualSide.A ? hooksA : hooksB)?.PlayVictory();
        }

        busy       = false;
        fwdRoutine = null;
        Publish();
    }

    private IEnumerator PlayProc(CombatProcEvent pe)
    {
        var side = SimToVisual(pe.TargetIsA);
        float before = side == CombatVisualSide.A ? shownHpA : shownHpB;
        float max    = side == CombatVisualSide.A ? hpMaxA : hpMaxB;
        RaiseProcPopup(pe, side, pe.TargetHpAfter - before);
        PushHp(side, pe.TargetHpAfter, max);
        yield return new WaitForSeconds(impactSeconds / Speed);
    }

    private CombatVisualSide SimToVisual(bool simIsA)
        => simIsA == activeRecord.SelfWasA ? CombatVisualSide.A : CombatVisualSide.B;

    private void RaiseProcPopup(CombatProcEvent pe, CombatVisualSide side, float delta)
    {
        if (pe.Kind == ModifierEffectKind.Stun)
        {
            CombatVisualEvents.Popup(new CombatVisualPopup
            {
                Side = side, Position = FighterPos(side),
                Kind = CombatPopupKind.Stun, Amount = pe.Amount,
            });
            return;
        }
        if (pe.Kind == ModifierEffectKind.Synergy && Mathf.Abs(delta) < 0.5f)
        {
            CombatVisualEvents.Popup(new CombatVisualPopup
            {
                Side = side, Position = FighterPos(side),
                Kind = CombatPopupKind.Synergy, Amount = 0f,
            });
            return;
        }
        if (Mathf.Abs(delta) < 0.5f) return;
        CombatVisualEvents.Popup(new CombatVisualPopup
        {
            Side = side, Position = FighterPos(side),
            Kind = ProcPopupKind(pe.Kind), Amount = Mathf.Abs(delta),
        });
    }

    private static CombatPopupKind ProcPopupKind(ModifierEffectKind k) => k switch
    {
        ModifierEffectKind.Poison       => CombatPopupKind.Poison,
        ModifierEffectKind.Burn         => CombatPopupKind.Burn,
        ModifierEffectKind.ReturnDamage => CombatPopupKind.Thorns,
        ModifierEffectKind.Heal         => CombatPopupKind.Heal,
        ModifierEffectKind.Regen        => CombatPopupKind.Regen,
        ModifierEffectKind.Stun         => CombatPopupKind.Stun,
        ModifierEffectKind.Synergy      => CombatPopupKind.Synergy,
        _                               => CombatPopupKind.Hit,
    };

    private Vector3 FighterPos(CombatVisualSide side)
    {
        var inst = side == CombatVisualSide.A ? instanceA : instanceB;
        if (inst != null) return inst.transform.position;
        var slot = side == CombatVisualSide.A ? slotA : slotB;
        return slot != null ? slot.position : Vector3.zero;
    }

    private static string ProcText(CombatProcEvent pe, string who, float delta)
    {
        if (Mathf.Approximately(delta, 0f))
        {
            return pe.Kind switch
            {
                ModifierEffectKind.Poison => $"{who} es envenenado",
                ModifierEffectKind.Burn   => $"{who} se incendia",
                ModifierEffectKind.Regen  => $"{who} entra en regeneración",
                ModifierEffectKind.Stun   => $"{who} queda aturdido {pe.Amount:F0}t",
                _                         => $"{who} — {pe.Kind}",
            };
        }
        string mag = $"{Mathf.Abs(delta):F0}";
        if (delta < 0f)
        {
            return pe.Kind switch
            {
                ModifierEffectKind.Poison       => $"{who} sufre {Colored(mag, DamageColor)} de veneno",
                ModifierEffectKind.Burn         => $"{who} sufre {Colored(mag, DamageColor)} de quemadura",
                ModifierEffectKind.ReturnDamage => $"{who} recibe {Colored(mag, DamageColor)} de espinas",
                _                               => $"{who} pierde {Colored(mag, DamageColor)}",
            };
        }
        return pe.Kind switch
        {
            ModifierEffectKind.Regen => $"{who} regenera {mag}",
            ModifierEffectKind.Heal  => $"{who} se cura {mag}",
            _                        => $"{who} recupera {mag}",
        };
    }

    private void Restore(CombatNode node)
    {
        if (node == null) return;
        current = node;

        SetFighterActive(CombatVisualSide.A, !node.ADead);
        SetFighterActive(CombatVisualSide.B, !node.BDead);

        RestoreAnim(animA, node.ADead);
        RestoreAnim(animB, node.BDead);

        PushHp(CombatVisualSide.A, node.HpA, hpMaxA);
        PushHp(CombatVisualSide.B, node.HpB, hpMaxB);

        if (node.IsEnd)
            CombatVisualEvents.VisualCombatEnd(endWinner, endIsDraw);

        Publish();
    }

    private static void RestoreAnim(MoriMonchiProceduralAnimator anim, bool dead)
    {
        if (anim == null) return;
        if (dead) anim.AnimDeath(); else anim.AnimIdle();
    }

    private void Publish()
    {
        if (current == null) return;
        CombatVisualEvents.PanelState(new CombatVisualPanelState
        {
            TurnNumber = current.TurnNumber,
            TotalTurns = totalTurns,
            Log        = current.Log.ToArray(),
            Ended      = current.IsEnd,
            IsDraw     = endIsDraw,
            Winner     = endWinner,
            IsAuto     = isAuto,
            CanForward = CanForward,
            CanBack    = CanBack,
            Speed      = playbackSpeed,
        });
    }

    private void SpawnFighters(CreatureDNA dnaA, CreatureDNA dnaB)
    {
        DespawnFighters();
        instanceA = SpawnOne(dnaA, slotA, out barA);
        instanceB = SpawnOne(dnaB, slotB, out barB);
        hooksA = instanceA as MoriMonchiCombatVisualizer;
        hooksB = instanceB as MoriMonchiCombatVisualizer;
        animA  = instanceA != null ? instanceA.GetComponent<MoriMonchiProceduralAnimator>() : null;
        animB  = instanceB != null ? instanceB.GetComponent<MoriMonchiProceduralAnimator>() : null;
    }

    private MoriMonchiVisualizer SpawnOne(CreatureDNA dna, Transform slot, out MoriMonchiCombatVisualizerUITK bar)
    {
        var inst = Instantiate(visualizerPrefab, slot.position, slot.rotation, slot);
        bar = inst.GetComponentInChildren<MoriMonchiCombatVisualizerUITK>(true);
        inst.SetFurDatabase(GameManager.Instance.FurTypeDatabase);
        inst.Assemble(dna, GameManager.Instance.PartVisualBank);
        return inst;
    }

    private void PushHp(CombatVisualSide side, float hp, float max)
    {
        CombatVisualEvents.HpChanged(side, hp, max);
        var bar = side == CombatVisualSide.A ? barA : barB;
        if (bar != null) bar.SetHp(hp, max);
        if (side == CombatVisualSide.A) shownHpA = hp; else shownHpB = hp;
    }

    private void SetFighterActive(CombatVisualSide side, bool active)
    {
        var inst = side == CombatVisualSide.A ? instanceA : instanceB;
        if (inst != null && inst.gameObject.activeSelf != active) inst.gameObject.SetActive(active);
    }

    private void DespawnFighters()
    {
        if (instanceA != null) Destroy(instanceA.gameObject);
        if (instanceB != null) Destroy(instanceB.gameObject);
        instanceA = null;
        instanceB = null;
        hooksA    = null;
        hooksB    = null;
        barA      = null;
        barB      = null;
        animA     = null;
        animB     = null;
    }

    // ── DEV — Test Harness ────────────────────────────────────────

    [FoldoutGroup("DEV — Test Harness"), ShowInInspector]
    [ValueDropdown(nameof(FighterOptions)), LabelText("Combatiente A")]
    private string devCreatureA;

    [FoldoutGroup("DEV — Test Harness"), ShowInInspector]
    [ValueDropdown(nameof(FightOptions)), LabelText("Pelea (slot)")]
    private int devFightIndex;

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
        var dnaB   = ResolveOpponent(record, dnaA);
        if (dnaB == null) { Debug.LogWarning($"[CombatVisualizer] El rival \"{record.OpponentName}\" no está en el registro (¿murió/se vendió?). No puedo armar su modelo."); return; }

        Play(dnaA, dnaB, record);
    }

    [ButtonGroup("DEV — Test Harness/Controles")]
    [Button("◀ Atrás"), DisableInEditorMode]
    private void DevBack() => Back();

    [ButtonGroup("DEV — Test Harness/Controles")]
    [Button("▶/❚❚"), DisableInEditorMode]
    private void DevTogglePlay() => TogglePlay();

    [ButtonGroup("DEV — Test Harness/Controles")]
    [Button("▶▶ Adelante"), DisableInEditorMode]
    private void DevNext() => Next();

    private static bool HasTurns(CombatRecord r) => r != null && r.Turns != null && r.Turns.Count > 0;

    private CreatureDNA ResolveDna(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        var reg = GameManager.Instance != null ? GameManager.Instance.Registry : null;
        return reg != null && reg.TryGet(id, out var dna) ? dna : null;
    }

    private CreatureDNA ResolveOpponent(CombatRecord record, CreatureDNA self)
    {
        var reg = GameManager.Instance != null ? GameManager.Instance.Registry : null;
        if (reg == null) return null;
        foreach (var dna in reg.GetAll().Values)
            if (dna != null && dna != self && dna.CustomName == record.OpponentName) return dna;
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
}
}
