using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

internal class AgentExpedition
{
    private enum Phase { Noticing, Moving, Mining, Losing, Returning, Securing, Guarding, Hunting, Decoying, Exploring }
    private enum DecoyStep { Approach, Taunt, Flee }

    private readonly MoriMochiAgent owner;
    private readonly AgentContext   ctx;
    private readonly AgentScout     scout;

    private MaterialPickup target;
    private float          repathTimer;
    private float          elapsed;
    private int            collected;
    private int            secured;
    private Phase          phase;
    private float          phaseTimer;
    private float          blockedTimer;
    private Vector3        lostPoint;

    private int            carried;
    private float          miningTimer;
    private ExitZone       exit;
    private MoriMochiAgent prey;
    private float          huntTimer;
    private float          decoyCooldownUntil;
    private DecoyStep      decoyStep;

    internal AgentExpedition(MoriMochiAgent owner, AgentContext ctx)
    {
        this.owner = owner;
        this.ctx   = ctx;
        scout      = new AgentScout(owner, ctx);
    }

    internal bool TryEngage()
    {
        var rules = ExpeditionRulesSO.Current;
        if (rules == null || ctx.Dna == null) return false;

        var occ = ctx.Occupation;
        if (occ == Occupation.None) occ = Occupation.Gather;

        switch (occ)
        {
            case Occupation.Guard:   return TryGuardEngage(rules);
            case Occupation.Break:   return TryBreakEngage(rules);
            case Occupation.Decoy:   return TryDecoyEngage(rules);
            case Occupation.Explore: return TryExploreEngage(rules);
            default:                 return TryGatherEngage(rules);
        }
    }

    private bool TryExploreEngage(ExpeditionRulesSO rules)
    {
        if (!scout.TryEngage(rules)) return TryGatherEngage(rules);

        target  = null;
        prey    = null;
        phase   = Phase.Exploring;
        elapsed = 0f;
        return true;
    }

    private bool TryGatherEngage(ExpeditionRulesSO rules)
    {
        if (carried >= rules.CarryCapacity) return BeginReturn(rules);

        float bestScore = float.NegativeInfinity;
        Percept bestPercept = default;
        ExpeditionRuleBase bestRule = null;

        for (int i = 0; i < ctx.Percepts.Count; i++)
        {
            var p = ctx.Percepts[i];
            var mat = p.Source != null ? p.Source.GetComponent<MaterialPickup>() : null;
            if (mat == null || mat.Taken || !mat.gameObject.activeInHierarchy) continue;

            for (int j = 0; j < rules.Rules.Count; j++)
            {
                var rule = rules.Rules[j];
                if (rule == null) continue;
                if (!rule.Matches(p, owner, rules, out float score)) continue;
                if (score <= bestScore) continue;

                bestScore   = score;
                bestPercept = p;
                bestRule    = rule;
            }
        }

        if (bestRule != null)
        {
            target = bestPercept.Source.GetComponent<MaterialPickup>();

            ctx.State   = AgentState.Expedition;
            elapsed     = 0f;
            repathTimer = 0f;
            phase       = Phase.Noticing;
            phaseTimer  = rules.NoticeSeconds;
            ctx.SetStopped(true);
            owner.EmitEmote(EmoteKind.Curioso);

            if (rules.NoticeSeconds <= 0f)
            {
                phase = Phase.Moving;
                ctx.SetStopped(false);
                ctx.SetDestinationSafe(ApproachPoint(rules));
            }

            return true;
        }

        if (carried > 0) return BeginReturn(rules);

        var known = InjectedPost();
        if (known == null && ctx.Board != null) known = ctx.Board.BestKnownVein(ctx.Body.position, null);
        if (known == null) return false;

        target      = known;
        ctx.State   = AgentState.Expedition;
        elapsed     = 0f;
        repathTimer = rules.RepathInterval;
        phase       = Phase.Moving;
        ctx.SetStopped(false);
        ctx.SetDestinationSafe(ApproachPoint(rules));
        return true;
    }

    private bool TryGuardEngage(ExpeditionRulesSO rules)
    {
        MaterialPickup best = InjectedPost();
        if (best == null) best = FindPost();
        if (best == null) return false;

        ctx.State   = AgentState.Expedition;
        target      = best;
        phase       = Phase.Guarding;
        elapsed     = 0f;
        repathTimer = 0f;
        ctx.SetStopped(false);
        ctx.SetDestinationSafe(GuardPoint(rules));
        return true;
    }

