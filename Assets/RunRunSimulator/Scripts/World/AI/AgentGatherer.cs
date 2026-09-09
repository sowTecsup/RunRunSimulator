using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

internal class AgentGatherer : IExpeditionTask
{
    private enum Phase { Noticing, Moving, Mining, Losing, Returning, Securing, Fleeing }

    private readonly MoriMochiAgent owner;
    private readonly AgentContext   ctx;

    private MaterialPickup target;
    private ExitZone       exit;
    private Phase          phase;
    private float          phaseTimer;
    private float          repathTimer;
    private float          elapsed;
    private float          blockedTimer;
    private Vector3        lostPoint;
    private int            carried;
    private int            collected;
    private int            secured;
    private int            fled;
    private float          miningTimer;
    private MoriMochiAgent fleeFrom;
    private MoriMochiAgent guardian;
    private float          fleeCooldownUntil;

    internal AgentGatherer(MoriMochiAgent owner, AgentContext ctx)
    {
        this.owner = owner;
        this.ctx   = ctx;
    }

    internal bool TryEngage(ExpeditionRulesSO rules)
    {
        if (carried >= ctx.Stats.CarryCapacity) return BeginReturn(rules);

        var site = PlannedSite(rules);
        if (site != null)
        {
            target       = site;
            ctx.State    = AgentState.Expedition;
            elapsed      = 0f;
            repathTimer  = rules.RepathInterval;
            blockedTimer = 0f;
            phase        = Phase.Moving;
            ctx.SetStopped(false);
            ctx.SetDestinationSafe(ExpeditionNav.ApproachPoint(ctx, owner, target, rules));
            return true;
        }

        bool small = ctx.Orders.Loot == LootChoice.Small;
        float bestScore = float.NegativeInfinity;
        Percept bestPercept = default;
        ExpeditionRuleBase bestRule = null;

        for (int i = 0; i < ctx.Percepts.Count; i++)
        {
            var p = ctx.Percepts[i];
            var mat = p.Source != null ? p.Source.GetComponent<MaterialPickup>() : null;
            if (!ExpeditionNav.Usable(mat)) continue;
            if (small && mat.IsLode) continue;

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

        if (bestRule == null && small)
        {
            for (int i = 0; i < ctx.Percepts.Count; i++)
            {
                var p = ctx.Percepts[i];
                var mat = p.Source != null ? p.Source.GetComponent<MaterialPickup>() : null;
                if (!ExpeditionNav.Usable(mat)) continue;

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
                ctx.SetDestinationSafe(ExpeditionNav.ApproachPoint(ctx, owner, target, rules));
            }

            return true;
        }

        if (carried > 0) return BeginReturn(rules);

        return false;
    }

    private float MiningSeconds(ExpeditionRulesSO rules) =>
        target != null && target.IsDrop ? rules.DropPickupSecondsPerUnit :
        target != null && target.IsLode ? rules.LodeMiningSecondsPerUnit : rules.MiningSecondsPerUnit;

    private MaterialPickup PlannedSite(ExpeditionRulesSO rules)
    {
        if (ctx.Occupation == Occupation.Break || ctx.Occupation == Occupation.Decoy)
        {
            var drop = ExpeditionNav.NearestDrop(ctx, rules.DropPickupRadius);
            if (drop != null) return drop;
        }

        bool small = ctx.Orders.Loot == LootChoice.Small;
        var post = ExpeditionNav.InjectedPost(ctx);
        if (post != null && (!small || !post.IsLode)) return post;
        if (ctx.Board != null)
        {
            var known = ctx.Board.BestKnownVein(ctx.Body.position, null, small);
            if (known != null) return known;
            var nearest = ctx.Board.NearestSite(ctx.Body.position, small);
            if (nearest != null) return nearest;
        }
        return null;
    }

    public bool Tick(ExpeditionRulesSO rules)
    {
        bool validatesTarget = phase == Phase.Noticing || phase == Phase.Moving || phase == Phase.Mining;
        if (validatesTarget && !ExpeditionNav.Usable(target))
        {
            EnterLosing(rules);
            return rules.LoseSeconds > 0f;
        }

        float dt = Time.deltaTime;
        elapsed += dt;
        if ((phase == Phase.Noticing || phase == Phase.Moving) && elapsed > rules.GiveUpSeconds) return false;

        bool watchful = (phase == Phase.Noticing || phase == Phase.Moving || phase == Phase.Mining || phase == Phase.Returning) &&
                        ctx.Orders.Contact == ContactChoice.Flee;
        guardian = watchful ? ExpeditionNav.FighterAllyNear(ctx, owner, rules.GuardTrustRadius) : null;
        if (watchful && guardian == null && Time.time >= fleeCooldownUntil)
        {
            var rival = ExpeditionNav.NearestRival(ctx, owner, out float sqr);
            if (rival != null && ExpeditionNav.IsThreat(rival) && sqr <= rules.FleeTriggerDistance * rules.FleeTriggerDistance)
            {
                BeginFlee(rival, rules);
                return true;
            }
        }

        switch (phase)
        {
            case Phase.Noticing:
                phaseTimer -= dt;
                if (phaseTimer <= 0f)
                {
                    phase = Phase.Moving;
                    ctx.SetStopped(false);
                    ctx.SetDestinationSafe(ExpeditionNav.ApproachPoint(ctx, owner, target, rules));
                    repathTimer = rules.RepathInterval;
                }
                break;

            case Phase.Moving:
                repathTimer -= dt;
                if (repathTimer <= 0f)
                {
                    repathTimer = rules.RepathInterval;
                    ctx.SetDestinationSafe(ExpeditionNav.ApproachPoint(ctx, owner, target, rules));
                }

                Vector3 approach = ExpeditionNav.ApproachPoint(ctx, owner, target, rules);
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
                    miningTimer  = MiningSeconds(rules);
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

                    if (target.Taken || carried >= ctx.Stats.CarryCapacity)
                    {
                        if (carried > 0 && ctx.HomeExit != null) BeginReturn(rules);
                        else return false;
                    }
                    else
                    {
                        miningTimer = MiningSeconds(rules);
                    }
                }
                break;

            case Phase.Losing:
                ExpeditionNav.FaceToward(ctx, lostPoint, dt);

                phaseTimer -= dt;
                if (phaseTimer <= 0f) return false;
                break;

            case Phase.Returning:
                if (exit == null) return false;

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
                    if (rules.DepositSeconds <= 0f) { Secure(); return false; }
                }
                break;

            case Phase.Securing:
                ExpeditionNav.FaceToward(ctx, exit.transform.position, dt);

                phaseTimer -= dt;
                if (phaseTimer <= 0f) { Secure(); return false; }
                break;

            case Phase.Fleeing:
                phaseTimer  -= dt;
                repathTimer -= dt;
                if (repathTimer <= 0f)
                {
                    repathTimer = rules.RepathInterval;
                    ctx.SetDestinationSafe(FleeDestination(rules));
                }
                if (phaseTimer <= 0f)
                {
                    if (carried > 0 && ctx.HomeExit != null) return BeginReturn(rules);
                    return false;
                }
                break;
        }

