using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
namespace MoriMonchiSimulator
{

// Active role targeting overrides: decide who gets hit before the default
// front-row pick applies. Serialized templates inside RoleProfile.Actives,
// executed each turn by CombatRoleHooks — same rng consumption order and log
// strings as the values they replace. ResolveTarget returns null when this
// active does not override targeting this turn.
[Serializable]
public abstract class RoleActiveBase
{
    public abstract Combatant ResolveTarget(Combatant actor, List<Combatant> allies, List<Combatant> enemies, CombatManagerSO config, CombatResult result, CombatResolver r, CombatRng rng);

    public abstract string Summary();
}

[Serializable]
public class BacklineHunterActive : RoleActiveBase
{
    [PropertyRange(0f, 1f), LabelText("Backline chance")]
    public float Chance = 0.5f;

    public override Combatant ResolveTarget(Combatant actor, List<Combatant> allies, List<Combatant> enemies, CombatManagerSO config, CombatResult result, CombatResolver r, CombatRng rng)
    {
        if (Chance <= 0f) return null;

        Combatant target;
        float aggroRoll = rng.NextFloat();
        if (aggroRoll < Chance)
        {
            var back = CombatTargeting.PickBacklineTarget(enemies, rng);
            if (back != null)
            {
                target = back;
                result.Log.Add($"    [Agresivo] {actor.Name} caza la backline");
                if (actor.Energy > 0)
                {
                    actor.Energy--;
                    r.RecordElement(ElementEventKind.EnergySpent, actor, amount: actor.Energy);
                    CombatElements.AddMark(CombatTargeting.PickAlly(allies, rng), actor.Element, true, actor, config, result, r, rng);
                }
            }
            else
            {
                if (actor.Energy > 0)
                {
                    actor.Energy--;
                    r.RecordElement(ElementEventKind.EnergySpent, actor, amount: actor.Energy);
                    var mate = CombatTargeting.PickAlly(allies, rng);
                    mate.Energy++;
                    result.Log.Add($"    [Agresivo] {actor.Name} comparte energía con {mate.Name} (energía {mate.Energy})");
                    r.RecordElement(ElementEventKind.EnergyGained, mate, amount: mate.Energy);
                }
                else
                {
                    result.Log.Add("    [Agresivo] sin backline — comparte energía (sin efecto)");
                }
                target = CombatTargeting.PickFrontTarget(enemies, rng);
            }
        }
        else
        {
            target = CombatTargeting.PickFrontTarget(enemies, rng);
        }
        return target;
    }

    public override string Summary()
    {
        return $"{Chance:P0} de probabilidad de cazar la backline enemiga";
    }
}
}
