using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

public static class DragonRpsRival
{
    public static CreatureDNA Generate(CreatureRegistrySO registry, CreatureDNA player, CombatTuningSO tuning, System.Random rng)
    {
        var candidates = new List<CreatureDNA>();
        foreach (var dna in registry.GetAll().Values)
            if (!dna.IsDead && !dna.IsSold && dna.UniqueID != player.UniqueID)
                candidates.Add(dna);

        if (candidates.Count == 0)
            return null;

        var src = candidates[rng.Next(candidates.Count)];

        var rival = CreatureDNA.FromID(src.ToStringID());
        rival.BaseColor      = ColorGenetics.RandomBase(rng);
        rival.SecondaryColor = ColorGenetics.DeriveSecondary(rival.BaseColor);
        rival.FurType        = src.FurType;
        rival.BodyTier       = src.BodyTier;
        rival.CustomName     = "Salvaje " + src.CustomName;

        int target  = DragonRpsGenes.Budget(player);
        bool matched = false;
        for (int i = 0; i < 32; i++)
        {
            rival.HornTier = (Tier)rng.Next(1, 4);
            rival.WingTier = (Tier)rng.Next(1, 4);
            rival.BackTier = (Tier)rng.Next(1, 4);
            if (Mathf.Abs(DragonRpsGenes.Budget(rival) - target) <= tuning.BudgetTolerance)
            {
                matched = true;
                break;
            }
        }

        if (!matched)
        {
            rival.HornTier = player.HornTier;
            rival.WingTier = player.WingTier;
            rival.BackTier = player.BackTier;
        }

        return rival;
    }
}
}
