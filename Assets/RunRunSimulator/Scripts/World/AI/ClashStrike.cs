using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

internal class ClashStrike
{
    private readonly MoriMochiAgent owner;
    private readonly AgentContext   ctx;

    private ClashMoveSO    move;
    private MoriMochiAgent target;
    private Vector3        lockedForward;
    private Vector3        impactPoint;
    private float          phaseTimer;
    private bool           diving;
    private Vector3        dashStart;
    private int            stuckFrames;
    private int            hitsLanded;
    private float          hitAt = -1f;
    private Vector3        hitPoint;

    private bool                  avoidanceOverridden;
    private ObstacleAvoidanceType savedAvoidance;

    private readonly HashSet<MoriMochiAgent> struckThisStrike = new HashSet<MoriMochiAgent>();
    private readonly List<Perceivable>       buffer           = new List<Perceivable>();

    internal ClashStrike(MoriMochiAgent owner, AgentContext ctx)
    {
        this.owner = owner;
        this.ctx   = ctx;
    }

    internal float   HitAt       => hitAt;
    internal Vector3 HitPoint    => hitPoint;
    internal int     HitsLanded  => hitsLanded;
    internal bool    Diving      => diving;
    internal Vector3 ImpactPoint => impactPoint;

    internal void BeginDive() => diving = true;

    internal Vector3 WingsImpactPoint(ClashMoveSO wingsMove, MoriMochiAgent wingsTarget, Vector3 forward)
    {
        Vector3 aim    = wingsTarget.transform.position;
        float   g      = Mathf.Abs(Physics.gravity.y);
        float   flight = Mathf.Sqrt(2f * wingsMove.RiseHeight / g) + wingsMove.DiveSeconds;

        var nav = wingsTarget.GetComponent<NavMeshAgent>();
        if (nav != null && nav.enabled)
        {
            Vector3 lead = nav.velocity; lead.y = 0f;
            lead *= flight * 0.35f;
            if (lead.magnitude > 2.5f) lead = lead.normalized * 2.5f;
            aim += lead;
        }

        Vector3 fromOwner = aim - ctx.Body.position; fromOwner.y = 0f;
        float   minDist   = Mathf.Max(wingsMove.HitRadius, 2.5f);
        if (fromOwner.magnitude < minDist)
        {
            float aimY = aim.y;
            aim   = ctx.Body.position + forward * minDist;
            aim.y = aimY;
        }

        return aim;
    }

    internal void Begin(ClashMoveSO clashMove, MoriMochiAgent clashTarget, Vector3 forward, Vector3 point, float strikeSeconds)
    {
        move          = clashMove;
        target        = clashTarget;
        lockedForward = forward;
        impactPoint   = point;
        phaseTimer    = strikeSeconds;
        struckThisStrike.Clear();

        if (move.Slot == ClashSlot.Horn)
        {
            OverrideNav();
            ctx.SetStopped(true);
            ctx.Agent.velocity = Vector3.zero;
            ctx.Agent.updateRotation = false;
            ctx.Body.rotation = Quaternion.LookRotation(lockedForward, Vector3.up);
            dashStart   = ctx.Body.position;
            stuckFrames = 0;
        }
        else if (move.Slot == ClashSlot.Wings)
        {
            float   T = move.DiveSeconds;
            Vector3 d = impactPoint - ctx.Body.position;
            ctx.Rb.linearVelocity = (d - 0.5f * Physics.gravity * T * T) / T;
        }
        else if (move.Slot == ClashSlot.Back)
        {
            ctx.SetStopped(true);
        }
    }

    internal bool Tick(float dt)
    {
        phaseTimer -= dt;
        if (target == null || target.IsHeld) { RestoreNav(); return true; }

        if (move.Slot == ClashSlot.Horn)
        {
            Vector3 step   = lockedForward * move.DashSpeed * dt;
            Vector3 before = ctx.Body.position;
            ctx.Agent.Move(step);
            ctx.Rb.position = ctx.Body.position;

            float moved = PlanarDistance(ctx.Body.position, before);
            stuckFrames = moved < step.magnitude * 0.2f ? stuckFrames + 1 : 0;

            buffer.Clear();
            PerceivableRegistry.QueryInRadius(ctx.Body.position, move.HitRadius, null, buffer);
            for (int i = 0; i < buffer.Count; i++)
            {
                var p = buffer[i];
                if (p == null || p.Monchi == null || p.Monchi == owner) continue;
                if (!ExpeditionTeams.AreRivals(owner.Team, p.Monchi.Team)) continue;
                if (p.Monchi.IsAirborne || p.Monchi.IsHeld || !p.Monchi.IsClashTargetable) continue;
                if (struckThisStrike.Contains(p.Monchi)) continue;

                struckThisStrike.Add(p.Monchi);
                Impact(p.Monchi);
                owner.onClashHit?.Invoke();
            }

            float traveled = PlanarDistance(ctx.Body.position, dashStart);
            bool  done     = traveled >= move.Range || phaseTimer <= 0f || stuckFrames >= 2;
            if (done) RestoreNav();
            return done;
        }

        if (move.Slot == ClashSlot.Back)
        {
            impactPoint = ctx.Body.position;
            if (phaseTimer <= 0f)
            {
                if (Sweep()) owner.onClashHit?.Invoke();
                return true;
            }
            return false;
        }

        return false;
    }

