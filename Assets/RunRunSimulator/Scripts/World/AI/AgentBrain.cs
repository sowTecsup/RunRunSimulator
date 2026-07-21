using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

internal class AgentBrain
{
    private readonly MoriMochiAgent owner;
    private readonly AgentContext   ctx;

    private float idleTimer;
    private float idleDuration;
    private float repathTimer;
    private float reactingTimer;
    private float reactCooldownTimer;
    private float pettingDisplayTimer;
    private AgentState stateBeforeReact = AgentState.Roaming;
    private ProximityReaction activeReaction;
    private NeedStation reservedStation;

    internal AgentBrain(MoriMochiAgent owner, AgentContext ctx)
    {
        this.owner = owner;
        this.ctx   = ctx;
    }

    // True for a brief moment after the player pets this creature — drives the "Petting…" label.
    internal bool IsBeingPetted => pettingDisplayTimer > 0f;

    // True while the creature is actively reacting to the player in a friendly way (not fleeing).
    // The NameTag polls this to show the pet hint — no dot product here so the hint doesn't flicker.
    internal bool IsInFriendlyReaction =>
        ctx.State == AgentState.Reacting && activeReaction != ProximityReaction.Flee;

    // True when this creature is in a friendly Reacting state and the player is facing it.
    internal bool CanBePetted =>
        ctx.State == AgentState.Reacting &&
        activeReaction != ProximityReaction.Flee &&
        IsPlayerFacingMe();

    // What this creature is trying to do RIGHT NOW, for the NameTag. Maps the internal
    // AgentState (+ active reaction / reserved need) to the player-facing CreatureIntent;
    // the tag turns it into words. Need-driven intents read reservedStation.Need so the
    // verb matches the station kind it's heading to / using.
    internal CreatureIntent Intent => ctx.State switch
    {
        AgentState.Carried      => CreatureIntent.Held,
        AgentState.Thrown       => CreatureIntent.Tumbling,
        AgentState.Recovering   => CreatureIntent.Tumbling,
        AgentState.SeekingNeed  => SeekIntent(),
        AgentState.UsingStation => UseIntent(),
        AgentState.Reacting     => ReactIntent(),
        AgentState.Idle         => CreatureIntent.Idle,
        _                       => CreatureIntent.Wandering,   // Roaming
    };

    private CreatureIntent SeekIntent() => reservedStation == null ? CreatureIntent.Wandering : reservedStation.Need switch
    {
        NeedType.Health => CreatureIntent.SeekingFood,
        NeedType.Energy => CreatureIntent.SeekingRest,
        _               => CreatureIntent.SeekingPlay,
    };

    private CreatureIntent UseIntent() => reservedStation == null ? CreatureIntent.Wandering : reservedStation.Need switch
    {
        NeedType.Health => CreatureIntent.Eating,
        NeedType.Energy => CreatureIntent.Resting,
        _               => CreatureIntent.Playing,
    };

    private CreatureIntent ReactIntent() => activeReaction switch
    {
        ProximityReaction.Follow   => CreatureIntent.Following,
        ProximityReaction.Approach => CreatureIntent.Approaching,
        ProximityReaction.Flee     => CreatureIntent.Fleeing,
        ProximityReaction.Retreat  => CreatureIntent.Retreating,
        _                          => CreatureIntent.Wandering,
    };
    // ── States ────────────────────────────────────────────────────

    internal void TickIdle()
    {
        if (TryEnterNeedSeeking()) return;
        if (ReactIfPlayerNear()) return;

        idleTimer += Time.deltaTime;
        if (idleTimer >= idleDuration) EnterRoaming();
    }

    internal void TickRoaming()
    {
        if (TryEnterNeedSeeking()) return;
        if (ReactIfPlayerNear()) return;

        if (!ctx.Agent.isOnNavMesh) return;
        if (!ctx.Agent.pathPending && ctx.Agent.remainingDistance <= ctx.Agent.stoppingDistance + 0.1f)
        {
            // Reached the waypoint — maybe pause, depending on personality.
            if (Random.value < ctx.Profile.IdleChance) EnterIdle();
            else                                       EnterRoaming();
        }
    }

