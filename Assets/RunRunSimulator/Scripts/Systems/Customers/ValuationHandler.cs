using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator
{
    [Serializable]
    public class ValuationHandler
    {
        public int Estimate(CreatureDNA dna, CustomerArchetypeSO archetype, CustomerPricingSO pricing)
        {
            if (dna == null || pricing == null) return 0;

            int basePrice =
                pricing.BasePricePerTier.GetValueOrDefault(dna.BodyTier, 0)
              + pricing.BasePricePerTier.GetValueOrDefault(dna.ArmTier, 0)
              + pricing.BasePricePerTier.GetValueOrDefault(dna.EyeTier, 0)
              + pricing.BasePricePerTier.GetValueOrDefault(dna.MouthTier, 0);

            float wBreed    = archetype != null ? archetype.WeightBreed    : 1f;
            float wCombat   = archetype != null ? archetype.WeightCombat   : 1f;
            float wStats    = archetype != null ? archetype.WeightStats    : 1f;
            float wTier     = archetype != null ? archetype.WeightTier     : 1f;
            float budgetMul = archetype != null ? archetype.BudgetMultiplier : 1f;

            float statsBonus  = (dna.BaseConstitution + dna.BaseAttack + dna.BaseSpeed
                               + dna.BaseDefense + dna.BaseLuck + dna.BaseEvasion) * wStats * pricing.StatsMultiplier;
            float breedBonus  = dna.BreedCount * wBreed * pricing.BreedCountMultiplier;
            float winrate     = dna.FightCount > 0 ? (float)dna.WinCount / dna.FightCount : 0f;
            float combatBonus = winrate * wCombat * pricing.CombatWinrateMultiplier;
            float tierBonus   = ((int)dna.BodyTier + (int)dna.ArmTier + (int)dna.EyeTier + (int)dna.MouthTier) * wTier * pricing.TierMultiplier;

            float objective = basePrice + statsBonus + breedBonus + combatBonus + tierBonus;
            return Mathf.Max(0, Mathf.RoundToInt(objective * budgetMul));
        }
    }
}
