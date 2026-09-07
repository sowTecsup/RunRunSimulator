using UnityEngine;
namespace MoriMonchiSimulator
{

internal class AgentDecoy : IExpeditionTask
{
    private enum Step { Approach, Taunt, Flee }

    private readonly MoriMochiAgent owner;
    private readonly AgentContext   ctx;

    private MoriMochiAgent prey;
    private MaterialPickup post;
    private Step           step;
    private float          phaseTimer;
    private float          huntTimer;
    private float          repathTimer;
    private float          elapsed;
    private float          cooldownUntil;
    private float          idle;

    internal AgentDecoy(MoriMochiAgent owner, AgentContext ctx)
    {
        this.owner = owner;
        this.ctx   = ctx;
    }

    internal bool TryEngage(ExpeditionRulesSO rules)
    {
        if (Time.time < cooldownUntil) return false;

        var found = ExpeditionNav.FindDecoyTarget(ctx, owner);
        if (found != null)
        {
            ctx.State = AgentState.Expedition;
            post      = null;
            prey      = found;
            step      = Step.Approach;
            huntTimer = 0f;
            elapsed   = 0f;
            ctx.SetStopped(false);
            ctx.SetDestinationSafe(prey.transform.position);
            return true;
        }

        post = ExpeditionNav.InjectedPost(ctx) ?? ExpeditionNav.FindPost(ctx);
        if (post == null) return false;

        ctx.State   = AgentState.Expedition;
        prey        = null;
        step        = Step.Approach;
        huntTimer   = 0f;
        repathTimer = 0f;
        elapsed     = 0f;
        ctx.SetStopped(false);
        ctx.SetDestinationSafe(ExpeditionNav.GuardPoint(ctx, post, rules));
        return true;
    }

    public bool Tick(ExpeditionRulesSO rules)
    {
        float dt = Time.deltaTime;
        elapsed += dt;

        switch (step)
        {
            case Step.Approach:
                if (prey != null)
                {
                    if (prey.IsAirborne || prey.IsHeld || prey.IsRecovering)
                    {
                        prey = null;
                        return true;
                    }

                    if (elapsed > rules.GiveUpSeconds) return End(rules);

                    huntTimer -= dt;
                    if (huntTimer <= 0f)
                    {
                        huntTimer = rules.HuntRepathInterval;
                        ctx.SetDestinationSafe(prey.transform.position);
                    }

                    Vector3 toPrey = prey.transform.position - ctx.Body.position; toPrey.y = 0f;
                    if (toPrey.magnitude <= rules.DecoyRange)
                    {
                        step       = Step.Taunt;
                        phaseTimer = rules.TauntSeconds;
                        ctx.SetStopped(true);
                        owner.EmitEmote(EmoteKind.Molesto);
                    }
                }
                else
                {
                    if (!ExpeditionNav.Usable(post)) return false;
                    ExpeditionNav.HoldAtPost(ctx, post, rules, ref repathTimer, dt);
                    idle = ExpeditionNav.NearestRival(ctx, owner, out _) != null ? 0f : idle + dt;

                    huntTimer -= dt;
                    if (huntTimer <= 0f)
                    {
                        huntTimer = rules.HuntRepathInterval;
                        var found = ExpeditionNav.FindDecoyTarget(ctx, owner);
                        if (found != null)
                        {
                            prey    = found;
                            elapsed = 0f;
                            ctx.SetStopped(false);
                            ctx.SetDestinationSafe(prey.transform.position);
                        }
                    }
                }
                break;

            case Step.Taunt:
                if (prey == null) return End(rules);

                ExpeditionNav.FaceToward(ctx, prey.transform.position, dt);
                phaseTimer -= dt;
                if (phaseTimer <= 0f)
                {
                    step       = Step.Flee;
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

            case Step.Flee:
                phaseTimer -= dt;
                if (phaseTimer <= 0f) return End(rules);
                break;
        }

        return true;
    }

    private bool End(ExpeditionRulesSO rules)
    {
        cooldownUntil = Time.time + rules.DecoyCooldown;
        return false;
    }

    public void Cancel()
    {
        prey        = null;
        post        = null;
        step        = Step.Approach;
        idle        = 0f;
        phaseTimer  = 0f;
        huntTimer   = 0f;
        repathTimer = 0f;
    }

    public void ResetForReuse()
    {
        Cancel();
        elapsed       = 0f;
        cooldownUntil = 0f;
    }

    internal float IdleSeconds => idle;
    internal float Cooldown01 => ExpeditionRulesSO.Current != null && ExpeditionRulesSO.Current.DecoyCooldown > 0f
        ? Mathf.Clamp01((cooldownUntil - Time.time) / ExpeditionRulesSO.Current.DecoyCooldown)
        : 0f;
    public CreatureIntent Intent => step == Step.Flee ? CreatureIntent.Fleeing : CreatureIntent.Taunting;
    public Transform TargetTransform => prey != null ? prey.transform : (post != null ? post.transform : null);
}
}