    internal void TickReacting()
    {
        // A need takes priority over any reaction, same as Idle/Roaming. If a station is
        // free, go use it. Otherwise, a friendly reaction (approach/follow/retreat) must
        // NOT keep it glued to the player once it's no longer Healthy — drop back to the
        // base behavior so the need-degrade path runs and ReactIfPlayerNear won't re-arm a
        // friendly reaction while it's critical. Flee stays: that IS the stress response.
        if (TryEnterNeedSeeking()) return;
        if (activeReaction != ProximityReaction.Flee && owner.Condition != CreatureCondition.Healthy)
        {
            ctx.State = stateBeforeReact;
            if (ctx.State == AgentState.Idle) EnterIdle(); else EnterRoaming();
            return;
        }

        // Friendly reactions time out so the creature doesn't stay glued to the player forever.
        // Flee is excluded — it's need-driven and must keep running until the need is met or the player leaves.
        if (activeReaction != ProximityReaction.Flee)
        {
            reactingTimer += Time.deltaTime;
            if (reactingTimer >= owner.followDuration)
            {
                reactCooldownTimer = owner.reactCooldown;   // won't react again until the cooldown expires
                ctx.State = stateBeforeReact;
                if (ctx.State == AgentState.Idle) EnterIdle(); else EnterRoaming();
                return;
            }
        }

        float dist = ctx.PlanarDistanceToPlayer();

        // Player left (with hysteresis) → resume previous behavior.
        // Non-flee reactions also start the cooldown so the creature doesn't immediately follow again
        // if the player circles back.
        if (ctx.Player == null || dist > ctx.Profile.ProximityRadius * 1.25f)
        {
            if (activeReaction != ProximityReaction.Flee) reactCooldownTimer = owner.reactCooldown;
            ctx.State = stateBeforeReact;
            if (ctx.State == AgentState.Idle) EnterIdle(); else EnterRoaming();
            return;
        }

        repathTimer -= Time.deltaTime;
        if (repathTimer > 0f) return;
        repathTimer = owner.repathInterval;

        Vector3 self = ctx.Body.position;
        Vector3 toPlayer = (ctx.Player.position - self); toPlayer.y = 0f;
        Vector3 dirAway  = (-toPlayer).normalized;

        switch (activeReaction)
        {
            case ProximityReaction.Flee:
                ctx.SetDestinationSafe(self + dirAway * ctx.Profile.RoamRadius * 1.5f);
                break;
            case ProximityReaction.Retreat:
                ctx.SetDestinationSafe(self + dirAway * Mathf.Max(1f, ctx.Profile.FollowDistance));
                break;
            case ProximityReaction.Approach:
            case ProximityReaction.Follow:
                // Stop a comfortable distance short of the player.
                Vector3 stop = ctx.Player.position - toPlayer.normalized * ctx.Profile.FollowDistance;
                ctx.SetDestinationSafe(stop);
                break;
        }
    }
    // ── Needs (decay + seeking) ───────────────────────────────────

    // Per-frame need decay. Runs only here → non-spawned creatures (registry only) don't decay.
    // Pure in-memory mutation of the shared DNA object: NO GameEvents, so it never pushes to Cloud
    // Save per frame (anti-saturation — see NeedsState/GameManager). Energy drains only while moving.
    private void TickNeeds(float dt)
    {
        if (ctx.Profile == null || ctx.Dna == null) return;

        ctx.Dna.Needs.AddHealth(-owner.healthDecayPerSecond * dt);
        ctx.Dna.Needs.AddAffect(-owner.affectDecayPerSecond * dt);
        if (ctx.IsMoving) ctx.Dna.Needs.AddEnergy(-owner.energyDecayPerSecond * dt);
    }

