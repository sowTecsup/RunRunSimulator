using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

internal static class ExpeditionNav
{
    internal static bool Usable(MaterialPickup m) => m != null && !m.Taken && m.gameObject.activeInHierarchy;

    internal static bool IsThreat(MoriMochiAgent rival)
    {
        if (rival == null || rival.IsAirborne || rival.IsHeld || rival.IsRecovering) return false;
        var intent = rival.Intent;
        return intent == CreatureIntent.Hunting || intent == CreatureIntent.Guarding || intent == CreatureIntent.Clashing ||
               intent == CreatureIntent.Taunting || intent == CreatureIntent.Fighting;
    }

    internal static MaterialPickup InjectedPost(AgentContext ctx)
    {
        if (ctx.GuardPost == null) return null;
        var post = ctx.GuardPost.GetComponent<MaterialPickup>();
        return Usable(post) ? post : null;
    }

    internal static MaterialPickup FindPost(AgentContext ctx, bool excludeLode = false)
    {
        MaterialPickup best = null;
        int   bestRemaining = -1;
        float bestSqrDist   = float.PositiveInfinity;

        for (int i = 0; i < ctx.Percepts.Count; i++)
        {
            var p = ctx.Percepts[i];
            if (p.Kind != PerceivableKind.Material) continue;
            var mat = p.Source != null ? p.Source.GetComponent<MaterialPickup>() : null;
            if (!Usable(mat)) continue;
            if (excludeLode && mat.IsLode) continue;

            if (mat.Remaining > bestRemaining || (mat.Remaining == bestRemaining && p.SqrDistance < bestSqrDist))
            {
                best          = mat;
                bestRemaining = mat.Remaining;
                bestSqrDist   = p.SqrDistance;
            }
        }

        return best;
    }

    internal static bool IsThiefIntent(CreatureIntent intent) =>
        intent == CreatureIntent.Taking || intent == CreatureIntent.Carrying ||
        intent == CreatureIntent.Securing || intent == CreatureIntent.Collecting;

    internal static MoriMochiAgent FindPrey(AgentContext ctx, MoriMochiAgent owner)
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
            if (rival.TrustedGuardian != null) continue;
            if (!IsThiefIntent(rival.Intent)) continue;
            if (p.SqrDistance >= bestSqrDist) continue;

