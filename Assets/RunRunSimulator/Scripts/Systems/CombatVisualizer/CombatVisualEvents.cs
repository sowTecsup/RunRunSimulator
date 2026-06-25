using System;
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
}

public struct CombatVisualHit
{
    public CombatVisualSide Attacker;
    public CombatVisualSide Defender;
    public float            Damage;
    public bool             Crit;
}

public enum CombatVisualLogKind { Versus, Hit, Crit, Death, Result }

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

    public static event Action<CombatVisualHit> OnCrit;
    public static void Crit(CombatVisualHit hit) => OnCrit?.Invoke(hit);

    public static event Action<CombatVisualSide, float, float> OnHpChanged;
    public static void HpChanged(CombatVisualSide side, float current, float max) => OnHpChanged?.Invoke(side, current, max);

    public static event Action<CombatVisualSide> OnDead;
    public static void Dead(CombatVisualSide side) => OnDead?.Invoke(side);

    public static event Action<string> OnLog;
    public static void Log(string line) => OnLog?.Invoke(line);

    public static event Action<CombatVisualPanelState> OnPanelState;
    public static void PanelState(CombatVisualPanelState st) => OnPanelState?.Invoke(st);
}
}
