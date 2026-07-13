using System;
using Sirenix.OdinInspector;
namespace MoriMonchiSimulator
{

// Passive role effects: triggered every turn (OnTurnStart) or after a landed
// strike (OnDamageDealt). Serialized templates inside RoleProfile.Passives,
// executed each turn by CombatRoleHooks — same rng consumption order and log
// strings as the values they replace.
[Serializable]
public abstract class RolePassiveBase
{
    public virtual void OnTurnStart(Combatant actor, System.Collections.Generic.List<Combatant> allies, CombatManagerSO config, CombatResult result, CombatResolver r, CombatRng rng) { }

    public virtual void OnDamageDealt(Combatant actor, System.Collections.Generic.List<Combatant> allies, float damage, CombatManagerSO config, CombatResult result, CombatResolver r, CombatRng rng) { }

    public abstract string Summary();
}

[Serializable]
public class ShieldAllyPassive : RolePassiveBase
{
    [MinValue(0), LabelText("Shield per turn")]
    public float AmountPerTurn = 1f;

    public override void OnTurnStart(Combatant actor, System.Collections.Generic.List<Combatant> allies, CombatManagerSO config, CombatResult result, CombatResolver r, CombatRng rng)
    {
        if (AmountPerTurn <= 0f) return;

        var ally = CombatTargeting.PickAlly(allies, rng);
        ally.Shield += AmountPerTurn;
        result.Log.Add($"    [Protector] {actor.Name} escuda a {ally.Name} +{AmountPerTurn:F0} (escudo {ally.Shield:F0})");
        r.Record(ModifierEffectKind.Shield, ally, AmountPerTurn);
        if (actor.Energy > 0)
        {
            actor.Energy--;
            r.RecordElement(ElementEventKind.EnergySpent, actor, amount: actor.Energy);
            CombatElements.AddMark(ally, actor.Element, true, actor, config, result, r, rng);
        }
    }

    public override string Summary()
    {
        return $"Escuda +{AmountPerTurn:0.##} por turno a un aliado";
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
            if (actor.Energy > 0)
            {
                actor.Energy--;
                r.RecordElement(ElementEventKind.EnergySpent, actor, amount: actor.Energy);
                CombatElements.AddMark(healTarget, actor.Element, true, actor, config, result, r, rng);
            }
        }
    }

    public override string Summary()
    {
        return $"Cura {PercentOfDamage:P0} del daño infligido al aliado con menos HP";
    }
}
}
