using System;
using Sirenix.OdinInspector;
namespace MoriMonchiSimulator
{

// Passive role effects. Both hooks run AFTER the strike (S46) so every role
// reads the same way — intent → damage → affinity → passive: OnAfterStrike
// fires every turn regardless of the strike's outcome, OnDamageDealt only
// when the strike actually landed. Serialized templates inside
// RoleProfile.Passives, executed each turn by CombatRoleHooks. Authoring rule
// (Index/13): every role passive applies the actor's Element to the ally it
// acts upon — with no resource gate, so it fires every single turn (S46: the
// Energy gate is gone). ShieldAllyPassive targets the ally with the lowest
// HP% (random pick only when the whole team is at 100% HP).
[Serializable]
public abstract class RolePassiveBase
{
    public virtual void OnAfterStrike(Combatant actor, System.Collections.Generic.List<Combatant> allies, CombatManagerSO config, CombatResult result, CombatResolver r, CombatRng rng) { }

    public virtual void OnDamageDealt(Combatant actor, System.Collections.Generic.List<Combatant> allies, float damage, CombatManagerSO config, CombatResult result, CombatResolver r, CombatRng rng) { }

    public abstract string Summary();
}

[Serializable]
public class ShieldAllyPassive : RolePassiveBase
{
    [MinValue(0), LabelText("Shield per turn")]
    public float AmountPerTurn = 1f;

    public override void OnAfterStrike(Combatant actor, System.Collections.Generic.List<Combatant> allies, CombatManagerSO config, CombatResult result, CombatResolver r, CombatRng rng)
    {
        if (AmountPerTurn <= 0f) return;

        var ally = CombatTargeting.LowestHpPercentAlly(allies);
        if (ally == null) return;
        if (ally.Hp >= ally.MaxHp)
        {
            ally = CombatTargeting.PickAlly(allies, rng);
            if (ally == null) return;
        }

        ally.Shield += AmountPerTurn;
        ally.ShieldExpiresAfterRound = r.Round + 1;
        result.Log.Add($"    [Protector] {actor.Name} escuda a {ally.Name} +{AmountPerTurn:F0} (escudo {ally.Shield:F0})");
        r.Record(ModifierEffectKind.Shield, ally, AmountPerTurn);
        CombatElements.AddMark(ally, actor.Element, true, actor, config, result, r, rng);
    }

    public override string Summary()
    {
        return Loc.Tr("effect.shield_ally.summary", AmountPerTurn);
    }
}

[Serializable]
public class HealLowestAllyOnHitPassive : RolePassiveBase
{
    [PropertyRange(0f, 1f), LabelText("% of damage")]
    public float PercentOfDamage = 0.5f;

    public override void OnDamageDealt(Combatant actor, System.Collections.Generic.List<Combatant> allies, float damage, CombatManagerSO config, CombatResult result, CombatResolver r, CombatRng rng)
    {
        if (PercentOfDamage <= 0f) return;

        var healTarget = CombatTargeting.LowestHpAlly(allies);
        if (healTarget != null)
        {
            float heal = damage * PercentOfDamage;
            healTarget.Hp = UnityEngine.Mathf.Min(healTarget.MaxHp, healTarget.Hp + heal);
            result.Log.Add($"    [Empático] {actor.Name} cura a {healTarget.Name} +{heal:F1} → {healTarget.Hp:F1}");
            r.Record(ModifierEffectKind.Heal, healTarget, heal);
            CombatElements.AddMark(healTarget, actor.Element, true, actor, config, result, r, rng);
        }
    }

    public override string Summary()
    {
        return Loc.Tr("effect.heal_lowest_ally.summary", PercentOfDamage);
    }
}

[Serializable]
public class MarkRandomAllyPassive : RolePassiveBase
{
    public override void OnAfterStrike(Combatant actor, System.Collections.Generic.List<Combatant> allies, CombatManagerSO config, CombatResult result, CombatResolver r, CombatRng rng)
    {
        var ally = CombatTargeting.PickAlly(allies, rng);
        if (ally == null) return;

        result.Log.Add($"    [Agresivo] {actor.Name} comparte su elemento con {ally.Name}");
        CombatElements.AddMark(ally, actor.Element, true, actor, config, result, r, rng);
    }

    public override string Summary()
    {
        return Loc.Tr("effect.mark_random_ally.summary");
    }
}
}
