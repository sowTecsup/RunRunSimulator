using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

public static class PerceivableRegistry
{
    private static readonly List<Perceivable> all = new List<Perceivable>();

    public static void Register(Perceivable p)   { if (!all.Contains(p)) all.Add(p); }
    public static void Unregister(Perceivable p) => all.Remove(p);

    public static int Count => all.Count;

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