    // If a need is critical, reserve the closest available station and head there (SeekingNeed).
    // Returns true if it took over this frame. No station free → returns false and the agent keeps
    // roaming with the need unmet (it just won't react to the player — see ReactIfPlayerNear).
    private bool TryEnterNeedSeeking()
    {
        if (ctx.CurrentContainer != null) return false;        // penned creatures can't wander to stations
        if (!TryGetCriticalNeed(out var need)) return false;

        var station = NeedStationRegistry.GetClosest(ctx.Body.position, need);
        if (station == null) return false;
        // Reserve the closest free, reachable slot (one per use point — handles unknown furniture
        // orientation). Fails if the station is full or no free slot snaps onto our area → stay degraded.
        if (!station.TryReserve(ctx.Owner, ctx.Body.position, ctx.Agent.areaMask, owner.sampleRadius, out var usePos)) return false;

        reservedStation      = station;
        ctx.State            = AgentState.SeekingNeed;
        ctx.Agent.updateRotation = true;
        ctx.SetStopped(false);
        ctx.SetDestinationSafe(usePos);   // the reserved slot — held until full / interrupted
        return true;
    }

    // Most urgent unmet need (priority Health > Energy > Affect). False if all are fine.
    private bool TryGetCriticalNeed(out NeedType need)
    {
        if (ctx.Dna.Needs.Health <= owner.criticalHealth) { need = NeedType.Health; return true; }
        if (ctx.Dna.Needs.Energy <= owner.criticalEnergy) { need = NeedType.Energy; return true; }
        if (ctx.Dna.Needs.Affect <= owner.criticalAffect) { need = NeedType.Affect; return true; }
        need = NeedType.Health;
        return false;
    }

    internal void TickSeekingNeed()
    {
        if (reservedStation == null) { EnterRoaming(); return; }   // station vanished / stolen → re-plan

        if (!ctx.Agent.isOnNavMesh) return;
        if (!ctx.Agent.pathPending && ctx.Agent.remainingDistance <= ctx.Agent.stoppingDistance + 0.2f)
        {
            ctx.SetStopped(true);                 // arrived → hold position and start consuming
            ctx.State = AgentState.UsingStation;
        }
    }

    internal void TickUsingStation()
    {
        if (reservedStation == null) { EnterRoaming(); return; }

        if (reservedStation.Refill(ctx.Dna.Needs, Time.deltaTime))
            EnterRoaming();                   // full → EnterRoaming releases the station + unstops
    }

    // Drops the reserved station (if any). Called on every transition out of seeking/using.
    internal void ReleaseStation()
    {
        if (reservedStation == null) return;
        reservedStation.Release(ctx.Owner);
        reservedStation = null;
    }
    // ── Transitions ───────────────────────────────────────────────

    private void EnterIdle()
    {
        ctx.State    = AgentState.Idle;
        idleTimer    = 0f;
        idleDuration = Random.Range(ctx.Profile.IdleMin, ctx.Profile.IdleMax);
        if (ctx.Agent.enabled && ctx.Agent.isOnNavMesh) ctx.Agent.ResetPath();
    }

    internal void EnterRoaming()
    {
        ReleaseStation();                   // leaving any need-seeking/using cleanly
        ctx.Agent.speed = ctx.BaseSpeed;     // drop any courtship speed boost
        ctx.State = AgentState.Roaming;
        ctx.Agent.updateRotation = true;     // hand rotation back to the agent (Recovering turns it off)
        ctx.SetStopped(false);
        ctx.SetDestinationSafe(NextRoamDestination());
    }

    // Where to wander next. Penned: a point inside the pen's bounds (the breeding-only areaMask
    // keeps it from pathing out; the bounds keep it in THIS pen even if two pens' floors touch).
    // Free: mostly nearby, but with AreaPreference odds pull toward the preferred area.
    private Vector3 NextRoamDestination()
    {
        if (ctx.CurrentContainer != null)
            return AgentContext.RandomPointInBounds(ctx.CurrentContainer.InteriorBounds);

        return (Random.value < ctx.Profile.AreaPreference && TryGetPreferredPoint(out var pref))
            ? pref
            : ctx.Body.position + Random.insideUnitSphere * ctx.Profile.RoamRadius;
    }

