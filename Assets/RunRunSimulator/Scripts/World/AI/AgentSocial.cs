using UnityEngine;
namespace MoriMonchiSimulator
{

// Social decision + pair-play collaborator of the agent composition (mirrors AgentConfinement's
// courtship). Reads ctx.Percepts (written by AgentSenses) against the role's ReactionRuleBase
// list to decide whether to approach a liked neighbor or invite one to a mutual chase, then owns
// the Socializing state end-to-end. The chase handshake is a mirror of EnterCourtship: the
// initiator asks TryJoinSocialPlay on the target and only proceeds if it accepts. Once both sides
// are in, there's no further cross-calls — each side detects the other left play passively, by
// polling partner.IsSocializing every tick, so neither side needs a callback into the other.
internal class AgentSocial
{
    private enum SocialMode { None, Approach, Chaser, Runner, Sleeping, Fighting }

    private readonly MoriMochiAgent owner;
    private readonly AgentContext   ctx;

    private SocialMode     mode;
    private MoriMochiAgent partner;
    private float          timer;
    private float          duration;
    private float          repathTimer;
    private float          cooldownUntil;
    private bool           swapped;
    private NeedStation    sleepStation;
    private Vector3        sleepSpot;
    private float          lungeTimer;
    private float          emoteTimer;

    internal AgentSocial(MoriMochiAgent owner, AgentContext ctx)
    {
        this.owner = owner;
        this.ctx   = ctx;
    }

    // Called by MoriMochiAgent.Update only when the brain's own tick left the state at
    // Idle/Roaming this frame — social decisions never preempt a need or a reaction.
    internal bool TryEngage()
    {
        var t = SocialTuningSO.Current;
        if (t == null) return false;
        if (Time.time < cooldownUntil) return false;
        if (ctx.CurrentContainer != null) return false;
        if (ctx.IsBreeding) return false;
        if (ctx.Percepts.Count == 0) return false;

        var rules = ctx.Profile?.Reactions;
        if (rules == null || rules.Count == 0) return false;

        float bestScore = float.NegativeInfinity;
        Percept bestPercept = default;
        ReactionRuleBase bestRule = null;

        for (int i = 0; i < ctx.Percepts.Count; i++)
        {
            var p = ctx.Percepts[i];
            for (int j = 0; j < rules.Count; j++)
            {
                var rule = rules[j];
                if (rule == null || rule.Action == SocialAction.Avoid) continue;
                if (!rule.Matches(p, owner, t, out float score)) continue;
                if (score <= bestScore) continue;

                bestScore   = score;
                bestPercept = p;
                bestRule    = rule;
            }
        }

        if (bestRule == null) return false;

        switch (bestRule.Action)
        {
            case SocialAction.Approach:      return BeginApproach(bestPercept, t);
            case SocialAction.PlayChase:     return BeginPlayChase(bestPercept, t);
            case SocialAction.SleepTogether: return BeginSleep(bestPercept, t);
            case SocialAction.Fight:         return BeginFight(bestPercept, t);
            default:                         return false;
        }
    }

    private bool BeginApproach(in Percept p, SocialTuningSO t)
    {
        var target = p.Source != null ? p.Source.Monchi : null;
        if (target == null) return false;

        partner     = target;
        mode        = SocialMode.Approach;
        timer       = 0f;
        duration    = t.ApproachDuration;
        repathTimer = 0f;

        ctx.State                = AgentState.Socializing;
        ctx.SetStopped(false);
        ctx.Agent.updateRotation = true;
        owner.EmitEmote(EmoteKind.Curioso);
        return true;
    }

    private bool BeginPlayChase(in Percept p, SocialTuningSO t)
    {
        var target = p.Source != null ? p.Source.Monchi : null;
        if (target == null) return false;
        if (!target.TryJoinSocialPlay(owner)) return false;

        partner     = target;
        mode        = SocialMode.Chaser;
        timer       = 0f;
        swapped     = false;
        duration    = t.ChaseDuration;
        repathTimer = 0f;

        ctx.State                = AgentState.Socializing;
        ctx.SetStopped(false);
        ctx.Agent.updateRotation = true;
        owner.EmitEmote(EmoteKind.Jugando);
        return true;
    }

