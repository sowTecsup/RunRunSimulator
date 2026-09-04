using UnityEngine;
namespace MoriMonchiSimulator
{

internal class AgentExpedition
{
    private enum Phase { Noticing, Moving, Taking, Losing }

    private readonly MoriMochiAgent owner;
    private readonly AgentContext   ctx;

    private MaterialPickup target;
    private float          repathTimer;
    private float          elapsed;
    private int            collected;
    private Phase          phase;
    private float          phaseTimer;
    private float          blockedTimer;
    private Vector3        lostPoint;

    internal AgentExpedition(MoriMochiAgent owner, AgentContext ctx)
    {
        this.owner = owner;
        this.ctx   = ctx;
    }

    internal bool TryEngage()
    {
        var rules = ExpeditionRulesSO.Current;
        if (rules == null || ctx.Dna == null || rules.Rules.Count == 0) return false;

        float bestScore = float.NegativeInfinity;
        Percept bestPercept = default;
        ExpeditionRuleBase bestRule = null;

        for (int i = 0; i < ctx.Percepts.Count; i++)
        {
            var p = ctx.Percepts[i];
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

        if (bestRule == null) return false;

        target = bestPercept.Source != null ? bestPercept.Source.GetComponent<MaterialPickup>() : null;
        if (target == null || target.Taken) { target = null; return false; }

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

    internal void TickExpedition()
    {
        var rules = ExpeditionRulesSO.Current;
        if (rules == null) { Abort(); return; }

        if (phase != Phase.Losing && (target == null || target.Taken || !target.gameObject.activeInHierarchy))
        {
            EnterLosing(rules);
            return;
        }

        float dt = Time.deltaTime;
        elapsed += dt;
        if (phase != Phase.Losing && elapsed > rules.GiveUpSeconds) { Abort(); return; }

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
                    phase        = Phase.Taking;
                    phaseTimer   = rules.TakeSeconds;
                    ctx.SetStopped(true);
                    if (rules.TakeSeconds <= 0f) TakeTarget();
                }
                break;

            case Phase.Taking:
                Vector3 dir = target.transform.position - ctx.Body.position; dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    ctx.Body.rotation = Quaternion.Slerp(
                        ctx.Body.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), 10f * dt);

                phaseTimer -= dt;
                if (phaseTimer <= 0f) TakeTarget();
                break;

            case Phase.Losing:
                Vector3 dirBack = lostPoint - ctx.Body.position; dirBack.y = 0f;
                if (dirBack.sqrMagnitude > 0.001f)
                    ctx.Body.rotation = Quaternion.Slerp(
                        ctx.Body.rotation, Quaternion.LookRotation(dirBack.normalized, Vector3.up), 10f * dt);

                phaseTimer -= dt;
                if (phaseTimer <= 0f) Abort();
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

    private void TakeTarget()
    {
        if (target.TryTake(out int v))
        {
            collected += v;
            owner.EmitEmote(EmoteKind.Feliz);
            owner.onPickup?.Invoke();
            Abort();
            return;
        }
        EnterLosing(ExpeditionRulesSO.Current);
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

    private void Abort()
    {
        target       = null;
        phase        = Phase.Noticing;
        phaseTimer   = 0f;
        blockedTimer = 0f;
        lostPoint    = Vector3.zero;
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
    }

    internal int             Collected => collected;
    internal MaterialPickup  Target    => target;
    internal CreatureIntent  Intent    =>
        phase == Phase.Taking ? CreatureIntent.Taking :
        phase == Phase.Losing ? CreatureIntent.Losing :
        CreatureIntent.Collecting;
}
}