            best        = rival;
            bestSqrDist = p.SqrDistance;
        }

        return best;
    }

    internal static MoriMochiAgent FindDecoyTarget(AgentContext ctx, MoriMochiAgent owner)
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

    internal static MoriMochiAgent NearestRival(AgentContext ctx, MoriMochiAgent owner, out float sqrDist)
    {
        MoriMochiAgent nearest = null;
        sqrDist = float.PositiveInfinity;

        for (int i = 0; i < ctx.Percepts.Count; i++)
        {
            var p = ctx.Percepts[i];
            if (p.Kind != PerceivableKind.Monchi) continue;
            if (!ExpeditionTeams.AreRivals(owner.Team, p.Team)) continue;
            if (p.SqrDistance >= sqrDist) continue;

            var rival = p.Source != null ? p.Source.Monchi : null;
            if (rival == null) continue;

            nearest = rival;
            sqrDist = p.SqrDistance;
        }

        return nearest;
    }

    internal static MoriMochiAgent FighterAllyNear(AgentContext ctx, MoriMochiAgent owner, float maxDistance)
    {
        float maxSqr = maxDistance * maxDistance;

        for (int i = 0; i < ctx.Percepts.Count; i++)
        {
            var p = ctx.Percepts[i];
            if (p.Kind != PerceivableKind.Monchi) continue;
            if (!ExpeditionTeams.AreAllies(owner.Team, p.Team)) continue;
            if (p.SqrDistance > maxSqr) continue;

            var ally = p.Source != null ? p.Source.Monchi : null;
            if (ally == null || ally == owner) continue;
            if (ally.Orders.Contact != ContactChoice.Fight) continue;
            if (ally.IsAirborne || ally.IsHeld || ally.IsRecovering || ally.Intent == CreatureIntent.Dazed) continue;

            return ally;
        }

        return null;
    }

    internal static MaterialPickup NearestDrop(AgentContext ctx, float maxDistance)
    {
        MaterialPickup best = null;
        float bestSqr = maxDistance * maxDistance;

        for (int i = 0; i < ctx.Percepts.Count; i++)
        {
            var p = ctx.Percepts[i];
            if (p.Kind != PerceivableKind.Material || p.SqrDistance >= bestSqr) continue;
            var mat = p.Source != null ? p.Source.GetComponent<MaterialPickup>() : null;
            if (!Usable(mat) || !mat.IsDrop) continue;

            best = mat;
            bestSqr = p.SqrDistance;
        }

        return best;
    }

    internal static MoriMochiAgent NearestTaunter(AgentContext ctx, MoriMochiAgent owner, float maxDistance)
    {
        MoriMochiAgent nearest = null;
        float bestSqr = maxDistance * maxDistance;

        for (int i = 0; i < ctx.Percepts.Count; i++)
        {
            var p = ctx.Percepts[i];
            if (p.Kind != PerceivableKind.Monchi) continue;
            if (!ExpeditionTeams.AreRivals(owner.Team, p.Team)) continue;
            if (p.SqrDistance >= bestSqr) continue;

            var rival = p.Source != null ? p.Source.Monchi : null;
            if (rival == null || rival.IsAirborne || rival.IsHeld || rival.IsRecovering) continue;
            if (rival.Intent != CreatureIntent.Taunting) continue;

            nearest = rival;
            bestSqr = p.SqrDistance;
        }

        return nearest;
    }

    internal static MoriMochiAgent NearestAlly(AgentContext ctx, MoriMochiAgent owner, float maxDistance, out float sqrDist)
    {
        MoriMochiAgent nearest = null;
        sqrDist = maxDistance * maxDistance;

        for (int i = 0; i < ctx.Percepts.Count; i++)
        {
            var p = ctx.Percepts[i];
            if (p.Kind != PerceivableKind.Monchi) continue;
            if (!ExpeditionTeams.AreAllies(owner.Team, p.Team)) continue;
            if (p.SqrDistance >= sqrDist) continue;

            var ally = p.Source != null ? p.Source.Monchi : null;
            if (ally == null || ally == owner) continue;

            nearest = ally;
            sqrDist = p.SqrDistance;
        }

        return nearest;
    }

    internal static Vector3 ApproachPoint(AgentContext ctx, MoriMochiAgent owner, MaterialPickup target, ExpeditionRulesSO rules)
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

    internal static Vector3 GuardPoint(AgentContext ctx, MaterialPickup post, ExpeditionRulesSO rules)
    {
        Vector3 center = post.transform.position;
        Vector3 dir    = ctx.HomeExit != null
            ? ctx.HomeExit.transform.position - center
            : ctx.Body.position - center;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = ctx.Body.forward;
        dir.Normalize();

        Vector3 point = center + dir * (rules.GuardRadius * 0.6f);
        point.y = center.y;
        return point;
    }

    internal static void FaceToward(AgentContext ctx, Vector3 point, float dt)
    {
        Vector3 dir = point - ctx.Body.position; dir.y = 0f;
        if (dir.sqrMagnitude <= 0.001f) return;
        ctx.Body.rotation = Quaternion.Slerp(ctx.Body.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), 10f * dt);
    }

    internal static bool HoldAtPost(AgentContext ctx, MaterialPickup post, ExpeditionRulesSO rules, ref float repathTimer, float dt)
    {
        Vector3 toPost = post.transform.position - ctx.Body.position; toPost.y = 0f;
        if (toPost.magnitude > rules.GuardRadius)
        {
            ctx.SetStopped(false);
            repathTimer -= dt;
            if (repathTimer <= 0f)
            {
                repathTimer = rules.RepathInterval;
                ctx.SetDestinationSafe(GuardPoint(ctx, post, rules));
            }
            return false;
        }

        ctx.SetStopped(true);
        return true;
    }

    internal static Vector3 FleePoint(AgentContext ctx, Vector3 threat, Vector3 pull, bool hasPull, float distance)
    {
        Vector3 position = ctx.Body.position;
        Vector3 away = position - threat; away.y = 0f;
        away = away.sqrMagnitude > 0.0001f ? away.normalized : -ctx.Body.forward;

        Vector3 dir = away;
        if (hasPull)
        {
            Vector3 toPull = pull - position; toPull.y = 0f;
            if (toPull.sqrMagnitude > 0.0001f) dir = (away + toPull.normalized * 0.8f).normalized;
        }

        Vector3 side = new Vector3(-away.z, 0f, away.x);
        Vector3[] candidates = { dir, away, (away + side).normalized, (away - side).normalized, side, -side };
        int mask = ctx.Agent != null && ctx.Agent.enabled ? ctx.Agent.areaMask : NavMesh.AllAreas;

        for (int i = 0; i < candidates.Length; i++)
        {
            Vector3 point = position + candidates[i] * distance;
            if (NavMesh.SamplePosition(point, out var hit, 3f, mask)) return hit.position;
        }

        return position;
    }
}
}