    // Samples a point on the creature's preferred NavMesh area. Fails gracefully if the
    // area isn't configured or none is reachable nearby (caller falls back to random).
    private bool TryGetPreferredPoint(out Vector3 point)
    {
        point = ctx.Body.position;
        int idx = NavMesh.GetAreaFromName(ctx.Profile.PreferredArea.ToString());
        if (idx < 0) return false;

        Vector3 probe = ctx.Body.position + Random.insideUnitSphere * (ctx.Profile.RoamRadius * 3f);
        if (NavMesh.SamplePosition(probe, out var hit, ctx.Profile.RoamRadius * 3f, 1 << idx))
        {
            point = hit.position;
            return true;
        }
        return false;
    }

    // Enters the reaction state if the player is within range and the personality
    // cares. Returns true if it took over this frame.
    private bool ReactIfPlayerNear()
    {
        if (ctx.Player == null) return false;
        if (ctx.PlanarDistanceToPlayer() > ctx.Profile.ProximityRadius) return false;
        if (reactCooldownTimer > 0f) return false;   // still cooling down from the last reaction

        // Stress (Affect emergency, no PlayZone freed it) → flee the player. This IS the need response,
        // not "following" the player, so it stays even though the creature isn't Healthy.
        if (ctx.Dna != null && ctx.Dna.Needs.Affect <= owner.criticalAffect)
            return BeginReaction(ProximityReaction.Flee);

        // Friendly reactions (follow / approach / retreat) only when nothing is critical — a creature
        // with an emergency keeps prioritizing its need and ignores the player.
        if (owner.Condition != CreatureCondition.Healthy) return false;
        if (ctx.Profile.Reaction == ProximityReaction.Ignore) return false;

        // Penned: the ONLY restricted behavior is coming to the player — skip approach/follow inside the
        // pen. Everything else (flee/retreat, roaming, idle) keeps running normally.
        if (ctx.CurrentContainer != null &&
            (ctx.Profile.Reaction == ProximityReaction.Approach || ctx.Profile.Reaction == ProximityReaction.Follow))
            return false;

        return BeginReaction(ctx.Profile.Reaction);
    }

    private bool BeginReaction(ProximityReaction reaction)
    {
        activeReaction   = reaction;
        stateBeforeReact = ctx.State == AgentState.Idle ? AgentState.Idle : AgentState.Roaming;
        ctx.State        = AgentState.Reacting;
        repathTimer      = 0f;
        reactingTimer    = 0f;
        return true;
    }
    // True when the player is within petRadius AND their horizontal forward aligns with the
    // direction from the player to this creature (petLookAngle cone). Uses player.forward
    // (the body yaw set by Move(), always horizontal) — no camera pitch issues, no creature
    // forward dependency.
    internal bool IsPlayerFacingMe()
    {
        if (ctx.Player == null) return false;

        Vector3 toMe = ctx.Body.position - ctx.Player.position; toMe.y = 0f;
        if (toMe.sqrMagnitude > owner.petRadius * owner.petRadius) return false;
        if (toMe.sqrMagnitude < 0.001f) return true;

        Vector3 playerFwd = ctx.Player.forward; playerFwd.y = 0f;
        if (playerFwd.sqrMagnitude < 0.001f) return true;

        float threshold = Mathf.Cos(owner.petLookAngle * Mathf.Deg2Rad);
        return Vector3.Dot(playerFwd.normalized, toMe.normalized) >= threshold;
    }

    // IInteractable — tap E while facing a creature in a friendly reaction to pet it.
    // Gives an Affect boost, starts the cooldown so it won't immediately follow again,
    // and sends the creature back to its own business.
    internal void Interact()
    {
        if (!CanBePetted) return;
        ctx.Dna?.Needs.AddAffect(owner.affectOnPet);
        reactCooldownTimer  = owner.reactCooldown;
        pettingDisplayTimer = 1.5f;   // NameTag shows "Petting…" for 1.5 s
        owner.onPet?.Invoke();
        EnterRoaming();
    }

    internal void TickAlways(float dt)
    {
        TickNeeds(dt);
        if (reactCooldownTimer > 0f) reactCooldownTimer -= dt;
        if (pettingDisplayTimer > 0f) pettingDisplayTimer -= dt;
    }

    internal void ResetForReuse()
    {
        idleTimer = repathTimer = reactingTimer = reactCooldownTimer = pettingDisplayTimer = 0f;
    }
}
}
