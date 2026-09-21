namespace MoriMonchiSimulator
{

public static class CreatureAvailability
{
    public static bool IsFree(CreatureDNA dna) =>
        dna != null && !dna.IsDead && !dna.IsSold && !dna.IsBusy;

    public static bool IsWellCared(CreatureDNA dna, CareGateSO gate)
    {
        if (gate == null) return true;
        if (dna == null || dna.Needs == null) return false;
        return dna.Needs.Health >= gate.MinHealth
            && dna.Needs.Energy >= gate.MinEnergy
            && dna.Needs.Affect >= gate.MinAffect;
    }

    public static bool CanExplore(CreatureDNA dna, CareGateSO gate) =>
        IsFree(dna) && IsWellCared(dna, gate);

    public static NeedType? WeakestNeed(CreatureDNA dna, CareGateSO gate)
    {
        if (dna == null || dna.Needs == null) return null;
        if (gate == null) return null;
        if (IsWellCared(dna, gate)) return null;

        float healthGap = gate.MinHealth - dna.Needs.Health;
        float energyGap = gate.MinEnergy - dna.Needs.Energy;
        float affectGap = gate.MinAffect - dna.Needs.Affect;

        NeedType weakest = NeedType.Health;
        float worstGap = healthGap;

        if (energyGap > worstGap) { weakest = NeedType.Energy; worstGap = energyGap; }
        if (affectGap > worstGap) { weakest = NeedType.Affect; worstGap = affectGap; }

        return weakest;
    }
}
}
