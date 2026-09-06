using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

internal class AgentClash
{
    private enum Phase { None, Anticipating, Striking, Resolving, Dazed }

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
        if (ctx.Dna.Boldness < t.MinBoldness) return false;

        var occ = ctx.Occupation;
        if (occ == Occupation.None) occ = Occupation.Gather;
        if (occ == Occupation.Gather || occ == Occupation.Decoy) return false;

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

        var chosen = ChooseMove(t, rival, bestDist, occ);
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
                FaceTowards(target, dt);
                phaseTimer -= dt;
                if (phaseTimer <= 0f) StartStrike(t);
                break;

            case Phase.Striking:
                phaseTimer -= dt;
                if (target == null || target.IsHeld) { Resolve(t); break; }

                if (move.Slot == ClashSlot.Horn)
                {
                    ctx.SetDestinationSafe(target.transform.position);
                    if (PlanarDistance(target) <= move.HitRadius)
                    {
                        Impact(target, t);
                        if (phase == Phase.None) break;
                        owner.onClashHit?.Invoke();
                        Resolve(t);
                    }
                    else if (phaseTimer <= 0f)
                    {
                        Resolve(t);
                    }
                }
                else if (move.Slot == ClashSlot.Back)
                {
                    FaceTowards(target, dt);
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
    }

    internal CreatureIntent Intent => phase == Phase.Dazed ? CreatureIntent.Dazed : CreatureIntent.Clashing;

    internal MoriMochiAgent Target =>
        phase == Phase.Anticipating || phase == Phase.Striking ? target : null;

    internal string Gesture =>
        phase == Phase.Anticipating ? (move != null ? move.TellGesture   : "") :
        phase == Phase.Striking     ? (move != null ? move.StrikeGesture : "") :
        "";

    private ClashMoveSO ChooseMove(ClashTuningSO t, MoriMochiAgent rival, float dist, Occupation occ)
    {
        if (occ == Occupation.Break)
        {
            if (t.Wings != null && dist >= t.DiveMinDistance && dist <= t.Wings.Range) return t.Wings;
            if (t.Horn != null && dist <= t.Horn.Range) return t.Horn;
            return null;
        }

        if (t.Back != null && dist <= t.Back.Range && CountRivalsWithin(t.SweepRange) >= t.SweepMinRivals) return t.Back;
        if (t.Wings != null && dist >= t.DiveMinDistance && dist <= t.Wings.Range) return t.Wings;
        if (t.Horn != null && dist <= t.Horn.Range) return t.Horn;
        return null;
    }

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

        ctx.State = AgentState.Clashing;
        ctx.Agent.updateRotation = false;
        ctx.SetStopped(true);
        owner.EmitEmote(EmoteKind.Molesto);
        owner.onClashTell?.Invoke();

        if (move.AnticipationSeconds <= 0f) StartStrike(t);
    }

    private void StartStrike(ClashTuningSO t)
    {
        phase      = Phase.Striking;
        phaseTimer = move.StrikeSeconds;

        if (move.Slot == ClashSlot.Horn)
        {
            OverrideNav();
            ctx.Agent.updateRotation = true;
            ctx.SetStopped(false);
            ctx.SetDestinationSafe(target.transform.position);
        }
        else if (move.Slot == ClashSlot.Wings)
        {
            float   angle = move.LaunchAngle * Mathf.Deg2Rad;
            Vector3 aim   = target.transform.position;
            Vector3 v     = SpawnBallistics.SolveLaunchVelocity(ctx.Body.position, aim, angle);
            var     nav   = target.GetComponent<NavMeshAgent>();
            if (nav != null && nav.enabled)
            {
                float flight = 2f * v.y / Mathf.Max(0.01f, Mathf.Abs(Physics.gravity.y));
                Vector3 lead = nav.velocity; lead.y = 0f;
                aim += lead * flight;
                v    = SpawnBallistics.SolveLaunchVelocity(ctx.Body.position, aim, angle);
            }
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
        Vector3 dir = victim.transform.position - ctx.Body.position; dir.y = 0f;
        if (dir.sqrMagnitude <= 0.0001f) { dir = ctx.Body.forward; dir.y = 0f; }
        dir = dir.normalized;

        Vector3 force = (dir + Vector3.up * move.UpBias).normalized * move.Impulse;
        victim.ReceiveClashHit(owner, force);

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
        cooldownUntil = Time.time + (t != null ? t.Cooldown : 8f);
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
            Time.time >= cooldownUntil && t.Horn != null && PlanarDistance(attacker) <= t.EngageRange;

        if (canCounter)
        {
            Begin(t, t.Horn, attacker);
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
