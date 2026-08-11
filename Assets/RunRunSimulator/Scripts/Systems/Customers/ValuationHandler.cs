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
              + pricing.BasePricePerTier.GetValueOrDefault(dna.HornTier, 0)
              + pricing.BasePricePerTier.GetValueOrDefault(dna.BackTier, 0)
              + pricing.BasePricePerTier.GetValueOrDefault(dna.WingTier, 0);

            float wBreed    = archetype != null ? archetype.WeightBreed    : 1f;
            float wStats    = archetype != null ? archetype.WeightStats    : 1f;
            float wTier     = archetype != null ? archetype.WeightTier     : 1f;
            float budgetMul = archetype != null ? archetype.BudgetMultiplier : 1f;

            float statsBonus  = (dna.BaseConstitution + dna.BaseAttack + dna.BaseSpeed
                               + dna.BaseDefense + dna.BaseLuck + dna.BaseEvasion) * wStats * pricing.StatsMultiplier;
            float breedBonus  = dna.BreedCount * wBreed * pricing.BreedCountMultiplier;
            float tierBonus   = ((int)dna.BodyTier + (int)dna.HornTier + (int)dna.BackTier + (int)dna.WingTier) * wTier * pricing.TierMultiplier;

            float objective = basePrice + statsBonus + breedBonus + tierBonus;
            return Mathf.Max(0, Mathf.RoundToInt(objective * budgetMul));
        }
    }
}