        return true;
    }

    private void BeginFlee(MoriMochiAgent rival, ExpeditionRulesSO rules)
    {
        fled++;
        fleeFrom          = rival;
        target            = null;
        phase             = Phase.Fleeing;
        phaseTimer        = rules.FleeSeconds;
        repathTimer       = 0f;
        blockedTimer      = 0f;
        fleeCooldownUntil = Time.time + rules.FleeSeconds + rules.FleeCooldown;
        ctx.State         = AgentState.Expedition;
        ctx.SetStopped(false);
        ctx.SetDestinationSafe(FleeDestination(rules));
        owner.EmitEmote(EmoteKind.Molesto);
    }

    private Vector3 FleeDestination(ExpeditionRulesSO rules)
    {
        Vector3 threat = fleeFrom != null ? fleeFrom.transform.position : ctx.Body.position + ctx.Body.forward;

        Vector3 pull = Vector3.zero;
        bool hasPull = false;

        if (carried > 0 && ctx.HomeExit != null)
        {
            pull    = ctx.HomeExit.transform.position;
            hasPull = true;
        }
        else if (ctx.Orders.Posture == PostureChoice.Protect)
        {
            var ally = ExpeditionNav.NearestAlly(ctx, owner, rules.AllyPullRadius, out _);
            if (ally != null)
            {
                pull    = ally.transform.position;
                hasPull = true;
            }
            else if (ctx.HomeExit != null)
            {
                pull    = ctx.HomeExit.transform.position;
                hasPull = true;
            }
        }
        else if (ctx.HomeExit != null)
        {
            pull    = ctx.HomeExit.transform.position;
            hasPull = true;
        }

        return ExpeditionNav.FleePoint(ctx, threat, pull, hasPull, rules.FleeDistance);
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
    }

    private void EnterLosing(ExpeditionRulesSO rules)
    {
        lostPoint  = target != null ? target.transform.position : ctx.Body.position + ctx.Body.forward;
        target     = null;
        phase      = Phase.Losing;
        phaseTimer = rules.LoseSeconds;
        ctx.SetStopped(true);
        owner.EmitEmote(EmoteKind.Molesto);
    }

    private void Drop(ExpeditionRulesSO rules)
    {
        Vector3 pos = ctx.Body.position;
        if (NavMesh.SamplePosition(pos, out var hit, 2f, NavMesh.AllAreas)) pos = hit.position;

        var drop = Object.Instantiate(rules.DropPrefab, pos, Quaternion.identity);
        drop.transform.localScale = Vector3.one * rules.DropScale;
        drop.SetValue(carried);
        drop.SetDrop();
    }

    internal void OnKnocked(ExpeditionRulesSO rules)
    {
        if (carried > 0 && !ctx.Stats.KeepCarryOnKnock)
        {
            if (rules != null && rules.DropPrefab != null) Drop(rules);
            carried = 0;
        }

        Cancel();
    }

    public void Cancel()
    {
        target       = null;
        exit         = null;
        fleeFrom     = null;
        guardian     = null;
        phase        = Phase.Noticing;
        phaseTimer   = 0f;
        miningTimer  = 0f;
        blockedTimer = 0f;
        lostPoint    = Vector3.zero;
    }

    public void ResetForReuse()
    {
        Cancel();
        elapsed           = 0f;
        repathTimer       = 0f;
        carried           = 0;
        collected         = 0;
        secured           = 0;
        fled              = 0;
        fleeCooldownUntil = 0f;
    }

    internal int Carried   => carried;
    internal float FleeCooldown01 => ExpeditionRulesSO.Current != null && ExpeditionRulesSO.Current.FleeSeconds + ExpeditionRulesSO.Current.FleeCooldown > 0f
        ? Mathf.Clamp01((fleeCooldownUntil - Time.time) / (ExpeditionRulesSO.Current.FleeSeconds + ExpeditionRulesSO.Current.FleeCooldown))
        : 0f;
    internal int CarryCapacity => ctx.Stats.CarryCapacity;
    internal int Collected => collected;
    internal int Secured   => secured;
    internal int Fled      => fled;
    internal MoriMochiAgent Guardian => guardian;

    internal float MiningProgress =>
        phase == Phase.Mining && ExpeditionRulesSO.Current != null && MiningSeconds(ExpeditionRulesSO.Current) > 0f
            ? 1f - miningTimer / MiningSeconds(ExpeditionRulesSO.Current)
            : 0f;

    public Transform TargetTransform =>
        (phase == Phase.Noticing || phase == Phase.Moving || phase == Phase.Mining)
            ? (target != null ? target.transform : null) :
        (phase == Phase.Returning || phase == Phase.Securing)
            ? (exit != null ? exit.transform : null) : null;

    public CreatureIntent Intent =>
        phase == Phase.Mining    ? CreatureIntent.Taking :
        phase == Phase.Losing    ? CreatureIntent.Losing :
        phase == Phase.Returning ? CreatureIntent.Carrying :
        phase == Phase.Securing  ? CreatureIntent.Securing :
        phase == Phase.Fleeing   ? CreatureIntent.Fleeing :
        CreatureIntent.Collecting;
}
}
