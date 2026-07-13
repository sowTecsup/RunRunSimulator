using System.Collections.Generic;

namespace MoriMonchiSimulator
{

// Equipped item uses for the 3v3 simulator: collects each ItemUseEffect's
// remaining-use counter from a creature's loadout and fires them
// deterministically during a turn (no roll — a use fires once its rule
// matches, decrementing its remaining count).
public static class CombatItems
{
    public static List<ItemUseState> CollectUses(CreatureDNA dna, EquipmentDatabaseSO equipDb)
    {
        var list = new List<ItemUseState>();
        if (equipDb == null || dna.Equipped == null) return list;
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            if (!dna.Equipped.TryGetValue(slot, out var id)) continue;
            var item = equipDb.GetByID(id);
            if (item?.Effects == null) continue;
            foreach (var e in item.Effects)
                if (e is ItemUseEffect use) list.Add(new ItemUseState { Effect = use, Remaining = use.Uses });
        }
        return list;
    }

    public static void UseItems(Combatant actor, Combatant target, CombatResolver r, CombatResult result)
    {
        foreach (var u in actor.Uses)
        {
            if (u.Remaining <= 0) continue;
            if (u.Effect.Rule == UseRule.SelfHpBelow && actor.Hp > actor.MaxHp * (u.Effect.HpThresholdPercent / 100f)) continue;
            u.Remaining--;
            r.Self = actor;
            r.Opponent = target;
            result.Log.Add($"    [item] {actor.Name} usa {u.Effect.Summary()} ({u.Remaining} restantes)");
            u.Effect.Apply(r);
        }
    }
}
}
