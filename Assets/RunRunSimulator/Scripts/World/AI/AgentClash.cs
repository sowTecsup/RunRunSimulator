using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

internal class AgentClash
{
    private enum Phase { None, Anticipating, Holding, Striking, Resolving, Dazed }

    private readonly MoriMochiAgent owner;
    private readonly AgentContext   ctx;

    private MoriMochiAgent target;
    private ClashMoveSO    move;
    private Phase          phase;
    private float          phaseTimer;
    private float          cooldownUntil;
    private bool           diving;
    private bool           knockedByClash;
    private MoriMochiAgent lastAttacker;
    private float          targetableAt;
    private float          chainImmuneUntil;
    private int            hitsLanded;
    private int            timesKnocked;
    private Vector3        impactPoint;
    private Vector3        lockedForward;
    private bool           hornPathSettled;
    private readonly HashSet<MoriMochiAgent> struckThisStrike = new HashSet<MoriMochiAgent>();

    private bool                  navOverridden;
    private float                 savedSpeed;
    private float                 savedAcceleration;
    private ObstacleAvoidanceType savedAvoidance;

    private readonly List<Perceivable> buffer = new List<Perceivable>();

    internal AgentClash(MoriMochiAgent owner, AgentContext ctx)
    {
        this.owner = owner;
        this.ctx   = ctx;
    }

    internal bool TryEngage()
    {
        var t = ClashTuningSO.Current;
        if (t == null || ctx.Dna == null) return false;
        if (Time.time < cooldownUntil) return false;
        if (ctx.Orders.Contact != ContactChoice.Fight && ctx.Dna.Boldness < t.MinBoldness) return false;

        var occ = ctx.Occupation;
        if (occ == Occupation.None) occ = Occupation.Gather;
        if (occ == Occupation.Gather || occ == Occupation.Decoy || occ == Occupation.Explore) return false;

        MoriMochiAgent preferred     = null;
        float          preferredDist = float.MaxValue;
        MoriMochiAgent fallback      = null;
        float          fallbackDist  = float.MaxValue;

        for (int i = 0; i < ctx.Percepts.Count; i++)
        {
            var p = ctx.Percepts[i];
            if (p.Kind != PerceivableKind.Monchi) continue;
            if (p.Source == null || p.Source.Monchi == null) continue;
            if (!ExpeditionTeams.AreRivals(owner.Team, p.Team)) continue;

            var other = p.Source.Monchi;
            if (other.IsHeld || other.IsAirborne || other.IsRecovering || !other.IsClashTargetable) continue;
            if (occ == Occupation.Break && other.TrustedGuardian != null) continue;
            if (occ == Occupation.Break && !ExpeditionNav.IsThiefIntent(other.Intent)) continue;

            float dist = PlanarDistance(other);
            if (dist > t.EngageRange) continue;

            if (dist < fallbackDist) { fallbackDist = dist; fallback = other; }

            if (occ == Occupation.Break)
            {
                var intent = other.Intent;
                bool isThief = intent == CreatureIntent.Taking || intent == CreatureIntent.Carrying ||
                               intent == CreatureIntent.Securing || intent == CreatureIntent.Collecting;
                if (isThief && dist < preferredDist) { preferredDist = dist; preferred = other; }
            }
            else if (other.Intent == CreatureIntent.Taunting && dist < preferredDist)
            {
                preferredDist = dist;
                preferred     = other;
            }
        }

        MoriMochiAgent rival    = preferred != null ? preferred : fallback;
        float          bestDist = preferred != null ? preferredDist : fallbackDist;

        if (rival == null) return false;

        var chosen = owner.Abilities.TryFireDamage(bestDist, CountRivalsWithin(t.SweepRange), occ != Occupation.Break);
        if (chosen == null) return false;

        Begin(t, chosen, rival);
        return true;
    }

    internal bool ForceMove(ClashMoveSO move, MoriMochiAgent rival)
    {
        if (move == null || rival == null) return false;
        if (!ctx.IsNavMeshControlled() || ctx.State == AgentState.Clashing) return false;

        owner.RequestReleaseStation();
        owner.RequestRoam();
        Begin(ClashTuningSO.Current, move, rival);
        return true;
    }

