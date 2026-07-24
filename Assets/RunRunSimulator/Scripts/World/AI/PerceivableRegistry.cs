using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

// Lightweight runtime index of the Perceivables currently in the scene. Perceivables self-register in
// OnEnable and unregister in OnDisable, so it self-cleans across scene loads (no manual census, no
// scene wiring) — same pattern as NeedStationRegistry.
//
// Static — mirrors the GameEvents bus pattern — instead of a MonoBehaviour singleton, so there's no
// lifetime to manage. Agent ↔ perceivables are the SAME World domain, so this direct query is
// consistent with the NeedStationRegistry precedent, not a cross-system singleton lookup.
public static class PerceivableRegistry
{
    private static readonly List<Perceivable> all = new List<Perceivable>();

    public static void Register(Perceivable p)   { if (!all.Contains(p)) all.Add(p); }
    public static void Unregister(Perceivable p) => all.Remove(p);

    public static int Count => all.Count;

    // Non-alloc: clears and fills 'results' with every registered Perceivable (except 'exclude')
    // within 'radius' of 'from'. Caller owns ordering/filtering beyond that.
    public static void QueryInRadius(Vector3 from, float radius, Perceivable exclude, List<Perceivable> results)
    {
        results.Clear();
        float sqrRadius = radius * radius;

        foreach (var p in all)
        {
            if (p == null || p == exclude) continue;
            if ((p.Position - from).sqrMagnitude <= sqrRadius) results.Add(p);
        }
    }
}
}
