using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

// Implementación de ICombatContext: los efectos de ítem emiten acciones acá
// sin mutar el estado del combate directamente. Cada mutación se graba como
// CombatProcEvent para el replay del Combat Visualizer. Aplica las salvaguardas
// anti-permastun (StunOpponent) y el stacking por instancias independientes (AddStatus).
public class CombatResolver : ICombatContext
{
    public CombatResult Result;
    public Combatant    Self;
    public Combatant    Opponent;
    public List<CombatProcEvent> TurnProcs;
    public bool                  BeforeStrike;

    public void DamageOpponent(float amount, string source)
    {
        Opponent.Hp = Mathf.Max(0f, Opponent.Hp - amount);
        Result.Log.Add($"    [{source}] {Opponent.Name} -{amount:F1} → {Opponent.Hp:F1}");
        Record(ModifierEffectKind.ReturnDamage, Opponent, amount);
    }

    public void HealSelf(float amount, string source)
    {
        Self.Hp = Mathf.Min(Self.MaxHp, Self.Hp + amount);
        Result.Log.Add($"    [{source}] {Self.Name} +{amount:F1} → {Self.Hp:F1}");
        Record(ModifierEffectKind.Heal, Self, amount);
    }

    public void ApplyStatusToOpponent(ModifierEffectKind kind, int turns, int magnitude, string source) =>
        AddStatus(Opponent, kind, turns, magnitude, source);

    public void ApplyStatusToSelf(ModifierEffectKind kind, int turns, int magnitude, string source) =>
        AddStatus(Self, kind, turns, magnitude, source);

    public void StunOpponent(int turns) => StunTarget(Opponent, turns);

    private void StunTarget(Combatant t, int turns)
    {
        if (t.StunTurns > 0)
        {
            Result.Log.Add($"    [stun] {t.Name} is already stunned — no effect");
            return;
        }
        if (t.StunImmunityTurns > 0)
        {
            Result.Log.Add($"    [stun] {t.Name} resists the stun (immune, {t.StunImmunityTurns}t left)");
            return;
        }
        t.StunTurns = turns;
        Result.Log.Add($"    [stun] {t.Name} stunned for {t.StunTurns} turn(s)");
        Record(ModifierEffectKind.Stun, t, turns);
    }

    private void AddStatus(Combatant t, ModifierEffectKind kind, int turns, int magnitude, string source)
    {
        t.Active.Add(new ActiveEffect { Kind = kind, RemainingTurns = turns, Magnitude = magnitude });
        int stacks = 0;
        foreach (var a in t.Active) if (a.Kind == kind) stacks++;
        Result.Log.Add($"    [{source}] {t.Name} gains {kind} ({magnitude}/turn, {turns}t){(stacks > 1 ? $" — x{stacks} stacks" : "")}");
        Record(kind, t, magnitude);
    }

    public void Record(ModifierEffectKind kind, Combatant target, float amount)
        => TurnProcs?.Add(new CombatProcEvent
        {
            Kind = kind, TargetIsA = target.IsA, TargetIndex = target.Index, Amount = amount,
            TargetHpAfter = target.Hp, BeforeStrike = BeforeStrike,
            TargetStatusAfter = StatusMarks(target),
        });

    public void Record(ModifierEffectKind kind, Combatant target) => Record(kind, target, 0f);

    public void RecordElement(ElementEventKind ev, Combatant target, float amount = 0f, Element element = default, Element elementB = default, bool allySource = false, ElementalState state = default, string reactionName = null)
        => TurnProcs?.Add(new CombatProcEvent
        {
            ElementEvent = ev, TargetIsA = target.IsA, TargetIndex = target.Index, Amount = amount,
            TargetHpAfter = target.Hp, BeforeStrike = BeforeStrike,
            Element = element, ElementB = elementB, AllySource = allySource, State = state, ReactionName = reactionName,
        });

    public static List<CombatStatusMark> StatusMarks(Combatant c)
    {
        var counts = new Dictionary<ModifierEffectKind, int>();
        foreach (var a in c.Active)
            counts[a.Kind] = counts.TryGetValue(a.Kind, out var n) ? n + 1 : 1;

        var marks = new List<CombatStatusMark>();
        foreach (ModifierEffectKind kind in System.Enum.GetValues(typeof(ModifierEffectKind)))
            if (counts.TryGetValue(kind, out var stacks))
                marks.Add(new CombatStatusMark { Kind = kind, Stacks = stacks });

        if (c.StunTurns > 0)
            marks.Add(new CombatStatusMark { Kind = ModifierEffectKind.Stun, Stacks = c.StunTurns });

        return marks;
    }

}
}