    internal void TickClashing()
    {
        var t = ClashTuningSO.Current;
        if (t == null || (move == null && phase != Phase.Dazed)) { Finish(t); return; }

        float dt = Time.deltaTime;

        switch (phase)
        {
            case Phase.Anticipating:
                if (move.Slot != ClashSlot.Back) FaceTowards(target, dt);
                if (target != null)
                    impactPoint = move.Slot == ClashSlot.Back ? ctx.Body.position : target.transform.position;
                phaseTimer -= dt;
                if (phaseTimer <= 0f) EnterHolding(t);
                break;

            case Phase.Holding:
                phaseTimer -= dt;
                if (phaseTimer <= 0f) StartStrike(t);
                break;

            case Phase.Striking:
                phaseTimer -= dt;
                if (target == null || target.IsHeld) { Resolve(t); break; }

                if (move.Slot == ClashSlot.Horn)
                {
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
                        Impact(p.Monchi, t);
                        owner.onClashHit?.Invoke();
                    }

                    bool noPath = hornPathSettled && !ctx.Agent.hasPath && !ctx.Agent.pathPending;
                    hornPathSettled = true;
                    if (PlanarDistanceToPoint(impactPoint) <= 0.6f || phaseTimer <= 0f || noPath) Resolve(t);
                }
                else if (move.Slot == ClashSlot.Back)
                {
                    impactPoint = ctx.Body.position;
                    if (phaseTimer <= 0f)
                    {
                        if (Sweep(t)) owner.onClashHit?.Invoke();
                        Resolve(t);
                    }
                }
                break;

            case Phase.Resolving:
                phaseTimer -= dt;
                if (phaseTimer <= 0f) Finish(t);
                break;

            case Phase.Dazed:
                if (lastAttacker != null) FaceTowards(lastAttacker, dt);
                phaseTimer -= dt;
                if (phaseTimer <= 0f) Decide(t);
                break;
        }
    }

    internal void TickAirborne()
    {
        if (!diving || move == null) return;
        if (target == null || target.IsAirborne || target.IsHeld) { diving = false; return; }

        if (PlanarDistance(target) <= move.HitRadius && ctx.Rb.linearVelocity.y <= 0.5f)
        {
            Impact(target, ClashTuningSO.Current);
            owner.onClashHit?.Invoke();
            diving = false;
        }
    }

    internal void ReceiveHit(MoriMochiAgent attacker)
    {
        var t = ClashTuningSO.Current;
        knockedByClash   = true;
        lastAttacker     = attacker;
        chainImmuneUntil = Time.time + (t != null ? t.ChainImmunitySeconds : 0.8f);
        timesKnocked++;
        owner.onKnocked?.Invoke();
    }

    internal bool IsTargetable => phase != Phase.Dazed && Time.time >= targetableAt;

    internal bool IgnoresChainKnock(MoriMochiAgent other) =>
        other != null && other == lastAttacker && Time.time < chainImmuneUntil;

    internal void Cancel()
    {
        RestoreNav();
        phase  = Phase.None;
        target = null;
        move   = null;
        diving = false;
        struckThisStrike.Clear();
        lockedForward = Vector3.zero;
    }

    internal void OnRecovered()
    {
        RestoreNav();
        phase  = Phase.None;
        move   = null;
        target = null;
        bool wasDiving = diving;
        diving = false;

        var t = ClashTuningSO.Current;
        if (wasDiving) cooldownUntil = Time.time + (t != null ? t.Cooldown : 8f);

        if (knockedByClash && t != null)
        {
            targetableAt = Time.time + t.VictimGraceSeconds;
            if (t.DazedSeconds > 0f)
            {
                knockedByClash = false;
                phase          = Phase.Dazed;
                phaseTimer     = t.DazedSeconds;
                ctx.State      = AgentState.Clashing;
                ctx.Agent.updateRotation = false;
                ctx.SetStopped(true);
                return;
            }
        }

        knockedByClash = false;
        owner.RequestRoam();
    }

    internal void ResetForReuse()
    {
        RestoreNav();
        target         = null;
        move           = null;
        phase          = Phase.None;
        phaseTimer     = 0f;
        cooldownUntil  = 0f;
        diving           = false;
        knockedByClash   = false;
        lastAttacker     = null;
        targetableAt     = 0f;
        chainImmuneUntil = 0f;
        hitsLanded       = 0;
        timesKnocked     = 0;
        impactPoint      = Vector3.zero;
        struckThisStrike.Clear();
        lockedForward    = Vector3.zero;
    }

    internal float Cooldown01
    {
        get
        {
            var t = ClashTuningSO.Current;
            if (t == null || t.Cooldown <= 0f) return 0f;
            return Mathf.Clamp01((cooldownUntil - Time.time) / t.Cooldown);
        }
    }

    internal int HitsLanded => hitsLanded;
    internal int TimesKnocked => timesKnocked;

    internal CreatureIntent Intent => phase == Phase.Dazed ? CreatureIntent.Dazed : CreatureIntent.Clashing;

    internal MoriMochiAgent Target =>
        phase == Phase.Anticipating || phase == Phase.Holding || phase == Phase.Striking ? target : null;

    internal string Gesture =>
        phase == Phase.Anticipating || phase == Phase.Holding ? (move != null ? move.TellGesture   : "") :
        phase == Phase.Striking                                ? (move != null ? move.StrikeGesture : "") :
        "";

    internal ClashMoveSO Move =>
        phase == Phase.Anticipating || phase == Phase.Holding || phase == Phase.Striking || phase == Phase.Resolving ? move : null;

    internal bool Holding => phase == Phase.Holding;

    internal bool Telegraphing =>
        phase == Phase.Anticipating || phase == Phase.Holding ||
        (phase == Phase.Striking && (move == null || move.Slot != ClashSlot.Wings || diving));

    internal float Tell01 =>
        phase == Phase.Anticipating
            ? (move != null && move.AnticipationSeconds > 0f ? 1f - Mathf.Clamp01(phaseTimer / move.AnticipationSeconds) : 1f)
            : (phase == Phase.None || phase == Phase.Dazed ? 0f : 1f);

    internal Vector3 ImpactPoint => impactPoint;

    private int CountRivalsWithin(float r)
    {
        int count = 0;
        for (int i = 0; i < ctx.Percepts.Count; i++)
        {
            var p = ctx.Percepts[i];
            if (p.Kind != PerceivableKind.Monchi) continue;
            if (p.Source == null || p.Source.Monchi == null) continue;
            if (!ExpeditionTeams.AreRivals(owner.Team, p.Team)) continue;

            var other = p.Source.Monchi;
            if (other.IsHeld || other.IsAirborne || other.IsRecovering || !other.IsClashTargetable) continue;

            if (PlanarDistance(other) <= r) count++;
        }
        return count;
    }

    private void Begin(ClashTuningSO t, ClashMoveSO chosenMove, MoriMochiAgent rival)
    {
        target     = rival;
        move       = chosenMove;
        phase      = Phase.Anticipating;
        phaseTimer = move.AnticipationSeconds;
        diving     = false;
        impactPoint = rival.transform.position;

        ctx.State = AgentState.Clashing;
        ctx.Agent.updateRotation = false;
        ctx.SetStopped(true);
        owner.EmitEmote(EmoteKind.Molesto);
        owner.onClashTell?.Invoke();

        if (move.Slot == ClashSlot.Back)
        {
            Vector3 dir = rival.transform.position - ctx.Body.position; dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                ctx.Body.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        if (move.AnticipationSeconds <= 0f) EnterHolding(t);
    }

    private void EnterHolding(ClashTuningSO t)
    {
        Vector3 toTarget = Vector3.zero;
        if (target != null) { toTarget = target.transform.position - ctx.Body.position; toTarget.y = 0f; }

        if (toTarget.sqrMagnitude > 0.0001f)
        {
            lockedForward = toTarget.normalized;
        }
        else
        {
            Vector3 fwd = ctx.Body.forward; fwd.y = 0f;
            lockedForward = fwd.sqrMagnitude > 0.0001f ? fwd.normalized : Vector3.forward;
        }

        if (move.Slot == ClashSlot.Horn)
            impactPoint = ctx.Body.position + lockedForward * move.Range;
        else if (move.Slot == ClashSlot.Wings)
            impactPoint = ComputeWingsImpactPoint();

        phase      = Phase.Holding;
        phaseTimer = move.HoldSeconds;

        if (move.HoldSeconds <= 0f) StartStrike(t);
    }

    private Vector3 ComputeWingsImpactPoint()
    {
        float   angle  = move.LaunchAngle * Mathf.Deg2Rad;
        Vector3 aim    = target.transform.position;
        Vector3 v      = SpawnBallistics.SolveLaunchVelocity(ctx.Body.position, aim, angle);
        float   flight = 2f * v.y / Mathf.Max(0.01f, Mathf.Abs(Physics.gravity.y));

        var nav = target.GetComponent<NavMeshAgent>();
        if (nav != null && nav.enabled)
        {
            Vector3 lead = nav.velocity; lead.y = 0f;
            lead *= flight * 0.35f;
            if (lead.magnitude > 2.5f) lead = lead.normalized * 2.5f;
            aim += lead;
        }

        Vector3 fromOwner = aim - ctx.Body.position; fromOwner.y = 0f;
        float   minDist   = Mathf.Max(move.HitRadius, 2.5f);
        if (fromOwner.magnitude < minDist)
        {
            float aimY = aim.y;
            aim   = ctx.Body.position + lockedForward * minDist;
            aim.y = aimY;
        }

        return aim;
    }

    private void StartStrike(ClashTuningSO t)
    {
        phase      = Phase.Striking;
        phaseTimer = move.StrikeSeconds;
        struckThisStrike.Clear();

        if (move.Slot == ClashSlot.Horn)
        {
            OverrideNav();
            ctx.Agent.updateRotation = true;
            ctx.SetStopped(false);
            ctx.SetDestinationSafe(impactPoint);
            hornPathSettled = false;
        }
        else if (move.Slot == ClashSlot.Wings)
        {
            float   angle = move.LaunchAngle * Mathf.Deg2Rad;
            Vector3 v     = SpawnBallistics.SolveLaunchVelocity(ctx.Body.position, impactPoint, angle);
            diving = true;
            owner.Launch(ctx.Body.position, v);
        }
        else if (move.Slot == ClashSlot.Back)
        {
            ctx.SetStopped(true);
        }
    }

    private void Impact(MoriMochiAgent victim, ClashTuningSO t)
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

        if (move.Slot == ClashSlot.Horn && move.SelfRecoil > 0f)
        {
            RestoreNav();
            owner.RequestPlayfulKnock((-dir + Vector3.up * 0.3f).normalized * move.SelfRecoil);
        }
    }

    private bool Sweep(ClashTuningSO t)
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

            Impact(p.Monchi, t);
            hitAny = true;
        }
        return hitAny;
    }

    private void Resolve(ClashTuningSO t)
    {
        RestoreNav();
        ctx.SetStopped(true);
        ctx.Agent.updateRotation = false;
        phase      = Phase.Resolving;
        phaseTimer = t.ResolveSeconds;
        if (phaseTimer <= 0f) Finish(t);
    }

    private void Finish(ClashTuningSO t)
    {
        RestoreNav();
        phase         = Phase.None;
        target        = null;
        move          = null;
        diving        = false;
        cooldownUntil = Time.time + (t != null ? t.ResolveSeconds : 0.4f);
        ctx.Agent.updateRotation = true;
        owner.RequestRoam();
    }

    private void Decide(ClashTuningSO t)
    {
        phase = Phase.None;
        var attacker = lastAttacker;
        lastAttacker = null;
        ctx.Agent.updateRotation = true;

        bool canCounter =
            attacker != null && !attacker.IsHeld && !attacker.IsAirborne && !attacker.IsRecovering &&
            ctx.Dna != null && ctx.Dna.Boldness >= t.ReengageBoldness &&
            Time.time >= cooldownUntil && PlanarDistance(attacker) <= t.EngageRange;

        var counterMove = canCounter ? owner.Abilities.TryFireDamage(PlanarDistance(attacker), 0, true) : null;
        if (counterMove != null)
        {
            Begin(t, counterMove, attacker);
            return;
        }

        owner.RequestRoam();

        Vector3 away = attacker != null ? ctx.Body.position - attacker.transform.position : Vector3.zero;
        away.y = 0f;
        if (away.sqrMagnitude <= 0.0001f) { away = -ctx.Body.forward; away.y = 0f; }
        ctx.SetDestinationSafe(ctx.Body.position + away.normalized * t.RetreatDistance);
    }

    private void OverrideNav()
    {
        savedSpeed        = ctx.Agent.speed;
        savedAcceleration = ctx.Agent.acceleration;
        savedAvoidance    = ctx.Agent.obstacleAvoidanceType;

        ctx.Agent.speed                  = move.DashSpeed;
        ctx.Agent.acceleration           = move.DashAcceleration;
        ctx.Agent.obstacleAvoidanceType  = ObstacleAvoidanceType.NoObstacleAvoidance;
        navOverridden = true;
    }

    private void RestoreNav()
    {
        if (!navOverridden) return;
        ctx.Agent.speed                 = savedSpeed;
        ctx.Agent.acceleration          = savedAcceleration;
        ctx.Agent.obstacleAvoidanceType = savedAvoidance;
        navOverridden = false;
    }

    private float PlanarDistance(MoriMochiAgent other)
    {
        Vector3 d = other.transform.position - ctx.Body.position; d.y = 0f;
        return d.magnitude;
    }

    private float PlanarDistanceToPoint(Vector3 point)
    {
        Vector3 d = point - ctx.Body.position; d.y = 0f;
        return d.magnitude;
    }

    private void FaceTowards(MoriMochiAgent other, float dt)
    {
        if (other == null) return;
        Vector3 dir = other.transform.position - ctx.Body.position; dir.y = 0f;
        if (dir.sqrMagnitude <= 0.001f) return;
        ctx.Body.rotation = Quaternion.Slerp(
            ctx.Body.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), 12f * dt);
    }
}
}
