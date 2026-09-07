using UnityEngine;
namespace MoriMonchiSimulator
{

internal class AgentExpedition
{
    private readonly MoriMochiAgent owner;
    private readonly AgentContext   ctx;
    private readonly AgentGatherer  gatherer;
    private readonly AgentGuard     guard;
    private readonly AgentHunter    hunter;
    private readonly AgentDecoy     decoy;
    private readonly AgentScout     scout;

    private IExpeditionTask active;

    internal AgentExpedition(MoriMochiAgent owner, AgentContext ctx)
    {
        this.owner = owner;
        this.ctx   = ctx;
        gatherer   = new AgentGatherer(owner, ctx);
        guard      = new AgentGuard(owner, ctx);
        hunter     = new AgentHunter(owner, ctx);
        decoy      = new AgentDecoy(owner, ctx);
        scout      = new AgentScout(owner, ctx);
    }

    private Occupation Occupation => ctx.Occupation == Occupation.None ? Occupation.Gather : ctx.Occupation;

    internal bool TryEngage()
    {
        var rules = ExpeditionRulesSO.Current;
        if (rules == null || ctx.Dna == null) return false;

        switch (Occupation)
        {
            case Occupation.Guard:
                if (Start(guard, guard.TryEngage(rules))) return true;
                return Start(gatherer, gatherer.TryEngage(rules));
            case Occupation.Break:
                if (Start(hunter, hunter.TryEngage(rules))) return true;
                return Start(gatherer, gatherer.TryEngage(rules));
            case Occupation.Decoy:
                if (Start(decoy, decoy.TryEngage(rules))) return true;
                return Start(gatherer, gatherer.TryEngage(rules));
            case Occupation.Explore:
                if (Start(scout, scout.TryEngage(rules))) return true;
                return Start(gatherer, gatherer.TryEngage(rules));
            default:
                return Start(gatherer, gatherer.TryEngage(rules));
        }
    }

    private bool Start(IExpeditionTask task, bool engaged)
    {
        active = engaged ? task : null;
        return engaged;
    }

    internal void TickExpedition()
    {
        var rules = ExpeditionRulesSO.Current;
        if (rules == null || active == null) { Abort(); return; }

        if (active == gatherer && PostureEngages(rules))
        {
            gatherer.Cancel();
            return;
        }

        if (!active.Tick(rules)) { Abort(); return; }

        bool lootNearby = (active == hunter || active == decoy) && ExpeditionNav.NearestDrop(ctx, rules.DropPickupRadius) != null;
        if (active != gatherer && (lootNearby || IdleSeconds(active) >= rules.IdleMineSeconds) && gatherer.TryEngage(rules))
        {
            active.Cancel();
            active = gatherer;
        }
    }

    private bool PostureEngages(ExpeditionRulesSO rules)
    {
        if (ExpeditionNav.NearestRival(ctx, owner, out _) == null) return false;

        IExpeditionTask task;
        bool engaged;
        switch (Occupation)
        {
            case Occupation.Guard: task = guard;  engaged = guard.TryEngage(rules);  break;
            case Occupation.Break: task = hunter; engaged = hunter.TryHunt(rules);   break;
            case Occupation.Decoy: task = decoy;  engaged = decoy.TryEngage(rules);  break;
            default:               return false;
        }

        if (!engaged) return false;
        active = task;
        return true;
    }

    private float IdleSeconds(IExpeditionTask task) =>
        task == guard  ? guard.IdleSeconds  :
        task == hunter ? hunter.IdleSeconds :
        task == decoy  ? decoy.IdleSeconds  : -1f;

    private void Abort()
    {
        if (active != null) active.Cancel();
        active = null;
        owner.RequestRoam();
    }

    internal void OnKnocked()
    {
        gatherer.OnKnocked(ExpeditionRulesSO.Current);
        hunter.OnKnocked(ExpeditionRulesSO.Current);
        Cancel();
    }

    internal void Cancel()
    {
        gatherer.Cancel();
        guard.Cancel();
        hunter.Cancel();
        decoy.Cancel();
        scout.Cancel();
        active = null;
    }

    internal void ResetForReuse()
    {
        gatherer.ResetForReuse();
        guard.ResetForReuse();
        hunter.ResetForReuse();
        decoy.ResetForReuse();
        scout.ResetForReuse();
        active = null;
    }

    internal int   Carried        => gatherer.Carried;
    internal int   CarryCapacity  => gatherer.CarryCapacity;
    internal int   Collected      => gatherer.Collected;
    internal int   Secured        => gatherer.Secured;
    internal int   Fled           => gatherer.Fled;
    internal int   Reports        => scout.Reports;
    internal MoriMochiAgent Guardian => gatherer.Guardian;
    internal float FleeCooldown01  => gatherer.FleeCooldown01;
    internal float DecoyCooldown01 => decoy.Cooldown01;
    internal float Retreat01       => hunter.Retreat01;
    internal bool  IsChasing       => guard.IsChasing || hunter.IsChasing;
    internal float MiningProgress => gatherer.MiningProgress;
    internal Transform      TargetTransform => active != null ? active.TargetTransform : null;
    internal CreatureIntent Intent          => active != null ? active.Intent : CreatureIntent.Collecting;
}
}
