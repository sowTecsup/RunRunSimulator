using MoriMonchiSimulator.DragonRps;
using System;
using UnityEngine;
namespace MoriMonchiSimulator
{

public static class DragonRpsGenes
{
    public static int PowerOf(Tier tier) => Mathf.Clamp((int)tier, 1, 3);

    public static DragonRpsDragon ToDragon(CreatureDNA dna)
    {
        string name = !string.IsNullOrEmpty(dna.CustomName) ? dna.CustomName : dna.ToStringID();

        DragonRpsDragon dragon = DragonRpsDragon.Standard(name, 1);
        dragon.Power[(int)DragonAction.Horns] = PowerOf(dna.HornTier);
        dragon.Power[(int)DragonAction.Wings] = PowerOf(dna.WingTier);
        dragon.Power[(int)DragonAction.Back]  = PowerOf(dna.BackTier);

        return dragon;
    }

    public static int Budget(CreatureDNA dna) =>
        PowerOf(dna.HornTier) + PowerOf(dna.WingTier) + PowerOf(dna.BackTier);

    public static bool CanFight(CreatureDNA dna, CombatTuningSO tuning, DateTime now) =>
        !dna.IsDead && !dna.IsSold && !dna.IsBusy
        && dna.CombatCooldownUntil <= now.Ticks
        && dna.Needs.Energy >= tuning.MinEnergyToFight;
}
}
