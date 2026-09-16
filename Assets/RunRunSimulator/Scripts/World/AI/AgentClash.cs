using UnityEngine;
namespace MoriMonchiSimulator
{

internal class AgentClash
{
    private enum Phase { None, Anticipating, Holding, Striking, Resolving, Dazed }

    private readonly MoriMochiAgent owner;
    private readonly AgentContext   ctx;
    private readonly ClashStrike    strike;

    private MoriMochiAgent target;
    private ClashMoveSO    move;
    private Phase          phase;
    private float          phaseTimer;
    private float          cooldownUntil;
    private bool           knockedByClash;
    private MoriMochiAgent lastAttacker;
    private float          targetableAt;
    private float          chainImmuneUntil;
    private int            timesKnocked;
    private Vector3        impactPoint;
    private Vector3        lockedForward;
    private float          riseFromY;
    private const float    LiftOffClearance = 0.15f;

    private float approachStartedAt;
    private float approachRetickAt;

    private bool  navOverridden;
    private float savedSpeed;

    internal AgentClash(MoriMochiAgent owner, AgentContext ctx)
    {
        this.owner  = owner;
        this.ctx    = ctx;
        this.strike = new ClashStrike(owner, ctx);
    }

    internal bool TryEngage()
    {
        var t = ClashTuningSO.Current;
        if (t == null || ctx.Dna == null) return false;
        if (Time.time < cooldownUntil) return false;
        bool superRequested = owner.Abilities.HasRequestedSuper;
        if (!superRequested && ctx.Orders.Contact != ContactChoice.Fight && ctx.Dna.Boldness < t.MinBoldness) return false;

        var occ = ctx.Occupation;
        if (occ == Occupation.None) occ = Occupation.Gather;
        if (!superRequested && (occ == Occupation.Gather || occ == Occupation.Decoy || occ == Occupation.Explore)) return false;

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
            if (occ == Occupation.Break && !superRequested && other.TrustedGuardian != null) continue;
            if (occ == Occupation.Break && !superRequested && !ExpeditionNav.IsThiefIntent(other.Intent)) continue;

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
                if (move.Slot == ClashSlot.Wings)
                {
                    FaceTowards(target, dt);
                    if (target != null) impactPoint = target.transform.position;
                    phaseTimer -= dt;
                    if (phaseTimer <= 0f) EnterHolding(t);
                }
                else
                {
                    TickApproach(t);
                }
                break;

            case Phase.Holding:
                phaseTimer -= dt;
                if (phaseTimer <= 0f) StartStrike(t);
                break;

            case Phase.Striking:
                if (strike.Tick(dt)) Resolve(t);
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
        if (!strike.Diving || move == null) return;
        var t = ClashTuningSO.Current;
        if (phase == Phase.Holding)
        {
            if (ctx.Rb.linearVelocity.y <= 0.05f && ctx.Body.position.y > riseFromY + 0.3f) StartStrike(t);
            return;
        }
        if (phase != Phase.Striking) return;

        strike.TickAirborne();
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
        strike.Cancel();
        phase         = Phase.None;
        target        = null;
        move          = null;
        lockedForward = Vector3.zero;
    }

    internal void OnRecovered()
    {
        RestoreNav();
        phase  = Phase.None;
        move   = null;
        target = null;
        bool wasDiving = strike.Diving;
        strike.Cancel();

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
        strike.ResetForReuse();
        target            = null;
        move              = null;
        phase             = Phase.None;
        phaseTimer        = 0f;
        cooldownUntil     = 0f;
        knockedByClash    = false;
        lastAttacker      = null;
        targetableAt      = 0f;
        chainImmuneUntil  = 0f;
        timesKnocked      = 0;
        impactPoint       = Vector3.zero;
        lockedForward     = Vector3.zero;
        approachStartedAt = 0f;
        approachRetickAt  = 0f;
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

    internal int HitsLanded => strike.HitsLanded;
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
        (phase == Phase.Striking && (move == null || move.Slot != ClashSlot.Wings || strike.Diving));

    internal float Tell01
    {
        get
        {
            if (phase == Phase.None || phase == Phase.Dazed) return 0f;
            if (phase != Phase.Anticipating) return 1f;
            if (move == null) return 1f;

            if (move.Slot == ClashSlot.Wings)
                return move.AnticipationSeconds > 0f ? 1f - Mathf.Clamp01(phaseTimer / move.AnticipationSeconds) : 1f;

            var   t           = ClashTuningSO.Current;
            float commitDist  = move.Slot == ClashSlot.Horn ? move.Range * 0.6f : move.SweepRadius * 0.8f;
            float engageRange = t != null ? t.EngageRange : commitDist + 1f;
            float dist        = target != null ? PlanarDistance(target) : commitDist;
            return Mathf.Clamp01(1f - Mathf.Clamp01((dist - commitDist) / Mathf.Max(0.01f, engageRange - commitDist)));
        }
    }

    internal Vector3 ImpactPoint => phase == Phase.Striking ? strike.ImpactPoint : impactPoint;
    internal float   HitAt => strike.HitAt;
    internal Vector3 HitPoint => strike.HitPoint;

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
        target      = rival;
        move        = chosenMove;
        impactPoint = rival.transform.position;

        ctx.State = AgentState.Clashing;
        owner.EmitEmote(EmoteKind.Molesto);
        owner.onClashTell?.Invoke();

        if (move.Slot == ClashSlot.Wings)
        {
            phase      = Phase.Anticipating;
            phaseTimer = move.AnticipationSeconds;
            ctx.Agent.updateRotation = false;
            ctx.SetStopped(true);
            if (move.AnticipationSeconds <= 0f) EnterHolding(t);
            return;
        }

        phase             = Phase.Anticipating;
        approachStartedAt = Time.time;
        approachRetickAt  = 0f;
        ctx.Agent.updateRotation = true;
        ctx.SetStopped(false);
        savedSpeed      = ctx.Agent.speed;
        ctx.Agent.speed = Mathf.Max(savedSpeed, t.ApproachSpeed);
        navOverridden   = true;

        if (move.Slot == ClashSlot.Back)
        {
            Vector3 dir = rival.transform.position - ctx.Body.position; dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                ctx.Body.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }
    }

