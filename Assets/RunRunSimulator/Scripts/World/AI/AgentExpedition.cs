using UnityEngine;
namespace MoriMonchiSimulator
{

internal class AgentExpedition
{
    private readonly MoriMochiAgent owner;
    private readonly AgentContext   ctx;

    private MaterialPickup target;
    private float          repathTimer;
    private float          elapsed;
    private int            collected;

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
        ctx.SetStopped(false);
        ctx.SetDestinationSafe(target.transform.position);
        owner.EmitEmote(EmoteKind.Curioso);
        return true;
    }

    internal void TickExpedition()
    {
        var rules = ExpeditionRulesSO.Current;
        if (target == null || rules == null || target.Taken || !target.gameObject.activeInHierarchy)
        {
            Abort();
            return;
        }

        elapsed += Time.deltaTime;
        if (elapsed > rules.GiveUpSeconds) { Abort(); return; }

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = rules.RepathInterval;
            ctx.SetDestinationSafe(target.transform.position);
        }

        Vector3 delta = target.transform.position - ctx.Body.position; delta.y = 0f;
        if (delta.magnitude <= rules.ArriveDistance)
        {
            if (target.TryTake(out int v))
            {
                collected += v;
                owner.EmitEmote(EmoteKind.Feliz);
            }
            Abort();
        }
    }

    private void Abort()
    {
        target = null;
        owner.RequestRoam();
    }

    internal void ResetForReuse()
    {
        target      = null;
        elapsed     = 0f;
        repathTimer = 0f;
    }

    internal int             Collected => collected;
    internal MaterialPickup  Target    => target;
    internal CreatureIntent  Intent    => CreatureIntent.Collecting;
}
}