    // The receiving side of the handshake — validated independently, since the initiator can't
    // see this creature's own needs/state.
    internal bool TryJoinSocialPlay(MoriMochiAgent initiator)
    {
        var t = SocialTuningSO.Current;
        if (t == null) return false;
        if (initiator == null) return false;
        if (Time.time < cooldownUntil) return false;
        if (ctx.State != AgentState.Idle && ctx.State != AgentState.Roaming) return false;
        if (ctx.CurrentContainer != null) return false;
        if (ctx.IsBreeding) return false;
        if (ctx.Dna == null) return false;
        if (ctx.Dna.Needs.Energy < t.MinEnergyToPlay) return false;
        if (owner.Condition != CreatureCondition.Healthy) return false;

        owner.RequestReleaseStation();

        partner     = initiator;
        mode        = SocialMode.Runner;
        timer       = 0f;
        swapped     = false;
        duration    = t.ChaseDuration;
        repathTimer = 0f;

        ctx.State                = AgentState.Socializing;
        ctx.SetStopped(false);
        ctx.Agent.updateRotation = true;
        owner.EmitEmote(EmoteKind.Jugando);
        return true;
    }

    // Sims-style shared nap: looks for a free Energy station near itself first (both sides end up
    // reserving their own slot at the SAME station when one is available); if there's none, they
    // just lie down at the midpoint between them.
    private bool BeginSleep(in Percept p, SocialTuningSO t)
    {
        var target = p.Source != null ? p.Source.Monchi : null;
        if (target == null) return false;

        var station = NeedStationRegistry.GetClosest(ctx.Body.position, NeedType.Energy);
        if (station != null && station.TryReserve(owner, ctx.Body.position, ctx.Agent.areaMask, owner.sampleRadius, out var usePos))
        {
            sleepStation = station;
            sleepSpot    = usePos;
        }
        else
        {
            sleepStation = null;
            sleepSpot    = (ctx.Body.position + target.transform.position) * 0.5f;
        }

        if (!target.TryJoinSocialSleep(owner, sleepStation, sleepSpot))
        {
            if (sleepStation != null) { sleepStation.Release(owner); sleepStation = null; }
            return false;
        }

        partner     = target;
        mode        = SocialMode.Sleeping;
        timer       = 0f;
        duration    = t.SleepDuration;
        repathTimer = 0f;
        emoteTimer  = 0f;

        ctx.State                = AgentState.Socializing;
        ctx.SetStopped(false);
        ctx.Agent.updateRotation = true;
        owner.EmitEmote(EmoteKind.Zzz);
        return true;
    }

    // The receiving side of the sleep handshake. Tries its OWN slot at the station the initiator
    // found (so both end up served by the same furniture piece when possible); falls back to a
    // spot just off to the side of the initiator's fallback midpoint so they don't overlap.
    internal bool TryJoinSleep(MoriMochiAgent initiator, NeedStation station, Vector3 fallbackSpot)
    {
        var t = SocialTuningSO.Current;
        if (t == null) return false;
        if (initiator == null) return false;
        if (Time.time < cooldownUntil) return false;
        if (ctx.State != AgentState.Idle && ctx.State != AgentState.Roaming) return false;
        if (ctx.CurrentContainer != null) return false;
        if (ctx.IsBreeding) return false;
        if (ctx.Dna == null) return false;
        if (ctx.Dna.Needs.Energy > t.MaxEnergyToSleep) return false;
        if (owner.Condition == CreatureCondition.Sick) return false;

        owner.RequestReleaseStation();

        if (station != null && station.TryReserve(owner, ctx.Body.position, ctx.Agent.areaMask, owner.sampleRadius, out var usePos))
        {
            sleepStation = station;
            sleepSpot    = usePos;
        }
        else
        {
            Vector3 delta = ctx.Body.position - fallbackSpot; delta.y = 0f;
            Vector3 side  = delta.sqrMagnitude < 0.001f ? Vector3.right : delta.normalized;
            sleepStation  = null;
            sleepSpot     = fallbackSpot + side * 0.8f;
        }

        partner     = initiator;
        mode        = SocialMode.Sleeping;
        timer       = 0f;
        duration    = t.SleepDuration;
        repathTimer = 0f;
        emoteTimer  = 0f;

        ctx.State                = AgentState.Socializing;
        ctx.SetStopped(false);
        ctx.Agent.updateRotation = true;
        owner.EmitEmote(EmoteKind.Zzz);
        return true;
    }