    private void TickApproach(ClashTuningSO t)
    {
        if (target == null || target.IsHeld || target.IsAirborne || !target.IsClashTargetable ||
            Time.time - approachStartedAt > t.ApproachSeconds)
        {
            Finish(t);
            return;
        }

        float dist = PlanarDistance(target);
        if (dist > t.EngageRange * 1.3f) { Finish(t); return; }

        if (Time.time >= approachRetickAt)
        {
            ctx.SetDestinationSafe(target.transform.position);
            approachRetickAt = Time.time + 0.15f;
        }

        impactPoint = move.Slot == ClashSlot.Horn ? ComputeHornImpactPoint() : ctx.Body.position;

        float commitDist = move.Slot == ClashSlot.Horn ? move.Range * 0.6f : move.SweepRadius * 0.8f;
        if (dist <= commitDist && WithinCommitAngle(t)) EnterHolding(t);
    }

    private bool WithinCommitAngle(ClashTuningSO t)
    {
        Vector3 toTarget = target.transform.position - ctx.Body.position; toTarget.y = 0f;
        Vector3 fwd      = ctx.Body.forward; fwd.y = 0f;
        if (toTarget.sqrMagnitude <= 0.0001f || fwd.sqrMagnitude <= 0.0001f) return true;
        return Vector3.Angle(fwd.normalized, toTarget.normalized) <= t.CommitAngle;
    }

    private Vector3 ComputeHornImpactPoint()
    {
        Vector3 dir = target.transform.position - ctx.Body.position; dir.y = 0f;
        dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : new Vector3(ctx.Body.forward.x, 0f, ctx.Body.forward.z).normalized;
        return ctx.Body.position + dir * move.Range;
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
        {
            ctx.SetStopped(true);
            ctx.Agent.updateRotation = false;
            impactPoint = ctx.Body.position + lockedForward * move.Range;
            phase       = Phase.Holding;
            phaseTimer  = move.HoldSeconds;
            if (move.HoldSeconds <= 0f) StartStrike(t);
        }
        else if (move.Slot == ClashSlot.Back)
        {
            ctx.SetStopped(true);
            ctx.Agent.updateRotation = false;
            impactPoint = ctx.Body.position;
            phase       = Phase.Holding;
            phaseTimer  = move.HoldSeconds;
            if (move.HoldSeconds <= 0f) StartStrike(t);
        }
        else if (move.Slot == ClashSlot.Wings)
        {
            impactPoint = strike.WingsImpactPoint(move, target, lockedForward);
            float riseSpeed = Mathf.Sqrt(2f * Mathf.Abs(Physics.gravity.y) * move.RiseHeight);
            strike.BeginDive();
            riseFromY = ctx.Body.position.y;
            owner.Launch(ctx.Body.position + Vector3.up * LiftOffClearance, Vector3.up * riseSpeed);
            owner.onDiveLaunch?.Invoke();
            ctx.Rb.linearDamping = 0f;
            phase      = Phase.Holding;
            phaseTimer = move.HoldSeconds;
        }
    }

    private void StartStrike(ClashTuningSO t)
    {
        phase = Phase.Striking;
        strike.Begin(move, target, lockedForward, impactPoint, move.StrikeSeconds);
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

    private void RestoreNav()
    {
        if (!navOverridden) return;
        ctx.Agent.speed = savedSpeed;
        navOverridden   = false;
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