    private bool TryBreakEngage(ExpeditionRulesSO rules)
    {
        var best = FindPrey();
        if (best != null)
        {
            ctx.State = AgentState.Expedition;
            target    = null;
            prey      = best;
            phase     = Phase.Hunting;
            huntTimer = 0f;
            elapsed   = 0f;
            ctx.SetStopped(false);
            ctx.SetDestinationSafe(prey.transform.position);
            return true;
        }

        MaterialPickup post = InjectedPost();
        if (post == null) post = FindPost();
        if (post == null) return false;

        ctx.State   = AgentState.Expedition;
        target      = post;
        prey        = null;
        phase       = Phase.Hunting;
        huntTimer   = 0f;
        elapsed     = 0f;
        ctx.SetStopped(false);
        ctx.SetDestinationSafe(GuardPoint(rules));
        return true;
    }

    private bool TryDecoyEngage(ExpeditionRulesSO rules)
    {
        if (Time.time < decoyCooldownUntil) return false;

        var found = FindDecoyTarget();
        if (found != null)
        {
            ctx.State = AgentState.Expedition;
            target    = null;
            prey      = found;
            phase     = Phase.Decoying;
            decoyStep = DecoyStep.Approach;
            huntTimer = 0f;
            elapsed   = 0f;
            ctx.SetStopped(false);
            ctx.SetDestinationSafe(prey.transform.position);
            return true;
        }

        MaterialPickup post = InjectedPost();
        if (post == null) post = FindPost();
        if (post == null) return false;

        ctx.State   = AgentState.Expedition;
        target      = post;
        prey        = null;
        phase       = Phase.Decoying;
        decoyStep   = DecoyStep.Approach;
        huntTimer   = 0f;
        elapsed     = 0f;
        ctx.SetStopped(false);
        ctx.SetDestinationSafe(GuardPoint(rules));
        return true;
    }

    private MaterialPickup InjectedPost()
    {
        if (ctx.GuardPost == null) return null;
        var post = ctx.GuardPost.GetComponent<MaterialPickup>();
        if (post == null || post.Taken || !post.gameObject.activeInHierarchy) return null;
        return post;
    }

    private MaterialPickup FindPost()
    {
        MaterialPickup best = null;
        int   bestRemaining = -1;
        float bestSqrDist   = float.PositiveInfinity;

        for (int i = 0; i < ctx.Percepts.Count; i++)
        {
            var p = ctx.Percepts[i];
            if (p.Kind != PerceivableKind.Material) continue;
            var mat = p.Source != null ? p.Source.GetComponent<MaterialPickup>() : null;
            if (mat == null || mat.Taken || !mat.gameObject.activeInHierarchy) continue;

            if (mat.Remaining > bestRemaining || (mat.Remaining == bestRemaining && p.SqrDistance < bestSqrDist))
            {
                best          = mat;
                bestRemaining = mat.Remaining;
                bestSqrDist   = p.SqrDistance;
            }
        }

        return best;
    }

    private MoriMochiAgent FindPrey()
    {
        MoriMochiAgent best = null;
        float bestSqrDist = float.PositiveInfinity;

        for (int i = 0; i < ctx.Percepts.Count; i++)
        {
            var p = ctx.Percepts[i];
            if (p.Kind != PerceivableKind.Monchi) continue;
            if (!ExpeditionTeams.AreRivals(owner.Team, p.Team)) continue;

            var rival = p.Source != null ? p.Source.Monchi : null;
            if (rival == null || rival.IsAirborne || rival.IsHeld || rival.IsRecovering) continue;

            var intent = rival.Intent;
            if (intent != CreatureIntent.Taking && intent != CreatureIntent.Carrying &&
                intent != CreatureIntent.Securing && intent != CreatureIntent.Collecting) continue;

            if (p.SqrDistance >= bestSqrDist) continue;

            best        = rival;
            bestSqrDist = p.SqrDistance;
        }

        return best;
    }

