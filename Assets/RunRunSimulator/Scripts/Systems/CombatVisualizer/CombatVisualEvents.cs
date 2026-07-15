using System;
using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

public enum CombatVisualSide { A, B }

public struct CombatVisualContext
{
    public CreatureDNA DnaA;
    public CreatureDNA DnaB;
    public float       HpMaxA;
    public float       HpMaxB;
    public Transform   SlotA;
    public Transform   SlotB;
    public int         TotalTurns;
    public List<CreatureDNA> TeamA;
    public List<CreatureDNA> TeamB;
    public float[]     HpMaxTeamA;
    public float[]     HpMaxTeamB;
    public List<CombatFighterSnapshot> SnapsA;
    public List<CombatFighterSnapshot> SnapsB;
}

public struct CombatVisualHit
{
    public CombatVisualSide Attacker;
    public CombatVisualSide Defender;
    public float            Damage;
    public bool             Crit;
    public int              AttackerIndex;
    public int              DefenderIndex;
}

public struct CombatVisualPopup
{
    public CombatVisualSide Side;
    public Vector3          Position;
    public CombatPopupKind  Kind;
    public float            Amount;
    public Transform        Follow;
    public string Text;
    public Color  OverrideColor;
    public bool   HasOverrideColor;
}

public struct CombatOrderEntry
{
    public CombatVisualSide Side;
    public int              Index;
    public bool             Alive;
    public CombatUnitState  State;
}

public struct CombatSpeechData
{
    public CombatVisualSide Side;
    public int              Index;
    public string           Text;
    public Color            Color;
    public bool             HasColor;
    public float            Duration;
    public Transform        Follow;
    public bool             HasTarget;
    public CombatVisualSide TargetSide;
    public int              TargetIndex;
    public Transform        TargetFollow;
}

public struct CombatElementEventData
{
    public CombatVisualSide Side;
    public int              Index;
    public ElementEventKind Kind;
    public Element          Element;
    public Element          ElementB;
    public bool             AllySource;
    public ElementalState   State;
    public string           ReactionName;
}

public enum CombatVisualLogKind { Versus, Hit, Crit, Death, Result, Proc }

public struct CombatVisualLogLine
{
    public string              Text;
    public CombatVisualLogKind Kind;
}

public struct CombatVisualPanelState
{
    public int                   TurnNumber;
    public int                   TotalTurns;
    public CombatVisualLogLine[] Log;
    public bool                  Ended;
    public bool                  IsDraw;
    public CombatVisualSide      Winner;
    public bool                  IsAuto;
    public bool                  CanForward;
    public bool                  CanBack;
    public float                 Speed;
}

public static class CombatVisualEvents
{
    public static event Action<CombatVisualContext> OnVisualCombatStart;
    public static void VisualCombatStart(CombatVisualContext ctx) => OnVisualCombatStart?.Invoke(ctx);

    public static event Action<CombatVisualSide, bool> OnVisualCombatEnd;
    public static void VisualCombatEnd(CombatVisualSide winner, bool isDraw) => OnVisualCombatEnd?.Invoke(winner, isDraw);

    public static event Action<CombatTurn> OnTurnStart;
    public static void TurnStart(CombatTurn turn) => OnTurnStart?.Invoke(turn);

    public static event Action<CombatTurn> OnTurnEnd;
    public static void TurnEnd(CombatTurn turn) => OnTurnEnd?.Invoke(turn);

    public static event Action<CombatVisualSide> OnAttack;
    public static void Attack(CombatVisualSide side) => OnAttack?.Invoke(side);

    public static event Action<CombatVisualHit> OnHit;
    public static void Hit(CombatVisualHit hit) => OnHit?.Invoke(hit);

    public static event Action<CombatVisualPopup> OnPopup;
    public static void Popup(CombatVisualPopup p) => OnPopup?.Invoke(p);

    public static event Action<CombatVisualHit> OnCrit;
    public static void Crit(CombatVisualHit hit) => OnCrit?.Invoke(hit);

    public static event Action<CombatVisualSide, float, float> OnHpChanged;
    public static void HpChanged(CombatVisualSide side, float current, float max) => OnHpChanged?.Invoke(side, current, max);

    public static event Action<CombatVisualSide> OnDead;
    public static void Dead(CombatVisualSide side) => OnDead?.Invoke(side);

    public static event Action<CombatVisualSide, int, float, float> OnUnitHpChanged;
    public static void UnitHpChanged(CombatVisualSide side, int index, float current, float max) => OnUnitHpChanged?.Invoke(side, index, current, max);

    public static event Action<CombatVisualSide, int> OnUnitDead;
    public static void UnitDead(CombatVisualSide side, int index) => OnUnitDead?.Invoke(side, index);

    public static event Action<string> OnLog;
    public static void Log(string line) => OnLog?.Invoke(line);

    public static event Action<CombatVisualPanelState> OnPanelState;
    public static void PanelState(CombatVisualPanelState st) => OnPanelState?.Invoke(st);

    public static event Action<List<CombatOrderEntry>> OnActionOrder;
    public static void ActionOrder(List<CombatOrderEntry> order) => OnActionOrder?.Invoke(order);

    public static event Action<CombatVisualSide, int, int> OnUnitAffinity;
    public static void UnitAffinity(CombatVisualSide side, int index, int affinity) => OnUnitAffinity?.Invoke(side, index, affinity);

    public static event Action<CombatVisualSide, int> OnActiveUnit;
    public static void ActiveUnit(CombatVisualSide side, int index) => OnActiveUnit?.Invoke(side, index);

    public static event Action<CombatSpeechData> OnSpeech;
    public static void Speech(CombatSpeechData d) => OnSpeech?.Invoke(d);

    public static event Action<CombatElementEventData> OnUnitElement;
    public static void UnitElement(CombatElementEventData d) => OnUnitElement?.Invoke(d);
}
}
