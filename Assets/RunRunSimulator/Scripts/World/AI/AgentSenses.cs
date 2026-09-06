using System;
using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

internal class AgentSenses
{
    private readonly MoriMochiAgent owner;
    private readonly AgentContext   ctx;

    private static readonly Comparison<Percept> BySqrDistanceAscending =
        (a, b) => a.SqrDistance.CompareTo(b.SqrDistance);

    private float nextScanAt;
    private bool  primed;
    private readonly List<Perceivable> buffer = new List<Perceivable>();
    private Perceivable selfPerceivable;
    private bool selfPerceivableResolved;

    internal AgentSenses(MoriMochiAgent owner, AgentContext ctx)
    {
        this.owner = owner;
        this.ctx   = ctx;
    }

    internal void Tick()
    {
        var t = SocialTuningSO.Current;
        if (t == null) return;

        if (!primed)
        {
            primed = true;
            nextScanAt = Time.time + UnityEngine.Random.Range(0f, t.ScanIntervalMin);
            return;
        }

        if (Time.time < nextScanAt) return;
        nextScanAt = Time.time + UnityEngine.Random.Range(t.ScanIntervalMin, t.ScanIntervalMax);

        if (!ctx.IsNavMeshControlled() || ctx.Dna == null)
        {
            ctx.Percepts.Clear();
            return;
        }

        if (!selfPerceivableResolved)
        {
            selfPerceivable = owner.GetComponent<Perceivable>();
            selfPerceivableResolved = true;
        }

        var rules = ExpeditionRulesSO.Current;
        bool cone = rules != null;
        float radius = t.PerceptionRadius;
        float degrees = 360f;
        float nearRadius = 0f;
        if (cone) VisionProfile.Resolve(ctx.Dna, rules, out radius, out degrees, out nearRadius);

        Vector3 origin = ctx.Body.position;
        Vector3 forward = ctx.Body.forward;

        PerceivableRegistry.QueryInRadius(origin, Mathf.Max(radius, nearRadius), selfPerceivable, buffer);

        ctx.Percepts.Clear();
        for (int i = 0; i < buffer.Count; i++)
        {
            var p = buffer[i];
            if (p.Kind == PerceivableKind.Monchi && p.Monchi == owner) continue;
            if (cone && !VisionProfile.CanSense(forward, origin, p.Position, radius, degrees, nearRadius)) continue;

            float affinity = (p.Kind == PerceivableKind.Monchi && p.Monchi != null && p.Monchi.DNA != null)
                ? SocialGraphService.EffectiveAffinity(ctx.Dna, p.Monchi.DNA, t)
                : 0f;

            ctx.Percepts.Add(new Percept
            {
                Source      = p,
                Kind        = p.Kind,
                SqrDistance = (p.Position - ctx.Body.position).sqrMagnitude,
                Affinity    = affinity,
                Team        = p.Team,
            });
        }

        ctx.Percepts.Sort(BySqrDistanceAscending);
        if (ctx.Percepts.Count > t.MaxPercepts)
            ctx.Percepts.RemoveRange(t.MaxPercepts, ctx.Percepts.Count - t.MaxPercepts);
    }

    internal void ResetForReuse()
    {
        nextScanAt = 0f;
        primed = false;
        ctx.Percepts.Clear();
    }
}
}
