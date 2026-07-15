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

    [Title("Boards")]
    [Required, SerializeField] private Transform boardA;
    [Required, SerializeField] private Transform boardB;

    [Title("Elemental")]
    [SerializeField] private ElementTableSO elementTable;

    [Title("Timing (seconds, divided by speed)")]
    [SerializeField, MinValue(0f)] private float windupSeconds       = 0.35f;
    [SerializeField, MinValue(0f)] private float impactSeconds       = 0.35f;
    [SerializeField, Range(0f, 1f)] private float lungeFraction      = 0.6f;
    [SerializeField, MinValue(0f)] private float betweenTurnsSeconds = 0.55f;
    [SerializeField, MinValue(0f)] private float deathPauseSeconds   = 0.6f;
    [SerializeField, MinValue(0f)] private float synergyPopupDelay   = 0.6f;
    [SerializeField, MinValue(0f)] private float stateBeatSeconds    = 0.5f;

    [Title("Speech (frases tweakeables)")]
    [SerializeField] private string protectorLine = "¡Toma, protégete!";
    [SerializeField] private string empaticoLine  = "¡{0}, te curo!";
    [SerializeField] private string agresivoLine  = "¡Te comparto mi espíritu de pelea!";
    [SerializeField] private string protectorSelfLine = "¡Me escudaré!";
    [SerializeField] private string empaticoSelfLine  = "¡Qué alivio!";
    [SerializeField] private string agresivoSelfLine  = "¡Me toca a mí!";
    [SerializeField] private string attackLine    = "¡TOMA, {0}!";
    [SerializeField, MinValue(0.2f)] private float speechSeconds = 1.6f;

    [Title("Playback")]
    [SerializeField, Range(0.25f, 4f)] private float playbackSpeed = 1f;

    private const string SelfColor   = "5AA0FF";
    private const string OppColor    = "FF6B6B";
    private const string DamageColor = "FF3B3B";

    private class CombatNode
    {
        public bool                      HasTurn;
        public CombatTurn                Turn;
        public int                       TurnNumber;
        public int                       ActionIndex;
        public List<CombatUnitState>     StateA;
        public List<CombatUnitState>     StateB;
        public bool[]                    DiedHereA;
        public bool[]                    DiedHereB;
        public CombatVisualSide          AttackerSide;
        public int                       AttackerIndex;
        public int                       DefenderIndex;
        public bool                      Crit;
        public List<CombatVisualLogLine> Log;
        public CombatNode                Prev;
        public CombatNode                Next;
        public bool IsEnd => Next == null;
    }

    private readonly CombatVisualUnits units = new CombatVisualUnits();
    private List<CreatureDNA> teamSelf;
    private List<CreatureDNA> teamOpp;

    private CombatRecord activeRecord;
    private CombatNode   head;
    private CombatNode   current;
    private int          totalTurns;
    private bool         endIsDraw;
    private CombatVisualSide endWinner;

    private readonly Dictionary<int, List<(CombatVisualSide Side, int Index)>> roundOrders = new Dictionary<int, List<(CombatVisualSide Side, int Index)>>();

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

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void Play(CreatureDNA self, CombatRecord record)
    {
        if (self == null || record == null)
        {
            Debug.LogError("[CombatVisualizer] Play called with null DNA or record.");
            return;
        }
        if (GameManager.Instance == null)
        {
            Debug.LogError("[CombatVisualizer] No GameManager in scene — cannot resolve visual databases.");
            return;
        }
        if (record.SelfTeam == null || record.SelfTeamIds == null || record.OpponentTeamIds == null)
        {
            Debug.LogWarning("[CombatVisualizer] record sin datos 3v3");
            return;
        }

        var reg = GameManager.Instance.Registry;
        var resolvedSelf = new List<CreatureDNA>();
        foreach (var id in record.SelfTeamIds)
        {
            if (!reg.TryGet(id, out var dna))
            { Debug.LogWarning($"[CombatVisualizer] No encontré al MoriMochi \"{id}\" del equipo propio."); return; }
            resolvedSelf.Add(dna);
        }
        var resolvedOpp = new List<CreatureDNA>();
        foreach (var id in record.OpponentTeamIds)
        {
            if (!reg.TryGet(id, out var dna))
            { Debug.LogWarning($"[CombatVisualizer] No encontré al MoriMochi \"{id}\" del equipo rival."); return; }
            resolvedOpp.Add(dna);
        }

        Stop();
        activeRecord = record;
        teamSelf     = resolvedSelf;
        teamOpp      = resolvedOpp;
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
        units.DespawnAll();
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

    public Unity.Cinemachine.CinemachineCamera VCamOf(CombatVisualSide side, int index) => units.Get(side, index)?.VCam;

    private static List<CombatUnitState> BuildInitialState(List<CombatFighterSnapshot> snaps)
    {
        var list = new List<CombatUnitState>(snaps.Count);
        foreach (var s in snaps) list.Add(new CombatUnitState { Hp = s.MaxHp, Shield = 0f });
        return list;
    }

    private static string NamesColored(List<CombatFighterSnapshot> team, string hex)
        => Colored(string.Join(" · ", team.Select(s => s.Name)), hex);

    private void BuildStates()
    {
        var snapsA = activeRecord.SelfTeam;
        var snapsB = activeRecord.OpponentTeam;

        var log = new List<CombatVisualLogLine>
        {
            Line(CombatVisualLogKind.Versus, $"{NamesColored(snapsA, SelfColor)}   VS   {NamesColored(snapsB, OppColor)}"),
        };

        head = new CombatNode
        {
            HasTurn    = false,
            StateA     = BuildInitialState(snapsA),
            StateB     = BuildInitialState(snapsB),
            DiedHereA  = new bool[snapsA.Count],
            DiedHereB  = new bool[snapsB.Count],
            TurnNumber = 0,
            Log        = new List<CombatVisualLogLine>(log),
        };
        var node = head;
        totalTurns = 0;
        roundOrders.Clear();

        var aliveA = new bool[snapsA.Count];
        var aliveB = new bool[snapsB.Count];
        for (int i = 0; i < aliveA.Length; i++) aliveA[i] = true;
        for (int i = 0; i < aliveB.Length; i++) aliveB[i] = true;

        var hpA = new float[snapsA.Count];
        var hpB = new float[snapsB.Count];
        for (int i = 0; i < hpA.Length; i++) hpA[i] = snapsA[i].MaxHp;
        for (int i = 0; i < hpB.Length; i++) hpB[i] = snapsB[i].MaxHp;

        if (activeRecord.Turns != null)
        {
            foreach (var t in activeRecord.Turns)
            {
                bool attackerIsSelf = t.AttackerIsA == activeRecord.SelfWasA;
                var attackerSide    = attackerIsSelf ? CombatVisualSide.A : CombatVisualSide.B;
                var defenderSide    = attackerIsSelf ? CombatVisualSide.B : CombatVisualSide.A;

                if (!roundOrders.TryGetValue(t.TurnNumber, out var ro))
                {
                    ro = new List<(CombatVisualSide Side, int Index)>();
                    roundOrders[t.TurnNumber] = ro;
                }
                if (!ro.Contains((attackerSide, t.AttackerIndex))) ro.Add((attackerSide, t.AttackerIndex));

                void ApplyProc(CombatProcEvent pe)
                {
                    bool  isA   = pe.TargetIsA == activeRecord.SelfWasA;
                    var   snaps = isA ? snapsA : snapsB;
                    var   hpArr = isA ? hpA : hpB;
                    float before = hpArr[pe.TargetIndex];
                    float after  = pe.TargetHpAfter;
                    hpArr[pe.TargetIndex] = after;

                    string who  = Colored(snaps[pe.TargetIndex].Name, isA ? SelfColor : OppColor);
                    string text = pe.ElementEvent == ElementEventKind.None
                        ? ProcText(pe, who, after - before)
                        : ElementText(pe, who, after - before);
                    if (text != null) log.Add(Line(CombatVisualLogKind.Proc, text));
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

                    if (defenderSide == CombatVisualSide.A) hpA[t.DefenderIndex] = t.DefenderHpAfter;
                    else                                    hpB[t.DefenderIndex] = t.DefenderHpAfter;
                }

                if (t.Procs != null)
                    foreach (var pe in t.Procs)
                        if (!pe.BeforeStrike) ApplyProc(pe);

                var stateA = activeRecord.SelfWasA ? t.TeamStateA : t.TeamStateB;
                var stateB = activeRecord.SelfWasA ? t.TeamStateB : t.TeamStateA;

                var diedHereA = new bool[stateA.Count];
                var diedHereB = new bool[stateB.Count];
                for (int i = 0; i < stateA.Count; i++)
                    if (stateA[i].Hp <= 0f && aliveA[i])
                    {
                        diedHereA[i] = true; aliveA[i] = false;
                        log.Add(Line(CombatVisualLogKind.Death, $"{Colored(snapsA[i].Name, SelfColor)} cae derrotado"));
                    }
                for (int i = 0; i < stateB.Count; i++)
                    if (stateB[i].Hp <= 0f && aliveB[i])
                    {
                        diedHereB[i] = true; aliveB[i] = false;
                        log.Add(Line(CombatVisualLogKind.Death, $"{Colored(snapsB[i].Name, OppColor)} cae derrotado"));
                    }

                totalTurns++;
                var next = new CombatNode
                {
                    HasTurn       = true, Turn = t,
                    StateA        = stateA, StateB = stateB,
                    DiedHereA     = diedHereA, DiedHereB = diedHereB,
                    TurnNumber    = t.TurnNumber, ActionIndex = totalTurns, Log = new List<CombatVisualLogLine>(log),
                    AttackerSide  = attackerSide,
                    AttackerIndex = t.AttackerIndex,
                    DefenderIndex = t.DefenderIndex,
                    Crit          = t.WasCrit,
                };
                node.Next = next; next.Prev = node; node = next;
            }
        }

        endIsDraw = activeRecord.Outcome == CombatOutcome.Draw;
        endWinner = activeRecord.Outcome == CombatOutcome.Won ? CombatVisualSide.A : CombatVisualSide.B;
        node.Log.Add(endIsDraw
            ? Line(CombatVisualLogKind.Result, "Empate")
            : Line(CombatVisualLogKind.Result, endWinner == CombatVisualSide.A
                ? Colored("Ganador: tu equipo", SelfColor)
                : Colored("Ganador: equipo rival", OppColor)));

        current = head;
    }

    private static CombatVisualLogLine Line(CombatVisualLogKind kind, string text)
        => new CombatVisualLogLine { Kind = kind, Text = text };

    private static string Colored(string text, string hex) => $"<color=#{hex}>{text}</color>";

    private IEnumerator BeginRoutine()
    {
        units.Spawn(CombatVisualSide.A, teamSelf, activeRecord.SelfTeam, boardA, visualizerPrefab);
        units.Spawn(CombatVisualSide.B, teamOpp, activeRecord.OpponentTeam, boardB, visualizerPrefab);
        yield return null;
        yield return null;

        var ctx = new CombatVisualContext
        {
            DnaA = teamSelf[0], DnaB = teamOpp[0],
            HpMaxA = activeRecord.SelfTeam[0].MaxHp, HpMaxB = activeRecord.OpponentTeam[0].MaxHp,
            SlotA = boardA, SlotB = boardB,
            TotalTurns = totalTurns,
            TeamA = teamSelf, TeamB = teamOpp,
            HpMaxTeamA = activeRecord.SelfTeam.Select(s => s.MaxHp).ToArray(),
            HpMaxTeamB = activeRecord.OpponentTeam.Select(s => s.MaxHp).ToArray(),
            SnapsA = activeRecord.SelfTeam, SnapsB = activeRecord.OpponentTeam,
        };
        CombatVisualEvents.VisualCombatStart(ctx);

        foreach (var unit in units.Team(CombatVisualSide.A)) unit.Hooks?.PlayCombatStart();
        foreach (var unit in units.Team(CombatVisualSide.B)) unit.Hooks?.PlayCombatStart();

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

        var atkUnit = units.Get(target.AttackerSide, target.AttackerIndex);
        var defSide = target.AttackerSide == CombatVisualSide.A ? CombatVisualSide.B : CombatVisualSide.A;
        var defUnit = units.Get(defSide, target.DefenderIndex);

        CombatVisualEvents.ActiveUnit(target.AttackerSide, target.AttackerIndex);
        SetActiveFrames(target.AttackerSide, target.AttackerIndex);
        CombatVisualEvents.TurnStart(t);
        if (!t.NoAttack) defUnit?.Bar?.SetTargeted(true);

        var atkHome = atkUnit?.Anchor != null ? atkUnit.Anchor.position
                    : atkUnit?.Instance != null ? atkUnit.Instance.transform.position : Vector3.zero;

        if (t.Procs != null)
            foreach (var pe in t.Procs)
                if (pe.BeforeStrike) yield return StartCoroutine(PlayProc(pe, target));

        if (t.Procs != null)
        {
            var passiveProcs = t.Procs.Where(pe => !pe.BeforeStrike && pe.PassivePhase).ToList();
            int gi = 0;
            while (gi < passiveProcs.Count)
            {
                var groupSide  = SimToVisual(passiveProcs[gi].TargetIsA);
                var groupIndex = passiveProcs[gi].TargetIndex;
                var group = new List<CombatProcEvent> { passiveProcs[gi] };
                int gj = gi + 1;
                while (gj < passiveProcs.Count && SimToVisual(passiveProcs[gj].TargetIsA) == groupSide && passiveProcs[gj].TargetIndex == groupIndex)
                {
                    group.Add(passiveProcs[gj]);
                    gj++;
                }
                gi = gj;

                bool isSelf = groupSide == target.AttackerSide && groupIndex == target.AttackerIndex;
                if (isSelf)
                {
                    foreach (var pe in group) yield return StartCoroutine(PlayProc(pe, target));
                }
                else
                {
                    var dest = atkHome + (units.PosOf(groupSide, groupIndex) - atkHome) * lungeFraction;
                    if (atkUnit?.Instance != null)
                        yield return StartCoroutine(MoveOverTime(atkUnit.Instance.transform, atkHome, dest, windupSeconds / Speed));
                    else
                        yield return new WaitForSeconds(windupSeconds / Speed);

                    foreach (var pe in group) yield return StartCoroutine(PlayProc(pe, target));

                    if (atkUnit?.Instance != null)
                        yield return StartCoroutine(MoveOverTime(atkUnit.Instance.transform, dest, atkHome, impactSeconds / Speed));
                    else
                        yield return new WaitForSeconds(impactSeconds / Speed);
                }
            }
        }

        if (!t.NoAttack)
        {
            CombatVisualEvents.Attack(target.AttackerSide);
            atkUnit?.Hooks?.PlayAttack();

            EmitSpeech(target.AttackerSide, target.AttackerIndex, string.Format(attackLine, t.DefenderName), defSide, target.DefenderIndex, true);

            var lungePos = atkHome + (units.PosOf(defSide, target.DefenderIndex) - atkHome) * lungeFraction;
            if (atkUnit?.Instance != null)
                yield return StartCoroutine(MoveOverTime(atkUnit.Instance.transform, atkHome, lungePos, windupSeconds / Speed));
            else
                yield return new WaitForSeconds(windupSeconds / Speed);

            var hit = new CombatVisualHit
            {
                Attacker = target.AttackerSide, Defender = defSide, Damage = t.Damage, Crit = t.WasCrit,
                AttackerIndex = target.AttackerIndex, DefenderIndex = target.DefenderIndex,
            };
            CombatVisualEvents.Hit(hit);
            if (t.WasCrit) { atkUnit?.Hooks?.PlayCritDealt(); defUnit?.Hooks?.PlayCritTaken(); }
            else            { atkUnit?.Hooks?.PlayHitDealt();  defUnit?.Hooks?.PlayHitTaken(); }
            defUnit?.Hooks?.PlayHpChanged(t.DefenderHpAfter, defUnit.MaxHp);
            if (t.WasCrit)
            {
                CombatVisualEvents.Crit(hit);
                CombatVisualEvents.Log($"¡CRÍTICO! {t.AttackerName} → {t.DefenderName} · {t.Damage:F0} de daño");
            }
            else
            {
                CombatVisualEvents.Log($"{t.AttackerName} golpea a {t.DefenderName} · {t.Damage:F0} de daño");
            }

            PushHp(defSide, target.DefenderIndex, t.DefenderHpAfter);
            PushShield(defSide, target.DefenderIndex, t.DefenderShieldAfter);
            if (t.Damage >= 0.5f)
                CombatVisualEvents.Popup(new CombatVisualPopup
                {
                    Side = defSide, Position = units.PosOf(defSide, target.DefenderIndex), Follow = units.TransformOf(defSide, target.DefenderIndex),
                    Kind = t.WasCrit ? CombatPopupKind.Crit : CombatPopupKind.Hit, Amount = t.Damage,
                });
            if (atkUnit?.Instance != null)
                yield return StartCoroutine(MoveOverTime(atkUnit.Instance.transform, lungePos, atkHome, impactSeconds / Speed));
            else
                yield return new WaitForSeconds(impactSeconds / Speed);
            defUnit?.Bar?.SetTargeted(false);
        }

        if (t.Procs != null)
            foreach (var pe in t.Procs)
                if (!pe.BeforeStrike && !pe.PassivePhase) yield return StartCoroutine(PlayProc(pe, target));

        current = target;
        PushShieldAll(target);
        Publish();

        bool anyDied = false;
        for (int i = 0; i < target.DiedHereA.Length; i++)
            if (target.DiedHereA[i])
            {
                anyDied = true;
                units.Get(CombatVisualSide.A, i)?.Hooks?.PlayDead();
                CombatVisualEvents.Dead(CombatVisualSide.A);
                CombatVisualEvents.UnitDead(CombatVisualSide.A, i);
            }
        for (int i = 0; i < target.DiedHereB.Length; i++)
            if (target.DiedHereB[i])
            {
                anyDied = true;
                units.Get(CombatVisualSide.B, i)?.Hooks?.PlayDead();
                CombatVisualEvents.Dead(CombatVisualSide.B);
                CombatVisualEvents.UnitDead(CombatVisualSide.B, i);
            }

        if (anyDied)
        {
            yield return new WaitForSeconds(deathPauseSeconds / Speed);
            for (int i = 0; i < target.DiedHereA.Length; i++)
                if (target.DiedHereA[i]) units.SetActive(units.Get(CombatVisualSide.A, i), false);
            for (int i = 0; i < target.DiedHereB.Length; i++)
                if (target.DiedHereB[i]) units.SetActive(units.Get(CombatVisualSide.B, i), false);
        }

        CombatVisualEvents.TurnEnd(t);

        if (current.IsEnd)
        {
            CombatVisualEvents.VisualCombatEnd(endWinner, endIsDraw);
            if (!endIsDraw)
            {
                var winnerState = endWinner == CombatVisualSide.A ? current.StateA : current.StateB;
                for (int i = 0; i < winnerState.Count; i++)
                    if (winnerState[i].Hp > 0f) { units.Get(endWinner, i)?.Hooks?.PlayVictory(); break; }
            }
        }

        busy       = false;
        fwdRoutine = null;
        Publish();
        PublishOrder();
    }

    private IEnumerator MoveOverTime(Transform tr, Vector3 from, Vector3 to, float duration)
    {
        if (tr == null) yield break;
        if (duration <= 0f) { tr.position = to; yield break; }
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (tr == null) yield break;
            elapsed += Time.deltaTime;
            tr.position = Vector3.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        if (tr != null) tr.position = to;
    }

    private IEnumerator PlayProc(CombatProcEvent pe, CombatNode node)
    {
        var side = SimToVisual(pe.TargetIsA);
        var unit = units.Get(side, pe.TargetIndex);
        if (unit == null) yield break;
        float delta = pe.TargetHpAfter - unit.ShownHp;

        if (pe.ElementEvent == ElementEventKind.Reaction)
        {
            yield return new WaitForSeconds(synergyPopupDelay / Speed);
            CombatVisualEvents.Popup(new CombatVisualPopup
            {
                Side = side, Position = units.PosOf(side, pe.TargetIndex), Follow = units.TransformOf(side, pe.TargetIndex),
                Kind = CombatPopupKind.Reaction, Amount = 0f,
                Text = pe.ReactionName,
                HasOverrideColor = elementTable != null,
                OverrideColor = elementTable != null ? elementTable.GetIdentity(pe.Element).UiColor : Color.white,
            });
        }
        else if (pe.ElementEvent != ElementEventKind.None)
        {
            if (Mathf.Abs(delta) >= 0.5f)
                CombatVisualEvents.Popup(new CombatVisualPopup
                {
                    Side = side, Position = units.PosOf(side, pe.TargetIndex), Follow = units.TransformOf(side, pe.TargetIndex),
                    Kind = delta > 0f ? CombatPopupKind.Heal : CombatPopupKind.Hit, Amount = Mathf.Abs(delta),
                });
        }
        else
        {
            RaiseProcPopup(pe, unit, delta);
        }

        bool esSelf = side == node.AttackerSide && pe.TargetIndex == node.AttackerIndex;

        if (pe.ElementEvent == ElementEventKind.None && pe.Kind == ModifierEffectKind.Shield)
        {
            if (esSelf)
                EmitSpeech(node.AttackerSide, node.AttackerIndex, protectorSelfLine, side, pe.TargetIndex, false);
            else
                EmitSpeech(node.AttackerSide, node.AttackerIndex, string.Format(protectorLine, SnapName(side, pe.TargetIndex)), side, pe.TargetIndex, true);
        }
        else if (pe.ElementEvent == ElementEventKind.None && pe.Kind == ModifierEffectKind.Heal && !pe.BeforeStrike)
        {
            if (esSelf)
                EmitSpeech(node.AttackerSide, node.AttackerIndex, empaticoSelfLine, side, pe.TargetIndex, false);
            else
                EmitSpeech(node.AttackerSide, node.AttackerIndex, string.Format(empaticoLine, SnapName(side, pe.TargetIndex)), side, pe.TargetIndex, true);
        }
        else if (pe.ElementEvent == ElementEventKind.MarkApplied && pe.AllySource && pe.PassivePhase
              && SnapRole(node.AttackerSide, node.AttackerIndex) == Role.Agresivo && esSelf)
            EmitSpeech(node.AttackerSide, node.AttackerIndex, agresivoSelfLine, side, pe.TargetIndex, false);
        else if (pe.ElementEvent == ElementEventKind.MarkApplied && pe.AllySource
              && SnapRole(node.AttackerSide, node.AttackerIndex) == Role.Agresivo
              && !esSelf)
            EmitSpeech(node.AttackerSide, node.AttackerIndex, string.Format(agresivoLine, SnapName(side, pe.TargetIndex)), side, pe.TargetIndex, true);
        else if (pe.ElementEvent == ElementEventKind.StateArmed)
            EmitSpeech(side, pe.TargetIndex, $"¡Quedé {StateName(pe.State)}!", side, pe.TargetIndex, false);

        PushHp(side, pe.TargetIndex, pe.TargetHpAfter);

        if (pe.ElementEvent == ElementEventKind.AffinityGained) CombatVisualEvents.UnitAffinity(side, pe.TargetIndex, (int)pe.Amount);


        if (pe.ElementEvent == ElementEventKind.MarkApplied
         || pe.ElementEvent == ElementEventKind.MarkRemoved
         || pe.ElementEvent == ElementEventKind.Reaction
         || pe.ElementEvent == ElementEventKind.StateArmed
         || pe.ElementEvent == ElementEventKind.StateConsumed
         || pe.ElementEvent == ElementEventKind.StateRemoved)
            CombatVisualEvents.UnitElement(new CombatElementEventData
            {
                Side = side, Index = pe.TargetIndex, Kind = pe.ElementEvent,
                Element = pe.Element, ElementB = pe.ElementB, AllySource = pe.AllySource, State = pe.State,
                ReactionName = pe.ReactionName,
            });

        bool beat = pe.ElementEvent == ElementEventKind.Reaction
                 || pe.ElementEvent == ElementEventKind.StateArmed
                 || pe.ElementEvent == ElementEventKind.StateConsumed
                 || pe.ElementEvent == ElementEventKind.MarkApplied
                 || (pe.ElementEvent == ElementEventKind.AffinityGained && pe.Amount >= 2f)
                 || (pe.ElementEvent == ElementEventKind.None && (pe.Kind == ModifierEffectKind.Shield || pe.Kind == ModifierEffectKind.Heal));
        if (beat) yield return new WaitForSeconds(stateBeatSeconds / Speed);

        yield return new WaitForSeconds(impactSeconds / Speed);
    }

    private CombatVisualSide SimToVisual(bool simIsA)
        => simIsA == activeRecord.SelfWasA ? CombatVisualSide.A : CombatVisualSide.B;

    private void EmitSpeech(CombatVisualSide speakerSide, int speakerIndex, string text, CombatVisualSide targetSide, int targetIndex, bool hasTarget)
    {
        var follow = units.TransformOf(speakerSide, speakerIndex);
        if (follow == null) return;
        CombatVisualEvents.Speech(new CombatSpeechData
        {
            Side         = speakerSide,
            Index        = speakerIndex,
            Text         = text,
            HasColor     = false,
            Duration     = speechSeconds / Speed,
            Follow       = follow,
            HasTarget    = hasTarget,
            TargetSide   = targetSide,
            TargetIndex  = targetIndex,
            TargetFollow = hasTarget ? units.TransformOf(targetSide, targetIndex) : null,
        });
    }

    private string SnapName(CombatVisualSide side, int index)
    {
        var team = side == CombatVisualSide.A ? activeRecord.SelfTeam : activeRecord.OpponentTeam;
        return team != null && index >= 0 && index < team.Count ? team[index].Name : "";
    }

    public Vector3 PosOf(CombatVisualSide side, int index) => units.PosOf(side, index);

    private Role SnapRole(CombatVisualSide side, int index)
    {
        var team = side == CombatVisualSide.A ? activeRecord.SelfTeam : activeRecord.OpponentTeam;
        return team != null && index >= 0 && index < team.Count ? team[index].Role : default;
    }

    private void RaiseProcPopup(CombatProcEvent pe, CombatVisualUnit unit, float delta)
    {
        if (pe.Kind == ModifierEffectKind.Shield)
        {
            CombatVisualEvents.Popup(new CombatVisualPopup
            {
                Side = unit.Side, Position = units.PosOf(unit.Side, unit.Index), Follow = units.TransformOf(unit.Side, unit.Index),
                Kind = CombatPopupKind.Shield, Amount = pe.Amount,
            });
            return;
        }
        if (pe.Kind == ModifierEffectKind.Stun)
        {
            CombatVisualEvents.Popup(new CombatVisualPopup
            {
                Side = unit.Side, Position = units.PosOf(unit.Side, unit.Index), Follow = units.TransformOf(unit.Side, unit.Index),
                Kind = CombatPopupKind.Stun, Amount = pe.Amount,
            });
            return;
        }
        if (pe.Kind == ModifierEffectKind.Synergy && Mathf.Abs(delta) < 0.5f)
        {
            CombatVisualEvents.Popup(new CombatVisualPopup
            {
                Side = unit.Side, Position = units.PosOf(unit.Side, unit.Index), Follow = units.TransformOf(unit.Side, unit.Index),
                Kind = CombatPopupKind.Synergy, Amount = 0f,
            });
            return;
        }
        if (Mathf.Abs(delta) < 0.5f) return;
        CombatVisualEvents.Popup(new CombatVisualPopup
        {
            Side = unit.Side, Position = units.PosOf(unit.Side, unit.Index), Follow = units.TransformOf(unit.Side, unit.Index),
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
        ModifierEffectKind.Static    => CombatPopupKind.Static,
        ModifierEffectKind.Pulse     => CombatPopupKind.Pulse,
        ModifierEffectKind.Steel     => CombatPopupKind.Steel,
        ModifierEffectKind.Mist     => CombatPopupKind.Mist,
        ModifierEffectKind.Lifesteal => CombatPopupKind.Lifesteal,
        _                               => CombatPopupKind.Hit,
    };

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
                ModifierEffectKind.Shield => $"{who} recibe escudo (+{pe.Amount:F0})",
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

    private string ElementText(CombatProcEvent pe, string who, float delta) => pe.ElementEvent switch
    {
        ElementEventKind.Reaction      => $"¡{pe.ReactionName}! sobre {who}",
        ElementEventKind.StateArmed    => $"{who} queda {StateName(pe.State)}",
        ElementEventKind.StateConsumed => $"{who} consume {StateName(pe.State)}",
        ElementEventKind.StateRemoved  => $"{who} pierde {StateName(pe.State)}",
        ElementEventKind.MarkApplied   => $"{who} recibe marca {ElemName(pe.Element)} {(pe.AllySource ? "aliada" : "enemiga")}",
        ElementEventKind.MarkRemoved   => $"{who} pierde la marca {ElemName(pe.Element)}",
        ElementEventKind.Heal          => $"{who} se cura {Mathf.Abs(delta):F0}",
        ElementEventKind.Damage        => $"{who} recibe {Colored($"{Mathf.Abs(delta):F0}", DamageColor)}",
        ElementEventKind.ShieldDoubled => $"{who} duplica su escudo",
        _                               => null,
    };

    private string ElemName(Element e)
        => elementTable != null ? elementTable.GetIdentity(e).DisplayName : e.ToString();

    private string StateName(ElementalState s)
        => elementTable != null && !string.IsNullOrEmpty(elementTable.GetState(s).DisplayName) ? elementTable.GetState(s).DisplayName : s.ToString();

    private void Restore(CombatNode node)
    {
        if (node == null) return;
        current = node;
        ClearTargetedFrames();

        for (int i = 0; i < node.StateA.Count; i++)
        {
            var unit  = units.Get(CombatVisualSide.A, i);
            var state = node.StateA[i];
            bool dead = state.Hp <= 0f;
            units.SetActive(unit, !dead);
            RestoreAnim(unit?.Anim, dead);
            PushHp(CombatVisualSide.A, i, state.Hp);
            PushShield(CombatVisualSide.A, i, state.Shield);
        }
        for (int i = 0; i < node.StateB.Count; i++)
        {
            var unit  = units.Get(CombatVisualSide.B, i);
            var state = node.StateB[i];
            bool dead = state.Hp <= 0f;
            units.SetActive(unit, !dead);
            RestoreAnim(unit?.Anim, dead);
            PushHp(CombatVisualSide.B, i, state.Hp);
            PushShield(CombatVisualSide.B, i, state.Shield);
        }

        if (node.IsEnd)
            CombatVisualEvents.VisualCombatEnd(endWinner, endIsDraw);

        Publish();
        PublishOrder();
        if (current.Next != null) { CombatVisualEvents.ActiveUnit(current.Next.AttackerSide, current.Next.AttackerIndex); SetActiveFrames(current.Next.AttackerSide, current.Next.AttackerIndex); }
        else                      { CombatVisualEvents.ActiveUnit(current.AttackerSide, current.AttackerIndex); SetActiveFrames(current.AttackerSide, current.AttackerIndex); }
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
            TurnNumber = current.ActionIndex,
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

    private void PushHp(CombatVisualSide side, int index, float hp)
    {
        var unit = units.Get(side, index);
        if (unit == null) return;
        unit.ShownHp = hp;
        unit.Bar?.SetHp(hp, unit.MaxHp);
        CombatVisualEvents.UnitHpChanged(side, index, hp, unit.MaxHp);
        CombatVisualEvents.HpChanged(side, hp, unit.MaxHp);
    }

    private void PushShield(CombatVisualSide side, int index, float shield)
        => units.Get(side, index)?.Bar?.SetShield(shield);

    private void PushShieldAll(CombatNode node)
    {
        for (int i = 0; i < node.StateA.Count; i++)
            PushShield(CombatVisualSide.A, i, node.StateA[i].Shield);
        for (int i = 0; i < node.StateB.Count; i++)
            PushShield(CombatVisualSide.B, i, node.StateB[i].Shield);
    }

    private void SetActiveFrames(CombatVisualSide side, int index)
    {
        foreach (var unit in units.Team(CombatVisualSide.A)) unit.Bar?.SetActiveTurn(unit.Side == side && unit.Index == index);
        foreach (var unit in units.Team(CombatVisualSide.B)) unit.Bar?.SetActiveTurn(unit.Side == side && unit.Index == index);
    }

    private void ClearTargetedFrames()
    {
        foreach (var unit in units.Team(CombatVisualSide.A)) unit.Bar?.SetTargeted(false);
        foreach (var unit in units.Team(CombatVisualSide.B)) unit.Bar?.SetTargeted(false);
    }

    private void PublishOrder()
    {
        if (current == null) return;

        int round = current.Next != null ? current.Next.TurnNumber : current.TurnNumber;

        var order = new List<CombatOrderEntry>();
        var seen  = new HashSet<(CombatVisualSide, int)>();

        if (roundOrders.TryGetValue(round, out var actors))
            foreach (var (side, index) in actors)
            {
                var states = side == CombatVisualSide.A ? current.StateA : current.StateB;
                if (index < 0 || index >= states.Count) continue;
                if (!seen.Add((side, index))) continue;
                var state = states[index];
                order.Add(new CombatOrderEntry { Side = side, Index = index, Alive = state.Hp > 0f, State = state });
            }

        for (int i = 0; i < current.StateA.Count; i++)
            if (current.StateA[i].Hp > 0f && seen.Add((CombatVisualSide.A, i)))
                order.Add(new CombatOrderEntry { Side = CombatVisualSide.A, Index = i, Alive = true, State = current.StateA[i] });
        for (int i = 0; i < current.StateB.Count; i++)
            if (current.StateB[i].Hp > 0f && seen.Add((CombatVisualSide.B, i)))
                order.Add(new CombatOrderEntry { Side = CombatVisualSide.B, Index = i, Alive = true, State = current.StateB[i] });

        for (int i = 0; i < current.StateA.Count; i++)
            if (current.StateA[i].Hp <= 0f && seen.Add((CombatVisualSide.A, i)))
                order.Add(new CombatOrderEntry { Side = CombatVisualSide.A, Index = i, Alive = false, State = current.StateA[i] });
        for (int i = 0; i < current.StateB.Count; i++)
            if (current.StateB[i].Hp <= 0f && seen.Add((CombatVisualSide.B, i)))
                order.Add(new CombatOrderEntry { Side = CombatVisualSide.B, Index = i, Alive = false, State = current.StateB[i] });

        CombatVisualEvents.ActionOrder(order);
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
        if (record.SelfTeam == null) { Debug.LogWarning("[CombatVisualizer] record sin datos 3v3"); return; }

        Play(dnaA, record);
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

    private static bool HasTurns(CombatRecord r) => r != null && r.Turns != null && r.Turns.Count > 0 && r.SelfTeam != null;

    private CreatureDNA ResolveDna(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        var reg = GameManager.Instance != null ? GameManager.Instance.Registry : null;
        return reg != null && reg.TryGet(id, out var dna) ? dna : null;
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