    private bool BeginFight(in Percept p, SocialTuningSO t)
    {
        var target = p.Source != null ? p.Source.Monchi : null;
        if (target == null) return false;
        if (!target.TryJoinSocialFight(owner)) return false;

        partner     = target;
        mode        = SocialMode.Fighting;
        timer       = 0f;
        duration    = t.FightDuration;
        repathTimer = 0f;
        lungeTimer  = 0f;
        emoteTimer  = 0f;

        ctx.State                = AgentState.Socializing;
        ctx.SetStopped(false);
        ctx.Agent.updateRotation = true;
        owner.EmitEmote(EmoteKind.Molesto);
        return true;
    }

    // The receiving side of the fight handshake — same fitness gate as a play chase (a gremlin
    // scuffle is still play, not real combat), just a different mode/duration.
    internal bool TryJoinFight(MoriMochiAgent initiator)
    {
        var t = SocialTuningSO.Current;
        if (t == null) return false;
        if (initiator == null) return false;
        if (Time.time < cooldownUntil) return false;
        if (ctx.State != AgentState.Idle && ctx.State != AgentState.Roaming) return false;
        if (ctx.CurrentContainer != null) return false;
        if (ctx.IsBreeding) return false;
        if (ctx.Dna == null) return false;
        if (ctx.Dna.Needs.Energy < t.MinEnergyToPlay) return false;
        if (owner.Condition != CreatureCondition.Healthy) return false;

        owner.RequestReleaseStation();

        partner     = initiator;
        mode        = SocialMode.Fighting;
        timer       = 0f;
        duration    = t.FightDuration;
        repathTimer = 0f;
        lungeTimer  = 0f;
        emoteTimer  = 0f;

        ctx.State                = AgentState.Socializing;
        ctx.SetStopped(false);
        ctx.Agent.updateRotation = true;
        owner.EmitEmote(EmoteKind.Molesto);
        return true;
    }

    internal void TickSocializing()
    {
        if (mode == SocialMode.None) { End(false); return; }
        if (partner == null) { End(false); return; }

        bool isChasePair = mode == SocialMode.Chaser || mode == SocialMode.Runner;
        bool isPairedUp  = isChasePair || mode == SocialMode.Sleeping || mode == SocialMode.Fighting;
        if (isPairedUp && !partner.IsSocializing)
        {
            End(false);
            return;
        }

        var t = SocialTuningSO.Current;
        if (t == null) { End(false); return; }

        timer += Time.deltaTime;

        if (isChasePair || mode == SocialMode.Fighting)
            ctx.Dna?.Needs.AddEnergy(-t.ChaseEnergyPerSecond * Time.deltaTime);

        if (isChasePair && !swapped && timer >= duration * t.ChaseSwapFraction)
        {
            swapped = true;
            mode    = mode == SocialMode.Chaser ? SocialMode.Runner : SocialMode.Chaser;
            owner.EmitEmote(EmoteKind.Jugando);
        }

        // Sleeping/Fighting tick every frame — their energy regen / emote cadence / lunge timer
        // can't wait on the chase repath gate below — so they bypass it entirely and own their
        // own completion check.
        if (mode == SocialMode.Sleeping) { TickSleeping(t); return; }
        if (mode == SocialMode.Fighting) { TickFighting(t); return; }

        if (timer >= duration) { End(true); return; }

        repathTimer -= Time.deltaTime;
        if (repathTimer > 0f) return;
        repathTimer = t.ChaseRepath;

        switch (mode)
        {
            case SocialMode.Approach: TickApproach(t); break;
            case SocialMode.Chaser:   ctx.SetDestinationSafe(partner.transform.position); break;
            case SocialMode.Runner:   TickRunner(t); break;
        }
    }

    private void TickApproach(SocialTuningSO t)
    {
        Vector3 to = partner.transform.position - ctx.Body.position; to.y = 0f;
        if (to.magnitude <= t.ApproachStopDistance)
        {
            ctx.SetStopped(true);
            FacePartner();
        }
        else
        {
            ctx.SetStopped(false);
            ctx.SetDestinationSafe(partner.transform.position - to.normalized * t.ApproachStopDistance);
        }
    }