    internal bool TickAirborne()
    {
        bool landed  = ctx.Rb.linearVelocity.y <= 0f && ctx.Body.position.y <= impactPoint.y + 0.4f;
        bool arrived = PlanarDistance(ctx.Body.position, impactPoint) <= 0.5f && ctx.Body.position.y <= impactPoint.y + 0.8f;
        if (!landed && !arrived) return false;

        Land();
        return true;
    }

    internal void Cancel()
    {
        RestoreNav();
        if (diving) ctx.Rb.linearDamping = owner.thrownLinearDamping;
        diving = false;
        struckThisStrike.Clear();
    }

    internal void ResetForReuse()
    {
        RestoreNav();
        move          = null;
        target        = null;
        diving        = false;
        hitAt         = -1f;
        hitPoint      = Vector3.zero;
        hitsLanded    = 0;
        impactPoint   = Vector3.zero;
        lockedForward = Vector3.zero;
        struckThisStrike.Clear();
    }

    private void Land()
    {
        buffer.Clear();
        PerceivableRegistry.QueryInRadius(impactPoint, move.HitRadius, null, buffer);

        bool hitAny = false;
        for (int i = 0; i < buffer.Count; i++)
        {
            var p = buffer[i];
            if (p == null || p.Monchi == null || p.Monchi == owner) continue;
            if (!ExpeditionTeams.AreRivals(owner.Team, p.Monchi.Team)) continue;
            if (p.Monchi.IsAirborne || p.Monchi.IsHeld || !p.Monchi.IsClashTargetable) continue;

            Impact(p.Monchi);
            hitAny = true;
        }
        if (hitAny) owner.onClashHit?.Invoke();

        diving = false;
        ctx.Rb.linearDamping  = owner.thrownLinearDamping;
        ctx.Rb.linearVelocity = Vector3.down * 2f;
    }

    private bool Sweep()
    {
        buffer.Clear();
        PerceivableRegistry.QueryInRadius(ctx.Body.position, move.SweepRadius, null, buffer);

        bool hitAny = false;
        for (int i = 0; i < buffer.Count; i++)
        {
            var p = buffer[i];
            if (p == null || p.Monchi == null || p.Monchi == owner) continue;
            if (!ExpeditionTeams.AreRivals(owner.Team, p.Monchi.Team)) continue;
            if (p.Monchi.IsAirborne || p.Monchi.IsHeld || !p.Monchi.IsClashTargetable) continue;

            Impact(p.Monchi);
            hitAny = true;
        }
        return hitAny;
    }

    private void Impact(MoriMochiAgent victim)
    {
        Vector3 dir;
        if (move.Slot == ClashSlot.Horn)
        {
            Vector3 towards = victim.transform.position - ctx.Body.position; towards.y = 0f;
            towards = towards.sqrMagnitude > 0.0001f ? towards.normalized : lockedForward;
            dir = (lockedForward * 0.6f + towards * 0.4f).normalized;
        }
        else
        {
            dir = victim.transform.position - ctx.Body.position; dir.y = 0f;
            if (dir.sqrMagnitude <= 0.0001f) { dir = ctx.Body.forward; dir.y = 0f; }
            dir = dir.normalized;
        }

        Vector3 force = (dir + Vector3.up * move.UpBias).normalized * move.Impulse;
        victim.ReceiveClashHit(owner, force);
        hitsLanded++;
        owner.NotifyCharge(AbilityChargeSource.Hit);
        hitAt    = Time.time;
        hitPoint = victim.transform.position;

        if (move.Slot == ClashSlot.Horn && move.SelfRecoil > 0f)
        {
            RestoreNav();
            owner.RequestPlayfulKnock((-dir + Vector3.up * 0.3f).normalized * move.SelfRecoil);
        }
    }

    private void OverrideNav()
    {
        savedAvoidance = ctx.Agent.obstacleAvoidanceType;
        ctx.Agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
        avoidanceOverridden = true;
    }

    private void RestoreNav()
    {
        if (!avoidanceOverridden) return;
        ctx.Agent.obstacleAvoidanceType = savedAvoidance;
        avoidanceOverridden = false;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        Vector3 d = a - b; d.y = 0f;
        return d.magnitude;
    }
}
}
