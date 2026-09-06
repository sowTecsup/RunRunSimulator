using UnityEngine;
namespace MoriMonchiSimulator
{

internal class AgentScout
{
    private enum Step { Traveling, Reporting }

    private readonly MoriMochiAgent owner;
    private readonly AgentContext   ctx;

    private MaterialPickup site;
    private Step           step;
    private float          timer;
    private float          repathTimer;
    private float          elapsed;
    private float          blockedTimer;
    private float          restUntil;
    private int            reports;

    internal AgentScout(MoriMochiAgent owner, AgentContext ctx)
    {
        this.owner = owner;
        this.ctx   = ctx;
    }

    internal int Reports => reports;
    internal Transform TargetTransform => site != null ? site.transform : null;
    internal CreatureIntent Intent => step == Step.Reporting ? CreatureIntent.Reporting : CreatureIntent.Exploring;

    internal bool TryEngage(ExpeditionRulesSO rules)
    {
        var board = ctx.Board;
        if (board == null || Time.time < restUntil) return false;

        site = board.NextSite(ctx.Body.position, out bool newCycle);
        if (site == null) return false;
        if (newCycle)
        {
            site      = null;
            restUntil = Time.time + rules.ScoutRestSeconds;
            return false;
        }

        step         = Step.Traveling;
        elapsed      = 0f;
        repathTimer  = 0f;
        blockedTimer = 0f;
        ctx.State    = AgentState.Expedition;
        ctx.SetStopped(false);
        ctx.SetDestinationSafe(ApproachPoint(rules));
        return true;
    }

    internal bool Tick(ExpeditionRulesSO rules)
    {
        var board = ctx.Board;
        if (board == null || site == null || !site.gameObject.activeInHierarchy) return false;

        float dt = Time.deltaTime;
        elapsed += dt;
        ReportSeen(board, rules);

        switch (step)
        {
            case Step.Traveling:
                if (elapsed > rules.GiveUpSeconds)
                {
                    board.MarkVisited(site);
                    return false;
                }

                repathTimer -= dt;
                if (repathTimer <= 0f)
                {
                    repathTimer = rules.RepathInterval;
                    ctx.SetDestinationSafe(ApproachPoint(rules));
                }

                Vector3 to  = site.transform.position - ctx.Body.position; to.y = 0f;
                float   rim = site.Radius + ctx.Agent.radius + rules.ScoutArriveDistance;
                bool arrived = to.magnitude <= rim;

                if (!arrived && ctx.Agent.velocity.magnitude < 0.05f && to.magnitude <= rim + 1.5f)
                {
                    blockedTimer += dt;
                    if (blockedTimer > 0.8f) arrived = true;
                }
                else blockedTimer = 0f;

                if (arrived)
                {
                    board.MarkVisited(site);
                    bool fresh = !site.Taken && board.ReportVein(site, Time.time, rules.ReportRepeatSeconds);
                    if (fresh) reports++;

                    step  = Step.Reporting;
                    timer = rules.ReportSeconds;
                    ctx.SetStopped(true);
                    owner.EmitEmote(fresh ? EmoteKind.Curioso : EmoteKind.Feliz);
                }
                break;

            case Step.Reporting:
                Vector3 face = site.transform.position - ctx.Body.position; face.y = 0f;
                if (face.sqrMagnitude > 0.001f)
                    ctx.Body.rotation = Quaternion.Slerp(
                        ctx.Body.rotation, Quaternion.LookRotation(face.normalized, Vector3.up), 10f * dt);

                timer -= dt;
                if (timer <= 0f) return false;
                break;
        }

        return true;
    }

    internal void Cancel()
    {
        site         = null;
        step         = Step.Traveling;
        timer        = 0f;
        blockedTimer = 0f;
    }

    internal void ResetForReuse()
    {
        Cancel();
        elapsed     = 0f;
        repathTimer = 0f;
        restUntil   = 0f;
        reports     = 0;
    }

    private void ReportSeen(TeamBlackboard board, ExpeditionRulesSO rules)
    {
        for (int i = 0; i < ctx.Percepts.Count; i++)
        {
            var p = ctx.Percepts[i];
            if (p.Kind != PerceivableKind.Material || p.Source == null) continue;

            var vein = p.Source.GetComponent<MaterialPickup>();
            if (vein == null || vein == site || vein.Taken || !vein.gameObject.activeInHierarchy) continue;

            if (board.ReportVein(vein, Time.time, rules.ReportRepeatSeconds)) reports++;
        }
    }

    private Vector3 ApproachPoint(ExpeditionRulesSO rules)
    {
        Vector3 center = site.transform.position;
        Vector3 dir    = ctx.Body.position - center; dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = ctx.Body.forward;
        dir.Normalize();

        Vector3 point = center + dir * (site.Radius + ctx.Agent.radius + rules.ScoutArriveDistance * 0.5f);
        point.y = center.y;
        return point;
    }
}
}