    private MoriMochiAgent FindDecoyTarget()
    {
        MoriMochiAgent bestPrio = null;
        float bestPrioSqrDist = float.PositiveInfinity;
        MoriMochiAgent bestAny = null;
        float bestAnySqrDist = float.PositiveInfinity;

        for (int i = 0; i < ctx.Percepts.Count; i++)
        {
            var p = ctx.Percepts[i];
            if (p.Kind != PerceivableKind.Monchi) continue;
            if (!ExpeditionTeams.AreRivals(owner.Team, p.Team)) continue;

            var rival = p.Source != null ? p.Source.Monchi : null;
            if (rival == null || rival.IsAirborne || rival.IsHeld || rival.IsRecovering) continue;

            if (p.SqrDistance < bestAnySqrDist)
            {
                bestAny        = rival;
                bestAnySqrDist = p.SqrDistance;
            }

            if ((rival.Occupation == Occupation.Guard || rival.Occupation == Occupation.Break) &&
                p.SqrDistance < bestPrioSqrDist)
            {
                bestPrio        = rival;
                bestPrioSqrDist = p.SqrDistance;
            }
        }

        return bestPrio != null ? bestPrio : bestAny;
    }

    internal void TickExpedition()
    {
        var rules = ExpeditionRulesSO.Current;
        if (rules == null) { Abort(); return; }

        if (phase == Phase.Exploring)
        {
            if (!scout.Tick(rules)) Abort();
            return;
        }

        bool validatesTarget = phase == Phase.Noticing || phase == Phase.Moving || phase == Phase.Mining;
        if (validatesTarget && (target == null || target.Taken || !target.gameObject.activeInHierarchy))
        {
            EnterLosing(rules);
            return;
        }

        float dt = Time.deltaTime;
        elapsed += dt;
        bool giveUpPhase = phase == Phase.Noticing || phase == Phase.Moving || (phase == Phase.Hunting && prey != null);
        if (giveUpPhase && elapsed > rules.GiveUpSeconds) { Abort(); return; }

        switch (phase)
        {
            case Phase.Noticing:
                phaseTimer -= dt;
                if (phaseTimer <= 0f)
                {
                    phase = Phase.Moving;
                    ctx.SetStopped(false);
                    ctx.SetDestinationSafe(ApproachPoint(rules));
                    repathTimer = rules.RepathInterval;
                }
                break;

            case Phase.Moving:
                repathTimer -= dt;
                if (repathTimer <= 0f)
                {
                    repathTimer = rules.RepathInterval;
                    ctx.SetDestinationSafe(ApproachPoint(rules));
                }

                Vector3 approach = ApproachPoint(rules);
                Vector3 delta    = approach - ctx.Body.position; delta.y = 0f;
                bool    arrived  = delta.magnitude <= rules.ArriveDistance;

                Vector3 toCenter = target.transform.position - ctx.Body.position; toCenter.y = 0f;
                float   rim      = target.Radius + ctx.Agent.radius + rules.ApproachMargin;
                if (ctx.Agent.velocity.magnitude < 0.05f &&
                    toCenter.magnitude <= rim + rules.ArriveDistance + ctx.Agent.radius * 2f)
                {
                    blockedTimer += dt;
                    if (blockedTimer > 0.6f) arrived = true;
                }
                else blockedTimer = 0f;

                if (arrived)
                {
                    blockedTimer = 0f;
                    phase        = Phase.Mining;
                    miningTimer  = rules.MiningSecondsPerUnit;
                    ctx.SetStopped(true);
                }
                break;

            case Phase.Mining:
                Vector3 dir = target.transform.position - ctx.Body.position; dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    ctx.Body.rotation = Quaternion.Slerp(
                        ctx.Body.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), 10f * dt);

                miningTimer -= dt;
                if (miningTimer <= 0f)
                {
                    if (target.TryMineUnit())
                    {
                        carried++;
                        collected++;
                        owner.onPickup?.Invoke();
                    }

                    if (target.Taken || carried >= rules.CarryCapacity)
                    {
                        if (carried > 0 && ctx.HomeExit != null) BeginReturn(rules);
                        else Abort();
                    }
                    else
                    {
                        miningTimer = rules.MiningSecondsPerUnit;
                    }
                }
                break;

            case Phase.Losing:
                Vector3 dirBack = lostPoint - ctx.Body.position; dirBack.y = 0f;
                if (dirBack.sqrMagnitude > 0.001f)
                    ctx.Body.rotation = Quaternion.Slerp(
                        ctx.Body.rotation, Quaternion.LookRotation(dirBack.normalized, Vector3.up), 10f * dt);

                phaseTimer -= dt;
                if (phaseTimer <= 0f) Abort();
                break;

            case Phase.Returning:
                if (exit == null) { Abort(); return; }

                repathTimer -= dt;
                if (repathTimer <= 0f)
                {
                    repathTimer = rules.RepathInterval;
                    ctx.SetDestinationSafe(exit.transform.position);
                }

                Vector3 toExit      = exit.transform.position - ctx.Body.position; toExit.y = 0f;
                bool    arrivedExit = exit.Contains(ctx.Body.position) || toExit.magnitude <= exit.Radius;

                if (!arrivedExit && ctx.Agent.velocity.magnitude < 0.05f && toExit.magnitude <= exit.Radius + 1.5f)
                {
                    blockedTimer += dt;
                    if (blockedTimer > 1.5f) arrivedExit = true;
                }
                else blockedTimer = 0f;

                if (arrivedExit)
                {
                    blockedTimer = 0f;
                    phase        = Phase.Securing;
                    phaseTimer   = rules.DepositSeconds;
                    ctx.SetStopped(true);
                    if (rules.DepositSeconds <= 0f) Secure();
                }
                break;

            case Phase.Securing:
                Vector3 faceExit = exit.transform.position - ctx.Body.position; faceExit.y = 0f;
                if (faceExit.sqrMagnitude > 0.001f)
                    ctx.Body.rotation = Quaternion.Slerp(
                        ctx.Body.rotation, Quaternion.LookRotation(faceExit.normalized, Vector3.up), 10f * dt);

                phaseTimer -= dt;
                if (phaseTimer <= 0f) Secure();
                break;

            case Phase.Guarding:
                if (target == null || target.Taken || !target.gameObject.activeInHierarchy) { Abort(); return; }

                Vector3 toPost = target.transform.position - ctx.Body.position; toPost.y = 0f;
                float   dPost  = toPost.magnitude;

                if (dPost > rules.GuardRadius)
                {
                    ctx.SetStopped(false);
                    repathTimer -= dt;
                    if (repathTimer <= 0f)
                    {
                        repathTimer = rules.RepathInterval;
                        ctx.SetDestinationSafe(GuardPoint(rules));
                    }
                }
                else
                {
                    ctx.SetStopped(true);

                    MoriMochiAgent nearestRival = null;
                    float bestSqrDist = float.PositiveInfinity;
                    for (int i = 0; i < ctx.Percepts.Count; i++)
                    {
                        var p = ctx.Percepts[i];
                        if (p.Kind != PerceivableKind.Monchi) continue;
                        if (!ExpeditionTeams.AreRivals(owner.Team, p.Team)) continue;
                        if (p.SqrDistance >= bestSqrDist) continue;

                        nearestRival = p.Source != null ? p.Source.Monchi : null;
                        bestSqrDist  = p.SqrDistance;
                    }

                    Vector3 faceDir = (nearestRival != null ? nearestRival.transform.position : target.transform.position) - ctx.Body.position;
                    faceDir.y = 0f;
                    if (faceDir.sqrMagnitude > 0.001f)
                        ctx.Body.rotation = Quaternion.Slerp(
                            ctx.Body.rotation, Quaternion.LookRotation(faceDir.normalized, Vector3.up), 10f * dt);
                }
                break;

            case Phase.Hunting:
                if (prey != null)
                {
                    bool abandonPrey = prey.IsAirborne || prey.IsHeld || prey.IsRecovering;
                    if (!abandonPrey)
                    {
                        var preyIntent = prey.Intent;
                        abandonPrey = preyIntent != CreatureIntent.Taking && preyIntent != CreatureIntent.Carrying &&
                                      preyIntent != CreatureIntent.Securing && preyIntent != CreatureIntent.Collecting;
                    }

                    if (abandonPrey)
                    {
                        prey = null;
                        if (target == null || target.Taken) { Abort(); return; }
                        break;
                    }

                    huntTimer -= dt;
                    if (huntTimer <= 0f)
                    {
                        huntTimer = rules.HuntRepathInterval;
                        ctx.SetDestinationSafe(prey.transform.position);
                    }
                }
                else
                {
                    if (target == null || target.Taken || !target.gameObject.activeInHierarchy) { Abort(); return; }

                    Vector3 toHuntPost = target.transform.position - ctx.Body.position; toHuntPost.y = 0f;
                    float   dHuntPost  = toHuntPost.magnitude;

                    if (dHuntPost > rules.GuardRadius)
                    {
                        ctx.SetStopped(false);
                        repathTimer -= dt;
                        if (repathTimer <= 0f)
                        {
                            repathTimer = rules.RepathInterval;
                            ctx.SetDestinationSafe(GuardPoint(rules));
                        }
                    }
                    else
                    {
                        ctx.SetStopped(true);

                        MoriMochiAgent nearestRival = null;
                        float bestSqrDist = float.PositiveInfinity;
                        for (int i = 0; i < ctx.Percepts.Count; i++)
                        {
                            var p = ctx.Percepts[i];
                            if (p.Kind != PerceivableKind.Monchi) continue;
                            if (!ExpeditionTeams.AreRivals(owner.Team, p.Team)) continue;
                            if (p.SqrDistance >= bestSqrDist) continue;

                            nearestRival = p.Source != null ? p.Source.Monchi : null;
                            bestSqrDist  = p.SqrDistance;
                        }

                        Vector3 faceHuntDir = (nearestRival != null ? nearestRival.transform.position : target.transform.position) - ctx.Body.position;
                        faceHuntDir.y = 0f;
                        if (faceHuntDir.sqrMagnitude > 0.001f)
                            ctx.Body.rotation = Quaternion.Slerp(
                                ctx.Body.rotation, Quaternion.LookRotation(faceHuntDir.normalized, Vector3.up), 10f * dt);
                    }

                    huntTimer -= dt;
                    if (huntTimer <= 0f)
                    {
                        huntTimer = rules.HuntRepathInterval;
                        var found = FindPrey();
                        if (found != null)
                        {
                            prey = found;
                            ctx.SetStopped(false);
                            ctx.SetDestinationSafe(prey.transform.position);
                        }
                    }
                }
                break;

            case Phase.Decoying:
                switch (decoyStep)
                {
                    case DecoyStep.Approach:
                        if (prey != null)
                        {
                            bool abandonPrey = prey.IsAirborne || prey.IsHeld || prey.IsRecovering;
                            if (abandonPrey)
                            {
                                prey = null;
                                break;
                            }

                            if (elapsed > rules.GiveUpSeconds) { EndDecoy(rules); return; }

                            huntTimer -= dt;
                            if (huntTimer <= 0f)
                            {
                                huntTimer = rules.HuntRepathInterval;
                                ctx.SetDestinationSafe(prey.transform.position);
                            }

                            Vector3 toPrey = prey.transform.position - ctx.Body.position; toPrey.y = 0f;
                            if (toPrey.magnitude <= rules.DecoyRange)
                            {
                                decoyStep  = DecoyStep.Taunt;
                                phaseTimer = rules.TauntSeconds;
                                ctx.SetStopped(true);
                                owner.EmitEmote(EmoteKind.Molesto);
                            }
                        }
                        else
                        {
                            if (target == null || target.Taken || !target.gameObject.activeInHierarchy) { Abort(); return; }

                            Vector3 toDecoyPost = target.transform.position - ctx.Body.position; toDecoyPost.y = 0f;
                            float   dDecoyPost  = toDecoyPost.magnitude;

                            if (dDecoyPost > rules.GuardRadius)
                            {
                                ctx.SetStopped(false);
                                repathTimer -= dt;
                                if (repathTimer <= 0f)
                                {
                                    repathTimer = rules.RepathInterval;
                                    ctx.SetDestinationSafe(GuardPoint(rules));
                                }
                            }
                            else
                            {
                                ctx.SetStopped(true);
                            }

                            huntTimer -= dt;
                            if (huntTimer <= 0f)
                            {
                                huntTimer = rules.HuntRepathInterval;
                                var found = FindDecoyTarget();
                                if (found != null)
                                {
                                    prey = found;
                                    ctx.SetStopped(false);
                                    ctx.SetDestinationSafe(prey.transform.position);
                                }
                            }
                        }
                        break;

                    case DecoyStep.Taunt:
                        Vector3 faceDir = prey.transform.position - ctx.Body.position; faceDir.y = 0f;
                        if (faceDir.sqrMagnitude > 0.001f)
                            ctx.Body.rotation = Quaternion.Slerp(
                                ctx.Body.rotation, Quaternion.LookRotation(faceDir.normalized, Vector3.up), 10f * dt);

                        phaseTimer -= dt;
                        if (phaseTimer <= 0f)
                        {
                            decoyStep  = DecoyStep.Flee;
                            phaseTimer = rules.DecoyFleeSeconds;

                            Vector3 away = ctx.Body.position - prey.transform.position; away.y = 0f;
                            away = away.sqrMagnitude > 0.0001f ? away.normalized : ctx.Body.forward;

                            if (ctx.HomeExit != null)
                            {
                                Vector3 dirAHome = ctx.HomeExit.transform.position - ctx.Body.position; dirAHome.y = 0f;
                                if (dirAHome.sqrMagnitude > 0.0001f) away = (away + dirAHome.normalized).normalized;
                            }

                            ctx.SetStopped(false);
                            ctx.SetDestinationSafe(ctx.Body.position + away * rules.DecoyFleeDistance);
                        }
                        break;

                    case DecoyStep.Flee:
                        phaseTimer -= dt;
                        if (phaseTimer <= 0f) EndDecoy(rules);
                        break;
                }
                break;
        }
    }

    private Vector3 ApproachPoint(ExpeditionRulesSO rules)
    {
        Vector3 center = target.transform.position;
        float   rim    = target.Radius + ctx.Agent.radius + rules.ApproachMargin;

        Vector3 toSelf = ctx.Body.position - center; toSelf.y = 0f;
        float   a = toSelf.sqrMagnitude > 0.0001f
            ? Mathf.Atan2(toSelf.z, toSelf.x)
            : Mathf.Atan2(ctx.Body.forward.z, ctx.Body.forward.x);

        float sep         = 2f * Mathf.Asin(Mathf.Clamp01((ctx.Agent.radius + 0.1f) / rim));
        float selfSqrDist = toSelf.sqrMagnitude;

        for (int pass = 0; pass < 2; pass++)
        {
            for (int i = 0; i < ctx.Percepts.Count; i++)
            {
                var p = ctx.Percepts[i];
                if (p.Source == null || p.Source.Monchi == null || p.Source.Monchi == owner) continue;
                if (p.Source.Monchi.ExpeditionTarget != target.transform) continue;
                var otherIntent = p.Source.Monchi.Intent;
                if (otherIntent != CreatureIntent.Collecting && otherIntent != CreatureIntent.Taking) continue;

                Vector3 other = p.Source.Monchi.transform.position - center; other.y = 0f;
                if (other.sqrMagnitude >= selfSqrDist) continue;

                float b     = Mathf.Atan2(other.z, other.x);
                float delta = Mathf.DeltaAngle(a * Mathf.Rad2Deg, b * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                if (Mathf.Abs(delta) < sep)
                    a = b + Mathf.Sign(delta != 0f ? delta : 1f) * sep;
            }
        }

        Vector3 point = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * rim;
        point.y = center.y;
        return point;
    }

    private Vector3 GuardPoint(ExpeditionRulesSO rules)
    {
        Vector3 post = target.transform.position;
        Vector3 dir  = ctx.HomeExit != null
            ? ctx.HomeExit.transform.position - post
            : ctx.Body.position - post;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = ctx.Body.forward;
        dir.Normalize();

        Vector3 point = post + dir * (rules.GuardRadius * 0.6f);
        point.y = post.y;
        return point;
    }

    private bool BeginReturn(ExpeditionRulesSO rules)
    {
        exit = ctx.HomeExit;
        if (exit == null)
        {
            carried = 0;
            owner.EmitEmote(EmoteKind.Feliz);
            return false;
        }

        ctx.State    = AgentState.Expedition;
        target       = null;
        phase        = Phase.Returning;
        elapsed      = 0f;
        repathTimer  = 0f;
        blockedTimer = 0f;
        ctx.SetStopped(false);
        ctx.SetDestinationSafe(exit.transform.position);
        return true;
    }

    private void Secure()
    {
        exit.Deposit(carried);
        secured += carried;
        carried = 0;
        owner.EmitEmote(EmoteKind.Feliz);
        Abort();
    }

    private void EnterLosing(ExpeditionRulesSO rules)
    {
        lostPoint  = target != null ? target.transform.position : ctx.Body.position + ctx.Body.forward;
        target     = null;
        phase      = Phase.Losing;
        phaseTimer = rules.LoseSeconds;
        ctx.SetStopped(true);
        owner.EmitEmote(EmoteKind.Molesto);
        if (rules.LoseSeconds <= 0f) Abort();
    }

    private void EndDecoy(ExpeditionRulesSO rules)
    {
        decoyCooldownUntil = Time.time + rules.DecoyCooldown;
        Abort();
    }

    internal void OnKnocked()
    {
        var rules = ExpeditionRulesSO.Current;
        if (carried > 0)
        {
            if (rules != null && rules.DropPrefab != null) Drop(rules);
            carried = 0;
        }

        Cancel();
    }

    internal void Cancel()
    {
        target       = null;
        prey         = null;
        exit         = null;
        phase        = Phase.Noticing;
        phaseTimer   = 0f;
        miningTimer  = 0f;
        blockedTimer = 0f;
        lostPoint    = Vector3.zero;
        decoyStep    = DecoyStep.Approach;
        scout.Cancel();
    }

    private void Drop(ExpeditionRulesSO rules)
    {
        Vector3 pos = ctx.Body.position;
        if (NavMesh.SamplePosition(pos, out var hit, 2f, NavMesh.AllAreas)) pos = hit.position;

        var drop = Object.Instantiate(rules.DropPrefab, pos, Quaternion.identity);
        drop.transform.localScale = Vector3.one * rules.DropScale;
        drop.SetValue(carried);
    }

    private void Abort()
    {
        target       = null;
        phase        = Phase.Noticing;
        phaseTimer   = 0f;
        blockedTimer = 0f;
        lostPoint    = Vector3.zero;
        prey         = null;
        exit         = null;
        miningTimer  = 0f;
        scout.Cancel();
        owner.RequestRoam();
    }

    internal void ResetForReuse()
    {
        target       = null;
        elapsed      = 0f;
        repathTimer  = 0f;
        phase        = Phase.Noticing;
        phaseTimer   = 0f;
        blockedTimer = 0f;
        lostPoint    = Vector3.zero;
        carried      = 0;
        collected    = 0;
        secured      = 0;
        exit         = null;
        prey         = null;
        miningTimer  = 0f;
        huntTimer    = 0f;
        decoyStep    = DecoyStep.Approach;
        decoyCooldownUntil = 0f;
        scout.ResetForReuse();
    }

    internal int Carried => carried;
    internal int Reports => scout.Reports;

    internal float MiningProgress =>
        phase == Phase.Mining && ExpeditionRulesSO.Current != null && ExpeditionRulesSO.Current.MiningSecondsPerUnit > 0f
            ? 1f - miningTimer / ExpeditionRulesSO.Current.MiningSecondsPerUnit
            : 0f;

    internal Transform TargetTransform =>
        phase == Phase.Exploring ? scout.TargetTransform :
        (phase == Phase.Noticing || phase == Phase.Moving || phase == Phase.Mining || phase == Phase.Guarding)
            ? (target != null ? target.transform : null) :
        (phase == Phase.Returning || phase == Phase.Securing)
            ? (exit != null ? exit.transform : null) :
        (phase == Phase.Hunting || phase == Phase.Decoying)
            ? (prey != null ? prey.transform : (target != null ? target.transform : null))
            : null;

    internal MaterialPickup Target => target;
    internal int             Collected => collected;
    internal int             Secured   => secured;
    internal CreatureIntent  Intent    =>
        phase == Phase.Exploring ? scout.Intent :
        phase == Phase.Mining    ? CreatureIntent.Taking :
        phase == Phase.Losing    ? CreatureIntent.Losing :
        phase == Phase.Returning ? CreatureIntent.Carrying :
        phase == Phase.Securing  ? CreatureIntent.Securing :
        phase == Phase.Guarding  ? CreatureIntent.Guarding :
        phase == Phase.Hunting   ? CreatureIntent.Hunting :
        phase == Phase.Decoying  ? (decoyStep == DecoyStep.Flee ? CreatureIntent.Fleeing : CreatureIntent.Taunting) :
        CreatureIntent.Collecting;
}
}
