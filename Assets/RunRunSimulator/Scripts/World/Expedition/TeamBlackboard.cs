using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

public struct KnownVein
{
    public MaterialPickup Vein;
    public int Remaining;
    public float ReportedAt;
}

public struct BlackboardPing
{
    public Vector3 Position;
    public float Time;
}

public class TeamBlackboard
{
    private const float PingKeepSeconds = 6f;

    private readonly List<MaterialPickup> sites = new();
    private readonly HashSet<MaterialPickup> visited = new();
    private readonly List<KnownVein> known = new();
    private readonly List<BlackboardPing> pings = new();
    private MaterialPickup lastSite;

    public TeamBlackboard(ExpeditionTeam team)
    {
        Team = team;
    }

    public ExpeditionTeam Team { get; }
    public IReadOnlyList<KnownVein> KnownVeins => known;
    public IReadOnlyList<BlackboardPing> Pings => pings;
    public int Reports { get; private set; }

    public void SetSites(IReadOnlyList<MaterialPickup> veins)
    {
        sites.Clear();
        visited.Clear();
        known.Clear();
        pings.Clear();
        lastSite = null;
        Reports = 0;
        if (veins == null) return;
        foreach (var vein in veins)
            if (vein != null) sites.Add(vein);
    }

    public MaterialPickup NextSite(Vector3 from, out bool newCycle)
    {
        newCycle = false;
        var pick = Nearest(from);
        if (pick == null && visited.Count > 0)
        {
            visited.Clear();
            newCycle = true;
            pick = Nearest(from);
        }
        return pick;
    }

    public void MarkVisited(MaterialPickup site)
    {
        if (site == null) return;
        visited.Add(site);
        lastSite = site;
    }

    public bool ReportVein(MaterialPickup vein, float now, float repeatSeconds)
    {
        if (vein == null) return false;

        int index = IndexOf(vein);
        bool fresh = index < 0 || (known[index].Remaining != vein.Remaining && now - known[index].ReportedAt >= repeatSeconds);
        var entry = new KnownVein { Vein = vein, Remaining = vein.Remaining, ReportedAt = fresh || index < 0 ? now : known[index].ReportedAt };

        if (index < 0) known.Add(entry);
        else known[index] = entry;

        if (!fresh) return false;

        Reports++;
        pings.Add(new BlackboardPing { Position = vein.transform.position, Time = now });
        PrunePings(now);
        return true;
    }

    public MaterialPickup BestKnownVein(Vector3 from, MaterialPickup exclude)
    {
        MaterialPickup best = null;
        float bestScore = 0f;

        for (int i = known.Count - 1; i >= 0; i--)
        {
            var vein = known[i].Vein;
            if (vein == null) { known.RemoveAt(i); continue; }
            if (vein.Taken || !vein.gameObject.activeInHierarchy || vein == exclude) continue;

            Vector3 d = vein.transform.position - from; d.y = 0f;
            float score = known[i].Remaining / (1f + d.magnitude * 0.15f);
            if (score <= bestScore) continue;

            best = vein;
            bestScore = score;
        }

        return best;
    }

    public void PrunePings(float now)
    {
        for (int i = pings.Count - 1; i >= 0; i--)
            if (now - pings[i].Time > PingKeepSeconds) pings.RemoveAt(i);
    }

    private MaterialPickup Nearest(Vector3 from)
    {
        MaterialPickup best = null;
        float bestSqr = float.PositiveInfinity;
        int candidates = 0;

        foreach (var site in sites)
        {
            if (site == null || site.Taken || !site.gameObject.activeInHierarchy || visited.Contains(site)) continue;
            candidates++;
        }

        foreach (var site in sites)
        {
            if (site == null || site.Taken || !site.gameObject.activeInHierarchy || visited.Contains(site)) continue;
            if (candidates > 1 && site == lastSite) continue;

            Vector3 d = site.transform.position - from; d.y = 0f;
            if (d.sqrMagnitude >= bestSqr) continue;

            best = site;
            bestSqr = d.sqrMagnitude;
        }

        return best;
    }

    private int IndexOf(MaterialPickup vein)
    {
        for (int i = 0; i < known.Count; i++)
            if (known[i].Vein == vein) return i;
        return -1;
    }
}
}
