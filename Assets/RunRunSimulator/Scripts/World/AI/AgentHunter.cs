using UnityEngine;
namespace MoriMonchiSimulator
{

internal class AgentHunter : IExpeditionTask
{
    private readonly MoriMochiAgent owner;
    private readonly AgentContext   ctx;

    private MoriMochiAgent prey;
    private MaterialPickup post;
    private float          huntTimer;
    private float          repathTimer;
    private float          elapsed;
    private float          retreatUntil;
    private float          idle;
    private bool           retreating;

    internal AgentHunter(MoriMochiAgent owner, AgentContext ctx)
    {
        this.owner = owner;
        this.ctx   = ctx;
    }

    internal bool TryEngage(ExpeditionRulesSO rules)
    {
        if (Time.time < retreatUntil) return BeginRetreat();

        prey = ExpeditionNav.FindPrey(ctx, owner);
        if (prey != null)
        {
            ctx.State   = AgentState.Expedition;
            post        = null;
            retreating  = false;
            huntTimer   = 0f;
            elapsed     = 0f;
            ctx.SetStopped(false);
            ctx.SetDestinationSafe(prey.transform.position);
            return true;
        }

        post = ExpeditionNav.InjectedPost(ctx) ?? ExpeditionNav.FindPost(ctx);
        if (post == null) return false;

        ctx.State   = AgentState.Expedition;
        prey        = null;
        retreating  = false;
        huntTimer   = 0f;
        repathTimer = 0f;
        elapsed     = 0f;
        ctx.SetStopped(false);
        ctx.SetDestinationSafe(ExpeditionNav.GuardPoint(ctx, post, rules));
        return true;
    }

    private bool BeginRetreat()
    {
        if (ctx.HomeExit == null) return false;

        ctx.State   = AgentState.Expedition;
        prey        = null;
        post        = null;
        retreating  = true;
        repathTimer = 0f;
        elapsed     = 0f;
        ctx.SetStopped(false);
        ctx.SetDestinationSafe(ctx.HomeExit.transform.position);
        return true;
    }

    public bool Tick(ExpeditionRulesSO rules)
    {
        float dt = Time.deltaTime;
        elapsed += dt;

        if (retreating)
        {
            if (Time.time >= retreatUntil || ctx.HomeExit == null) return false;

            Vector3 toExit = ctx.HomeExit.transform.position - ctx.Body.position; toExit.y = 0f;
            if (toExit.magnitude > ctx.HomeExit.Radius)
            {
                ctx.SetStopped(false);
                repathTimer -= dt;
                if (repathTimer <= 0f)
                {
                    repathTimer = rules.RepathInterval;
                    ctx.SetDestinationSafe(ctx.HomeExit.transform.position);
                }
            }
            else
            {
                ctx.SetStopped(true);
                ExpeditionNav.FaceToward(ctx, ctx.Body.position - toExit, dt);
            }

            return true;
        }

        if (prey != null)
        {
            if (elapsed > rules.GiveUpSeconds) return false;

            if (prey.IsAirborne || prey.IsHeld || prey.IsRecovering || !ExpeditionNav.IsThiefIntent(prey.Intent))
            {
                prey = null;
                if (!ExpeditionNav.Usable(post)) return false;
                return true;
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
            if (!ExpeditionNav.Usable(post)) return false;

            if (ExpeditionNav.HoldAtPost(ctx, post, rules, ref repathTimer, dt))
            {
                var rival = ExpeditionNav.NearestRival(ctx, owner, out _);
                ExpeditionNav.FaceToward(ctx, rival != null ? rival.transform.position : post.transform.position, dt);
                idle = rival != null ? 0f : idle + dt;
            }

            huntTimer -= dt;
            if (huntTimer <= 0f)
            {
                huntTimer = rules.HuntRepathInterval;
                var found = ExpeditionNav.FindPrey(ctx, owner);
                if (found != null)
                {
                    prey    = found;
                    elapsed = 0f;
                    ctx.SetStopped(false);
                    ctx.SetDestinationSafe(prey.transform.position);
                }
            }
        }

        return true;
    }

    internal void OnKnocked(ExpeditionRulesSO rules)
    {
        if (rules != null && ctx.Occupation == Occupation.Break) retreatUntil = Time.time + rules.HunterRetreatSeconds;
        Cancel();
    }

    internal float IdleSeconds => idle;
    internal float Retreat01 => ExpeditionRulesSO.Current != null && ExpeditionRulesSO.Current.HunterRetreatSeconds > 0f
        ? Mathf.Clamp01((retreatUntil - Time.time) / ExpeditionRulesSO.Current.HunterRetreatSeconds)
        : 0f;

    public void Cancel()
    {
        prey        = null;
        post        = null;
        retreating  = false;
        huntTimer   = 0f;
        repathTimer = 0f;
        idle        = 0f;
    }

    public void ResetForReuse()
    {
        Cancel();
        elapsed      = 0f;
        retreatUntil = 0f;
    }

    public CreatureIntent Intent => retreating ? CreatureIntent.Retreating : CreatureIntent.Hunting;
    public Transform TargetTransform =>
        retreating ? (ctx.HomeExit != null ? ctx.HomeExit.transform : null) :
        prey != null ? prey.transform : (post != null ? post.transform : null);
}
}
