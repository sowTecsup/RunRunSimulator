using UnityEngine;
namespace MoriMonchiSimulator
{

public static class SocialAffinity
{
    public static float Compute(CreatureDNA a, CreatureDNA b, SocialTuningSO t)
    {
        if (a == null || b == null || t == null)
            return 0f;

        float result = 0f;

        if (a.Element == b.Element)
            result += t.SameElementBonus;

        bool aIsParentOfB = b.MotherID == a.UniqueID || b.FatherID == a.UniqueID;
        bool bIsParentOfA = a.MotherID == b.UniqueID || a.FatherID == b.UniqueID;
        bool sharesMother = !string.IsNullOrEmpty(a.MotherID) && a.MotherID == b.MotherID;
        bool sharesFather = !string.IsNullOrEmpty(a.FatherID) && a.FatherID == b.FatherID;

        if (aIsParentOfB || bIsParentOfA || sharesMother || sharesFather)
            result += t.KinshipBonus;

        result += t.GetRoleBias(a.Role);

        bool aFirst = string.CompareOrdinal(a.UniqueID, b.UniqueID) <= 0;
        string first  = aFirst ? a.UniqueID : b.UniqueID;
        string second = aFirst ? b.UniqueID : a.UniqueID;
        uint hash = Fnv1a32(first + second);
        float normalized = hash / (float)uint.MaxValue * 2f - 1f;
        result += normalized * t.PairChemistrySpread;

        return Mathf.Clamp(result, -1f, 1f);
    }

    private static uint Fnv1a32(string s)
    {
        const uint offsetBasis = 2166136261;
        const uint prime = 16777619;
        uint hash = offsetBasis;
        for (int i = 0; i < s.Length; i++)
        {
            hash ^= s[i];
            hash *= prime;
        }
        return hash;
    }
}
}