    private void TickRunner(SocialTuningSO t)
    {
        Vector3 dir = ctx.Body.position - partner.transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f)
        {
            Vector2 rnd = Random.insideUnitCircle;
            dir = new Vector3(rnd.x, 0f, rnd.y);
        }
        dir = dir.normalized;

        Vector3 jitter = Random.insideUnitSphere * 1f; jitter.y = 0f;
        ctx.SetDestinationSafe(ctx.Body.position + dir * t.ChaseFleeStep + jitter);
    }

    private void TickSleeping(SocialTuningSO t)
    {
        Vector3 to = sleepSpot - ctx.Body.position; to.y = 0f;
        if (to.magnitude > t.SleepStopDistance)
        {
            ctx.SetStopped(false);
            ctx.SetDestinationSafe(sleepSpot);
        }
        else
        {
            ctx.SetStopped(true);
            FacePartner();
        }

        ctx.Dna?.Needs.AddEnergy(t.SleepEnergyPerSecond * Time.deltaTime);

        emoteTimer -= Time.deltaTime;
        if (emoteTimer <= 0f)
        {
            emoteTimer = 3f;
            owner.EmitEmote(EmoteKind.Zzz);
        }

        if (ctx.Dna != null && ctx.Dna.Needs.Energy >= 100f) { End(true); return; }
        if (timer >= duration) End(true);
    }

    private void TickFighting(SocialTuningSO t)
    {
        FacePartner();

        lungeTimer -= Time.deltaTime;
        if (lungeTimer <= 0f)
        {
            lungeTimer = t.FightLungeInterval;
            Vector3 to = partner.transform.position - ctx.Body.position; to.y = 0f;
            Vector3 dir = to.sqrMagnitude > 0.0001f ? to.normalized : ctx.Body.forward;
            ctx.SetStopped(false);
            ctx.SetDestinationSafe(partner.transform.position - dir * t.FightStopDistance);
        }

        // No dedicated counter field for "every 2 lunges" — reuses emoteTimer at twice the lunge
        // interval, which lands the Molesto beat roughly every other abalanzada.
        emoteTimer -= Time.deltaTime;
        if (emoteTimer <= 0f)
        {
            emoteTimer = t.FightLungeInterval * 2f;
            owner.EmitEmote(EmoteKind.Molesto);
        }

        if (timer >= duration) End(true);
    }

