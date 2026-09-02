using MoriMonchiSimulator.DragonRps;
using System;
using UnityEngine;
namespace MoriMonchiSimulator
{

public static class DragonRpsGenes
{
    public static int PowerOf(int potential) => Mathf.Clamp(potential, CreatureGenerator.PotentialMin, CreatureGenerator.PotentialMax);

    public static DragonRpsDragon ToDragon(CreatureDNA dna)
    {
        string name = !string.IsNullOrEmpty(dna.CustomName) ? dna.CustomName : dna.ToStringID();

        DragonRpsDragon dragon = DragonRpsDragon.Standard(name, 1);
        dragon.Power[(int)DragonAction.Horns] = PowerOf(dna.HornPotential);
        dragon.Power[(int)DragonAction.Wings] = PowerOf(dna.WingPotential);
        dragon.Power[(int)DragonAction.Back]  = PowerOf(dna.BackPotential);

        return dragon;
    }

    public static int Budget(CreatureDNA dna) =>
        PowerOf(dna.HornPotential) + PowerOf(dna.WingPotential) + PowerOf(dna.BackPotential);

    public static bool CanFight(CreatureDNA dna, CombatTuningSO tuning, DateTime now) =>
        !dna.IsDead && !dna.IsSold && !dna.IsBusy
        && dna.CombatCooldownUntil <= now.Ticks
        && dna.Needs.Energy >= tuning.MinEnergyToFight;
}
}
