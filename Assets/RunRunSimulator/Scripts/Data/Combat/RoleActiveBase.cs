using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
namespace MoriMonchiSimulator
{

// Active role targeting overrides: decide who gets hit before the default
// front-row pick applies. Serialized templates inside RoleProfile.Actives,
// executed each turn by CombatRoleHooks. ResolveTarget returns null when this
// active does not override targeting this turn. Targeting only — elemental
// marks belong to the role's passives (S46), never here.
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

        float aggroRoll = rng.NextFloat();
        if (aggroRoll >= Chance) return CombatTargeting.PickFrontTarget(enemies, rng);

        var back = CombatTargeting.PickBacklineTarget(enemies, rng);
        if (back == null) return CombatTargeting.PickFrontTarget(enemies, rng);

        result.Log.Add($"    [Agresivo] {actor.Name} caza la backline");
        return back;
    }

    public override string Summary()
    {
        return Loc.Tr("effect.backline_hunter.summary", Chance);
    }
}
}
