using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

public static class NeedStationRegistry
{
    private static readonly List<NeedStation> stations = new List<NeedStation>();

    public static void Register(NeedStation s)   { if (!stations.Contains(s)) stations.Add(s); }
    public static void Unregister(NeedStation s) => stations.Remove(s);

    public static NeedStation GetClosest(Vector3 from, NeedType type, bool onlyAvailable = true)
    {
        NeedStation best = null;
        float bestSqr = float.MaxValue;

        foreach (var s in stations)
        {
            if (s == null || s.Need != type) continue;
            if (onlyAvailable && !s.IsAvailable) continue;

            float d = (s.UsePosition - from).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = s; }
        }
        return best;
    }
}
}
