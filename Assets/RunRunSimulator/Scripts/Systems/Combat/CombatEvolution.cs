using System.Collections.Generic;
namespace MoriMonchiSimulator
{

// Single authority for combat tier evolution: rolls and applies part-slot
// tier-ups. Used by both the local CombatService simulator and the async
// result applier (AsyncCombatService) so the two never drift.
public static class CombatEvolution
{
    public static string TryEvolveRandomSlot(CreatureDNA dna, CombatRng rng)
    {
        var eligible = new List<string>();
        if (dna.BodyTier  < Tier.Tier3) eligible.Add("Body");
        if (dna.ArmTier   < Tier.Tier3) eligible.Add("Arm");
        if (dna.EyeTier   < Tier.Tier3) eligible.Add("Eye");
        if (dna.MouthTier < Tier.Tier3) eligible.Add("Mouth");

        if (eligible.Count == 0) return null;

        string slot = eligible[rng.Range(0, eligible.Count)];
        AdvanceTier(dna, slot);
        return slot;
    }

    public static void AdvanceTier(CreatureDNA dna, string slot)
    {
        switch (slot)
        {
            case "Body":  if (dna.BodyTier  < Tier.Tier3) dna.BodyTier  = (Tier)((int)dna.BodyTier  + 1); break;
            case "Arm":   if (dna.ArmTier   < Tier.Tier3) dna.ArmTier   = (Tier)((int)dna.ArmTier   + 1); break;
            case "Eye":   if (dna.EyeTier   < Tier.Tier3) dna.EyeTier   = (Tier)((int)dna.EyeTier   + 1); break;
            case "Mouth": if (dna.MouthTier < Tier.Tier3) dna.MouthTier = (Tier)((int)dna.MouthTier + 1); break;
        }
    }

    public static int GetSlotTier(CreatureDNA dna, string slot) => slot switch
    {
        "Body"  => (int)dna.BodyTier,
        "Arm"   => (int)dna.ArmTier,
        "Eye"   => (int)dna.EyeTier,
        "Mouth" => (int)dna.MouthTier,
        _       => 0
    };
}
}
