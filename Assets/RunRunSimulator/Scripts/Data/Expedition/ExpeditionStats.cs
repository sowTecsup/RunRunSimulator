using UnityEngine;
namespace MoriMonchiSimulator
{

public struct ExpeditionStats
{
    public int CarryCapacity;
    public float LoadedSpeedFactor;
    public bool KeepCarryOnKnock;
    public float GuardRadius;
    public float VisibleFrom;

    public static ExpeditionStats Default => new ExpeditionStats
    {
        CarryCapacity = 3,
        LoadedSpeedFactor = 1f,
        KeepCarryOnKnock = false,
        GuardRadius = 4f,
        VisibleFrom = 0f
    };

    public static ExpeditionStats Resolve(ExpeditionRulesSO rules, Occupation occupation, AbilitySO[] abilities)
    {
        ExpeditionStats stats = Default;

        if (rules != null)
        {
            stats.CarryCapacity = occupation == Occupation.Gather || occupation == Occupation.Explore ? rules.CarryCapacity : rules.SupportCarryCapacity;
            stats.GuardRadius = rules.GuardRadius;
        }

        int bestCarryCapacity = 0;
        bool hasCarryCapacityOverride = false;

        if (abilities != null)
        {
            for (int i = 0; i < abilities.Length; i++)
            {
                AbilitySO a = abilities[i];
                if (a == null) continue;

                if (a.CarryCapacity > 0 && (!hasCarryCapacityOverride || a.CarryCapacity < bestCarryCapacity))
                {
                    bestCarryCapacity = a.CarryCapacity;
                    hasCarryCapacityOverride = true;
                }

                if (a.LoadedSpeedFactor > 0f) stats.LoadedSpeedFactor *= a.LoadedSpeedFactor;

                stats.KeepCarryOnKnock |= a.KeepCarryOnKnock;

                if (a.GuardRadius > 0f) stats.GuardRadius = a.GuardRadius;

                stats.VisibleFrom = Mathf.Max(stats.VisibleFrom, a.VisibleFrom);
            }
        }

        if (hasCarryCapacityOverride) stats.CarryCapacity = bestCarryCapacity;

        return stats;
    }
}
}
