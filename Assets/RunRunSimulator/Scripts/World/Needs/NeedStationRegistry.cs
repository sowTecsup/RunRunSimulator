using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

// Lightweight runtime index of the NeedStations currently in the scene. Stations self-register in
// OnEnable and unregister in OnDisable, so it self-cleans across scene loads (no manual census, no
// scene wiring). Agents query it for the closest available station of a given need.
//
// Static — mirrors the GameEvents bus pattern — instead of a MonoBehaviour singleton, so there's no
// lifetime to manage. Agent ↔ stations are the SAME World domain (like agent ↔ MoriMochiContainer),
// so this direct query is consistent with the pen precedent, not a cross-system singleton lookup.
public static class NeedStationRegistry
{
    private static readonly List<NeedStation> stations = new List<NeedStation>();

    public static void Register(NeedStation s)   { if (!stations.Contains(s)) stations.Add(s); }
    public static void Unregister(NeedStation s) => stations.Remove(s);

    // Closest station of 'type' to 'from'. With onlyAvailable, skips ones already reserved by
    // another agent. Returns null if none qualify.
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
