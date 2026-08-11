using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace MoriMonchiSimulator
{
[CreateAssetMenu(fileName = "CustomerPricing", menuName = "RunRunSimulator/Customers/Customer Pricing")]
[InlineEditor]
public class CustomerPricingSO : SerializedScriptableObject
{
    [Title("Base Price per Tier")]
    [OdinSerialize] public Dictionary<Tier, int> BasePricePerTier = new();

    [Title("Multipliers")]
    [MinValue(0)] public float StatsMultiplier = 1f;
    [MinValue(0)] public float BreedCountMultiplier = 2f;
    [MinValue(0)] public float TierMultiplier = 5f;

    [Title("Renegotiation")]
    [Range(0f, 1f)] public float RenegotiationStep = 0.20f;

    [Button("Seed Defaults")]
    private void SeedDefaults()
    {
        if (BasePricePerTier == null || BasePricePerTier.Count == 0)
        {
            BasePricePerTier = new Dictionary<Tier, int>
            {
                { Tier.Tier1, 20 },
                { Tier.Tier2, 50 },
                { Tier.Tier3, 120 }
            };
        }
    }
}
}