    private void FacePartner()
    {
        if (partner == null) return;
        Vector3 dir = partner.transform.position - ctx.Body.position; dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        ctx.Body.rotation = Quaternion.Slerp(
            ctx.Body.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), 10f * Time.deltaTime);
    }

    // notifyPartner: a natural chase completion also completes the partner's side in the same
    // frame, so both collect the Affect boost — otherwise whoever ticks second sees a vanished
    // partner and closes through the interruption path, losing the reward to a frame race. The
    // same race applies to the SocialGraph write, so history is recorded ONLY on the notifying
    // (primary) side — the partner's own End() runs with notifyPartner=false and skips it.
    private void End(bool completed, bool notifyPartner = true)
    {
        var t            = SocialTuningSO.Current;
        var endedPartner = partner;
        var endedMode    = mode;
        bool wasChase    = endedMode == SocialMode.Chaser || endedMode == SocialMode.Runner;
        bool wasPaired   = wasChase || endedMode == SocialMode.Sleeping || endedMode == SocialMode.Fighting;

        if (completed && endedMode == SocialMode.Approach)
        {
            owner.EmitEmote(EmoteKind.Feliz);
        }
        else if (completed && wasChase)
        {
            if (t != null) ctx.Dna?.Needs.AddAffect(t.SocialAffectBoost);
            owner.EmitEmote(EmoteKind.Feliz);
        }
        else if (completed && endedMode == SocialMode.Sleeping)
        {
            if (t != null) ctx.Dna?.Needs.AddAffect(t.SleepAffectBoost);
            owner.EmitEmote(EmoteKind.Feliz);
        }
        else if (completed && endedMode == SocialMode.Fighting)
        {
            if (t != null) ctx.Dna?.Needs.AddAffect(-t.FightAffectLoss);
            owner.EmitEmote(EmoteKind.Molesto);
        }

        if (sleepStation != null) { sleepStation.Release(owner); sleepStation = null; }

        cooldownUntil = Time.time + (t != null ? t.ScaledSocialCooldown(ctx.Dna != null ? ctx.Dna.Sociability : 0.5f) : 20f);
        partner       = null;
        mode          = SocialMode.None;
        swapped       = false;

        if (ctx.State == AgentState.Socializing) owner.RequestRoam();

        // Each side lands its own knock on its own End() call, never on the partner — a mutual
        // scuffle, both back off with a little shove.
        if (completed && endedMode == SocialMode.Fighting && t != null)
        {
            Vector3 dir = endedPartner != null ? ctx.Body.position - endedPartner.transform.position : Vector3.zero;
            dir.y = 0f;
            dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : -ctx.Body.forward;
            owner.RequestPlayfulKnock((dir + Vector3.up * 0.5f).normalized * t.FightKnockForce);
        }

        if (completed && notifyPartner && wasPaired && endedPartner != null &&
            ctx.Dna != null && endedPartner.DNA != null)
        {
            var kind = endedMode == SocialMode.Sleeping ? SocialInteractionKind.SleepTogether
                     : endedMode == SocialMode.Fighting  ? SocialInteractionKind.GremlinFight
                     :                                     SocialInteractionKind.PlayChase;
            SocialGraphService.RecordInteraction(ctx.Dna.UniqueID, endedPartner.DNA.UniqueID, kind);
        }

        if (completed && notifyPartner && wasPaired && endedPartner != null && endedPartner.IsSocializing)
            endedPartner.CompleteSocialPlayFromPartner();
    }

    internal void CompleteFromPartner()
    {
        if (mode != SocialMode.Chaser && mode != SocialMode.Runner &&
            mode != SocialMode.Sleeping && mode != SocialMode.Fighting) return;
        End(true, false);
    }

    internal void ResetForReuse()
    {
        if (sleepStation != null) { sleepStation.Release(owner); sleepStation = null; }
        partner       = null;
        mode          = SocialMode.None;
        swapped       = false;
        lungeTimer    = 0f;
        emoteTimer    = 0f;
        timer         = 0f;
        cooldownUntil = 0f;
    }

    // Cheap repulsion filter for the brain's next roam point — pushes it away from any nearby
    // Monchi this creature's Avoid rules dislike. A single pass, no recursion: if the nudged
    // point is still uncomfortably close to something else, the next roam tick nudges again.
    internal Vector3 AdjustRoamForAvoidance(Vector3 candidate)
    {
        var t = SocialTuningSO.Current;
        if (t == null) return candidate;

        var rules = ctx.Profile?.Reactions;
        if (rules == null) return candidate;

        for (int i = 0; i < ctx.Percepts.Count; i++)
        {
            var p = ctx.Percepts[i];
            for (int j = 0; j < rules.Count; j++)
            {
                var rule = rules[j];
                if (rule == null || rule.Action != SocialAction.Avoid) continue;
                if (!rule.Matches(p, owner, t, out _)) continue;

                Vector3 source = p.Source != null ? p.Source.Position : candidate;
                Vector3 delta  = candidate - source; delta.y = 0f;
                if (delta.sqrMagnitude < t.AvoidClearance * t.AvoidClearance)
                {
                    Vector3 push = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector3.forward;
                    candidate += push * t.AvoidClearance;
                }
                break;
            }
        }
        return candidate;
    }

    internal CreatureIntent Intent => mode switch
    {
        SocialMode.Chaser   => CreatureIntent.Chasing,
        SocialMode.Runner   => CreatureIntent.Chasing,
        SocialMode.Approach => CreatureIntent.Socializing,
        SocialMode.Sleeping => CreatureIntent.SleepingTogether,
        SocialMode.Fighting => CreatureIntent.Fighting,
        _                   => CreatureIntent.Wandering,
    };

    internal string Describe() =>
        mode == SocialMode.None ? "—"
        : $"{mode} ↔ {partner?.DNA?.CustomName ?? "?"} ({timer:0.0}/{duration:0.0}s)";
}
}
