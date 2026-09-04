using UnityEngine;
namespace MoriMonchiSimulator
{

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
            if (ExpeditionTeams.AreRivals(owner.Team, p.Team)) continue;
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

    private static MoriMochiAgent TargetOf(in Percept p) => p.Source != null ? p.Source.Monchi : null;

    private void Enter(SocialMode newMode, MoriMochiAgent newPartner, float newDuration, EmoteKind emote)
    {
        partner     = newPartner;
        mode        = newMode;
        timer       = 0f;
        duration    = newDuration;
        repathTimer = 0f;
        swapped     = false;
        lungeTimer  = 0f;
        emoteTimer  = 0f;

        ctx.State                = AgentState.Socializing;
        ctx.SetStopped(false);
        ctx.Agent.updateRotation = true;
        owner.EmitEmote(emote);
    }

    private bool BeginApproach(in Percept p, SocialTuningSO t)
    {
        var target = TargetOf(p);
        if (target == null) return false;

        Enter(SocialMode.Approach, target, t.ApproachDuration, EmoteKind.Curioso);
        return true;
    }

    private bool BeginPlayChase(in Percept p, SocialTuningSO t)
    {
        var target = TargetOf(p);
        if (target == null) return false;
        if (!target.TryJoinSocialPlay(owner)) return false;

        Enter(SocialMode.Chaser, target, t.ChaseDuration, EmoteKind.Jugando);
        return true;
    }

    private bool CanPair(SocialTuningSO t, MoriMochiAgent initiator)
    {
        if (t == null) return false;
        if (initiator == null) return false;
        if (ExpeditionTeams.AreRivals(owner.Team, initiator.Team)) return false;
        if (Time.time < cooldownUntil) return false;
        if (ctx.State != AgentState.Idle && ctx.State != AgentState.Roaming) return false;
        if (ctx.CurrentContainer != null) return false;
        if (ctx.IsBreeding) return false;
        if (ctx.Dna == null) return false;
        return true;
    }

    internal bool TryJoinSocialPlay(MoriMochiAgent initiator)
    {
        var t = SocialTuningSO.Current;
        if (!CanPair(t, initiator)) return false;
        if (ctx.Dna.Needs.Energy < t.MinEnergyToPlay) return false;
        if (owner.Condition != CreatureCondition.Healthy) return false;

        owner.RequestReleaseStation();
        Enter(SocialMode.Runner, initiator, t.ChaseDuration, EmoteKind.Jugando);
        return true;
    }

    private bool BeginSleep(in Percept p, SocialTuningSO t)
    {
        var target = TargetOf(p);
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

        Enter(SocialMode.Sleeping, target, t.SleepDuration, EmoteKind.Zzz);
        return true;
    }

    internal bool TryJoinSleep(MoriMochiAgent initiator, NeedStation station, Vector3 fallbackSpot)
    {
        var t = SocialTuningSO.Current;
        if (!CanPair(t, initiator)) return false;
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

        Enter(SocialMode.Sleeping, initiator, t.SleepDuration, EmoteKind.Zzz);
        return true;
    }

    private bool BeginFight(in Percept p, SocialTuningSO t)
    {
        var target = TargetOf(p);
        if (target == null) return false;
        if (!target.TryJoinSocialFight(owner)) return false;

        Enter(SocialMode.Fighting, target, t.FightDuration, EmoteKind.Molesto);
        return true;
    }

    internal bool TryJoinFight(MoriMochiAgent initiator)
    {
        var t = SocialTuningSO.Current;
        if (!CanPair(t, initiator)) return false;
        if (ctx.Dna.Needs.Energy < t.MinEnergyToPlay) return false;
        if (owner.Condition != CreatureCondition.Healthy) return false;

        owner.RequestReleaseStation();
        Enter(SocialMode.Fighting, initiator, t.FightDuration, EmoteKind.Molesto);
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

    internal MoriMochiAgent Partner => partner;

    internal string Describe() =>
        mode == SocialMode.None ? "—"
        : $"{mode} ↔ {partner?.DNA?.CustomName ?? "?"} ({timer:0.0}/{duration:0.0}s)";
}
}
