using UnityEngine;
namespace MoriMonchiSimulator
{

internal class AgentGuard : IExpeditionTask
{
    private readonly MoriMochiAgent owner;
    private readonly AgentContext   ctx;

    private MaterialPickup post;
    private MoriMochiAgent chasing;
    private float          chaseUntil;
    private float          repathTimer;
    private float          idle;

    internal AgentGuard(MoriMochiAgent owner, AgentContext ctx)
    {
        this.owner = owner;
        this.ctx   = ctx;
    }

    internal bool TryEngage(ExpeditionRulesSO rules)
    {
        post = ExpeditionNav.InjectedPost(ctx) ?? ExpeditionNav.FindPost(ctx);
        if (post == null) return false;

        ctx.State   = AgentState.Expedition;
        chasing     = null;
        repathTimer = 0f;
        ctx.SetStopped(false);
        ctx.SetDestinationSafe(ExpeditionNav.GuardPoint(ctx, post, rules));
        return true;
    }

    public bool Tick(ExpeditionRulesSO rules)
    {
        if (!ExpeditionNav.Usable(post)) return false;

        float dt = Time.deltaTime;

        if (chasing != null)
        {
            Vector3 toChase = chasing.transform.position - ctx.Body.position; toChase.y = 0f;
            bool lost = Time.time >= chaseUntil || chasing.IsAirborne || chasing.IsHeld || chasing.IsRecovering ||
                        toChase.magnitude > rules.GuardChaseRadius * 1.75f;
            if (lost)
            {
                chasing     = null;
                repathTimer = 0f;
            }
            else
            {
                repathTimer -= dt;
                if (repathTimer <= 0f)
                {
                    repathTimer = rules.HuntRepathInterval;
                    ctx.SetStopped(false);
                    ctx.SetDestinationSafe(chasing.transform.position);
                }
                return true;
            }
        }

        var taunter = ExpeditionNav.NearestTaunter(ctx, owner, rules.GuardChaseRadius);
        if (taunter != null)
        {
            chasing     = taunter;
            chaseUntil  = Time.time + rules.GuardChaseSeconds;
            repathTimer = rules.HuntRepathInterval;
            ctx.SetStopped(false);
            ctx.SetDestinationSafe(taunter.transform.position);
            owner.EmitEmote(EmoteKind.Molesto);
            return true;
        }

        if (ExpeditionNav.HoldAtPost(ctx, post, rules, ref repathTimer, dt))
        {
            var rival = ExpeditionNav.NearestRival(ctx, owner, out _);
            ExpeditionNav.FaceToward(ctx, rival != null ? rival.transform.position : post.transform.position, dt);
            idle = rival != null ? 0f : idle + dt;
        }

        return true;
    }

    internal float IdleSeconds => idle;
    internal bool IsChasing => chasing != null;

    public void Cancel()
    {
        post        = null;
        chasing     = null;
        repathTimer = 0f;
        idle        = 0f;
    }

    public void ResetForReuse()
    {
        Cancel();
        chaseUntil = 0f;
    }

    public CreatureIntent Intent => chasing != null ? CreatureIntent.Chasing : CreatureIntent.Guarding;
    public Transform TargetTransform => chasing != null ? chasing.transform : (post != null ? post.transform : null);
}
}
