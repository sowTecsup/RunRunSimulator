using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

// Implementación de ICombatContext: los procs de equipamiento emiten acciones acá
// sin mutar el estado del combate directamente. Cada mutación se graba como
// CombatProcEvent para el replay del Combat Visualizer. Aplica las salvaguardas
// anti-permastun (StunOpponent) y el stacking por instancias independientes (AddStatus).
// También detona recetas de sinergia (SynergyTableSO): quema stacks FIFO y aplica los SynergyEffectBase sobre el portador.
public class CombatResolver : ICombatContext
{
    public CombatResult Result;
    public Combatant    Self;
    public Combatant    Opponent;
    public List<CombatProcEvent> TurnProcs;
    public bool                  BeforeStrike;
    public SynergyTableSO        Synergies;

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

    public void StunBearer(Combatant bearer, int turns) => StunTarget(bearer, turns);

    public void DamageBearer(Combatant bearer, float amount, string source)
    {
        bearer.Hp = Mathf.Max(0f, bearer.Hp - amount);
        Result.Log.Add($"    [{source}] {bearer.Name} -{amount:F1} → {bearer.Hp:F1}");
        Record(ModifierEffectKind.Synergy, bearer, amount);
    }

    public void HealBearer(Combatant bearer, float amount, string source)
    {
        bearer.Hp = Mathf.Min(bearer.MaxHp, bearer.Hp + amount);
        Result.Log.Add($"    [{source}] {bearer.Name} +{amount:F1} → {bearer.Hp:F1}");
        Record(ModifierEffectKind.Synergy, bearer, amount);
    }

    public void AddStatusTo(Combatant bearer, ModifierEffectKind kind, int turns, int magnitude, string source) =>
        AddStatus(bearer, kind, turns, magnitude, source);

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
        CheckSynergies(t);
    }

    public void Record(ModifierEffectKind kind, Combatant target, float amount)
        => TurnProcs?.Add(new CombatProcEvent
        {
            Kind = kind, TargetIsA = target.IsA, Amount = amount,
            TargetHpAfter = target.Hp, BeforeStrike = BeforeStrike,
        });

    public void Record(ModifierEffectKind kind, Combatant target) => Record(kind, target, 0f);

    private bool resolvingSynergies;

    private void CheckSynergies(Combatant bearer)
    {
        if (resolvingSynergies || Synergies == null || Synergies.Rules == null) return;
        resolvingSynergies = true;
        for (int guard = 0; guard < 8; guard++)
        {
            var rule = FirstSatisfiedRule(bearer);
            if (rule == null) break;
            Result.Log.Add($"    [synergy] ¡{rule.Name}! detonates on {bearer.Name}");
            Record(ModifierEffectKind.Synergy, bearer);
            ConsumeStacks(bearer, rule);
            foreach (var e in rule.Effects)
                if (e != null) e.Apply(this, bearer);
        }
        resolvingSynergies = false;
    }

    private SynergyRule FirstSatisfiedRule(Combatant bearer)
    {
        foreach (var rule in Synergies.Rules)
        {
            if (rule == null || rule.Requirements == null || rule.Requirements.Count == 0) continue;
            bool satisfied = true;
            foreach (var req in rule.Requirements)
            {
                int count = 0;
                foreach (var a in bearer.Active) if (a.Kind == req.Kind) count++;
                if (count < req.Stacks) { satisfied = false; break; }
            }
            if (satisfied) return rule;
        }
        return null;
    }

    private void ConsumeStacks(Combatant bearer, SynergyRule rule)
    {
        foreach (var req in rule.Requirements)
        {
            int toRemove = req.Stacks;
            for (int i = 0; i < bearer.Active.Count && toRemove > 0; )
            {
                if (bearer.Active[i].Kind == req.Kind) { bearer.Active.RemoveAt(i); toRemove--; }
                else i++;
            }
            Result.Log.Add($"    [synergy] consumed {req.Stacks}x {req.Kind} stacks");
        }
    }
}
}
