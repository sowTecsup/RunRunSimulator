using System.Collections.Generic;

namespace MoriMonchiSimulator
{

// Role-driven turn hooks for the 3v3 simulator: executes the polymorphic
// Passives/Actives lists carried by each RoleProfile (RolePassiveBase /
// RoleActiveBase). Same public signatures as before the refactor so
// CombatService stays untouched — same rng consumption order, same log
// strings, now data-driven instead of inline per-role branches.
public static class CombatRoleHooks
{
    public static Combatant ResolveTarget(Combatant actor, RoleProfile profile, List<Combatant> allies, List<Combatant> enemies, CombatManagerSO config, CombatResult result, CombatResolver r, CombatRng rng)
    {
        if (profile != null && profile.Actives != null)
        {
            foreach (var active in profile.Actives)
            {
                var t = active.ResolveTarget(actor, allies, enemies, config, result, r, rng);
                if (t != null) return t;
            }
        }
        return CombatTargeting.PickFrontTarget(enemies, rng);
    }

    public static void GrantShield(Combatant actor, RoleProfile profile, List<Combatant> allies, CombatManagerSO config, CombatResult result, CombatResolver r, CombatRng rng)
    {
        if (profile == null || profile.Passives == null) return;

        foreach (var passive in profile.Passives)
            passive.OnTurnStart(actor, allies, config, result, r, rng);
    }

    public static void HealAfterStrike(Combatant actor, RoleProfile profile, List<Combatant> allies, bool dodged, float damage, CombatManagerSO config, CombatResult result, CombatResolver r, CombatRng rng)
    {
        if (dodged || damage <= 0f || profile == null || profile.Passives == null) return;

        foreach (var passive in profile.Passives)
            passive.OnDamageDealt(actor, allies, damage, config, result, r, rng);
    }
}
}
