using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
namespace MoriMonchiSimulator
{

public partial class MoriMochiAgent
{
    public bool IsHeld => state == AgentState.Carried;
    // True only while ragdolling after a throw — not while carried, not while NavMesh-driven.
    // A container admits only creatures for which this is true (thrown in), never walk-ins.
    public bool IsAirborne => state == AgentState.Thrown;
    public CreatureDNA DNA => dna;

    // True while this creature is confined to a pen (breeding/store container). The NameTag
    // reads it to swap to the pen layout (gender + name + personality, plus heart/timer if breeding).
    public bool IsPenned => currentContainer != null;
    public bool IsCourting => state == AgentState.Courting;
    public bool IsRecovering => state == AgentState.Recovering;
    private bool IsBreeding => dna != null && dna.BusyState == BusyReason.Breeding;

    // True while the creature is actively reacting to the player in a friendly way (not fleeing).
    // The NameTag polls this to show the pet hint — no dot product here so the hint doesn't flicker.
    public bool IsInFriendlyReaction =>
        state == AgentState.Reacting && activeReaction != ProximityReaction.Flee;

    // True for a brief moment after the player pets this creature — drives the "Petting…" label.
    public bool IsBeingPetted => pettingDisplayTimer > 0f;

    // True when this creature is in a friendly Reacting state and the player is facing it.
    public bool CanBePetted =>
        state == AgentState.Reacting &&
        activeReaction != ProximityReaction.Flee &&
        IsPlayerFacingMe();

    // What this creature is trying to do RIGHT NOW, for the NameTag. Maps the internal
    // AgentState (+ active reaction / reserved need) to the player-facing CreatureIntent;
    // the tag turns it into words. Need-driven intents read reservedStation.Need so the
    // verb matches the station kind it's heading to / using.
    public CreatureIntent Intent => state switch
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

    private void TickIdle()
    {
        if (TryEnterNeedSeeking()) return;
        if (ReactIfPlayerNear()) return;

        idleTimer += Time.deltaTime;
        if (idleTimer >= idleDuration) EnterRoaming();
    }

    private void TickRoaming()
    {
        if (TryEnterNeedSeeking()) return;
        if (ReactIfPlayerNear()) return;

        if (!agent.isOnNavMesh) return;
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            // Reached the waypoint — maybe pause, depending on personality.
            if (Random.value < profile.IdleChance) EnterIdle();
            else                                    EnterRoaming();
        }
    }

    private void TickReacting()
    {
        // A need takes priority over any reaction, same as Idle/Roaming. If a station is
        // free, go use it. Otherwise, a friendly reaction (approach/follow/retreat) must
        // NOT keep it glued to the player once it's no longer Healthy — drop back to the
        // base behavior so the need-degrade path runs and ReactIfPlayerNear won't re-arm a
        // friendly reaction while it's critical. Flee stays: that IS the stress response.
        if (TryEnterNeedSeeking()) return;
        if (activeReaction != ProximityReaction.Flee && Condition != CreatureCondition.Healthy)
        {
            state = stateBeforeReact;
            if (state == AgentState.Idle) EnterIdle(); else EnterRoaming();
            return;
        }

        // Friendly reactions time out so the creature doesn't stay glued to the player forever.
        // Flee is excluded — it's need-driven and must keep running until the need is met or the player leaves.
        if (activeReaction != ProximityReaction.Flee)
        {
            reactingTimer += Time.deltaTime;
            if (reactingTimer >= followDuration)
            {
                reactCooldownTimer = reactCooldown;   // won't react again until the cooldown expires
                state = stateBeforeReact;
                if (state == AgentState.Idle) EnterIdle(); else EnterRoaming();
                return;
            }
        }

        float dist = PlanarDistanceToPlayer();

        // Player left (with hysteresis) → resume previous behavior.
        // Non-flee reactions also start the cooldown so the creature doesn't immediately follow again
        // if the player circles back.
        if (player == null || dist > profile.ProximityRadius * 1.25f)
        {
            if (activeReaction != ProximityReaction.Flee) reactCooldownTimer = reactCooldown;
            state = stateBeforeReact;
            if (state == AgentState.Idle) EnterIdle(); else EnterRoaming();
            return;
        }

        repathTimer -= Time.deltaTime;
        if (repathTimer > 0f) return;
        repathTimer = repathInterval;

        Vector3 self = transform.position;
        Vector3 toPlayer = (player.position - self); toPlayer.y = 0f;
        Vector3 dirAway  = (-toPlayer).normalized;

        switch (activeReaction)
        {
            case ProximityReaction.Flee:
                SetDestinationSafe(self + dirAway * profile.RoamRadius * 1.5f);
                break;
            case ProximityReaction.Retreat:
                SetDestinationSafe(self + dirAway * Mathf.Max(1f, profile.FollowDistance));
                break;
            case ProximityReaction.Approach:
            case ProximityReaction.Follow:
                // Stop a comfortable distance short of the player.
                Vector3 stop = player.position - toPlayer.normalized * profile.FollowDistance;
                SetDestinationSafe(stop);
                break;
        }
    }
    // ── Needs (decay + seeking) ───────────────────────────────────

    // Per-frame need decay. Runs only here → non-spawned creatures (registry only) don't decay.
    // Pure in-memory mutation of the shared DNA object: NO GameEvents, so it never pushes to Cloud
    // Save per frame (anti-saturation — see NeedsState/GameManager). Energy drains only while moving.
    private void TickNeeds(float dt)
    {
        if (profile == null || dna == null) return;

        dna.Needs.AddHealth(-healthDecayPerSecond * dt);
        dna.Needs.AddAffect(-affectDecayPerSecond * dt);
        if (IsMoving) dna.Needs.AddEnergy(-energyDecayPerSecond * dt);
    }

    private bool IsMoving =>
        agent != null && agent.enabled && agent.isOnNavMesh && !agent.isStopped &&
        agent.velocity.sqrMagnitude > 0.01f;

    // If a need is critical, reserve the closest available station and head there (SeekingNeed).
    // Returns true if it took over this frame. No station free → returns false and the agent keeps
    // roaming with the need unmet (it just won't react to the player — see ReactIfPlayerNear).
    private bool TryEnterNeedSeeking()
    {
        if (currentContainer != null) return false;        // penned creatures can't wander to stations
        if (!TryGetCriticalNeed(out var need)) return false;

        var station = NeedStationRegistry.GetClosest(transform.position, need);
        if (station == null) return false;
        // Reserve the closest free, reachable slot (one per use point — handles unknown furniture
        // orientation). Fails if the station is full or no free slot snaps onto our area → stay degraded.
        if (!station.TryReserve(this, transform.position, agent.areaMask, sampleRadius, out var usePos)) return false;

        reservedStation      = station;
        state                = AgentState.SeekingNeed;
        agent.updateRotation = true;
        SetStopped(false);
        SetDestinationSafe(usePos);   // the reserved slot — held until full / interrupted
        return true;
    }

    // Most urgent unmet need (priority Health > Energy > Affect). False if all are fine.
    private bool TryGetCriticalNeed(out NeedType need)
    {
        if (dna.Needs.Health <= criticalHealth) { need = NeedType.Health; return true; }
        if (dna.Needs.Energy <= criticalEnergy) { need = NeedType.Energy; return true; }
        if (dna.Needs.Affect <= criticalAffect) { need = NeedType.Affect; return true; }
        need = NeedType.Health;
        return false;
    }

    private void TickSeekingNeed()
    {
        if (reservedStation == null) { EnterRoaming(); return; }   // station vanished / stolen → re-plan

        if (!agent.isOnNavMesh) return;
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            SetStopped(true);                 // arrived → hold position and start consuming
            state = AgentState.UsingStation;
        }
    }

    private void TickUsingStation()
    {
        if (reservedStation == null) { EnterRoaming(); return; }

        if (reservedStation.Refill(dna.Needs, Time.deltaTime))
            EnterRoaming();                   // full → EnterRoaming releases the station + unstops
    }

    private void TickCourting()
    {
        if (courtPartner == null || courtPartner.DNA == null ||
            courtPartner.DNA.BusyState != BusyReason.Breeding)
        {
            ExitCourtship();
            return;
        }

        if (courtRole == CourtRole.Orbit) TickOrbit();
        else                              TickTend();

        FacePartner();
    }

    private void TickOrbit()
    {
        courtAngle += courtAngularSpeed * Mathf.Deg2Rad * Time.deltaTime;

        courtRepathTimer -= Time.deltaTime;
        if (courtRepathTimer > 0f) return;
        courtRepathTimer = courtRepath;

        float   a      = courtAngle + courtLookahead * Mathf.Deg2Rad;
        Vector3 center = courtPartner.transform.position;
        Vector3 target = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * courtOrbitRadius;
        SetDestinationSafe(target);
    }

    private void TickTend()
    {
        courtRepathTimer -= Time.deltaTime;
        if (courtRepathTimer > 0f) return;
        courtRepathTimer = courtTendInterval;

        Vector2 d = Random.insideUnitCircle * courtTendRadius;
        SetDestinationSafe(courtAnchor + new Vector3(d.x, 0f, d.y));
    }

    private void FacePartner()
    {
        if (courtPartner == null) return;
        Vector3 dir = courtPartner.transform.position - transform.position; dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        transform.rotation = Quaternion.Slerp(
            transform.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), 10f * Time.deltaTime);
    }

    // Drops the reserved station (if any). Called on every transition out of seeking/using.
    private void ReleaseStation()
    {
        if (reservedStation == null) return;
        reservedStation.Release(this);
        reservedStation = null;
    }
    // ── Transitions ───────────────────────────────────────────────

    private void EnterIdle()
    {
        state        = AgentState.Idle;
        idleTimer    = 0f;
        idleDuration = Random.Range(profile.IdleMin, profile.IdleMax);
        if (agent.enabled && agent.isOnNavMesh) agent.ResetPath();
    }

    private void EnterRoaming()
    {
        ReleaseStation();                   // leaving any need-seeking/using cleanly
        agent.speed = baseSpeed;            // drop any courtship speed boost
        state = AgentState.Roaming;
        agent.updateRotation = true;        // hand rotation back to the agent (Recovering turns it off)
        SetStopped(false);
        SetDestinationSafe(NextRoamDestination());
    }

    // Where to wander next. Penned: a point inside the pen's bounds (the breeding-only areaMask
    // keeps it from pathing out; the bounds keep it in THIS pen even if two pens' floors touch).
    // Free: mostly nearby, but with AreaPreference odds pull toward the preferred area.
    private Vector3 NextRoamDestination()
    {
        if (currentContainer != null)
            return RandomPointInBounds(currentContainer.InteriorBounds);

        return (Random.value < profile.AreaPreference && TryGetPreferredPoint(out var pref))
            ? pref
            : transform.position + Random.insideUnitSphere * profile.RoamRadius;
    }

    // Samples a point on the creature's preferred NavMesh area. Fails gracefully if the
    // area isn't configured or none is reachable nearby (caller falls back to random).
    private bool TryGetPreferredPoint(out Vector3 point)
    {
        point = transform.position;
        int idx = NavMesh.GetAreaFromName(profile.PreferredArea.ToString());
        if (idx < 0) return false;

        Vector3 probe = transform.position + Random.insideUnitSphere * (profile.RoamRadius * 3f);
        if (NavMesh.SamplePosition(probe, out var hit, profile.RoamRadius * 3f, 1 << idx))
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
        if (player == null) return false;
        if (PlanarDistanceToPlayer() > profile.ProximityRadius) return false;
        if (reactCooldownTimer > 0f) return false;   // still cooling down from the last reaction

        // Stress (Affect emergency, no PlayZone freed it) → flee the player. This IS the need response,
        // not "following" the player, so it stays even though the creature isn't Healthy.
        if (dna != null && dna.Needs.Affect <= criticalAffect)
            return BeginReaction(ProximityReaction.Flee);

        // Friendly reactions (follow / approach / retreat) only when nothing is critical — a creature
        // with an emergency keeps prioritizing its need and ignores the player.
        if (Condition != CreatureCondition.Healthy) return false;
        if (profile.Reaction == ProximityReaction.Ignore) return false;

        // Penned: the ONLY restricted behavior is coming to the player — skip approach/follow inside the
        // pen. Everything else (flee/retreat, roaming, idle) keeps running normally.
        if (currentContainer != null &&
            (profile.Reaction == ProximityReaction.Approach || profile.Reaction == ProximityReaction.Follow))
            return false;

        return BeginReaction(profile.Reaction);
    }

    private bool BeginReaction(ProximityReaction reaction)
    {
        activeReaction   = reaction;
        stateBeforeReact = state == AgentState.Idle ? AgentState.Idle : AgentState.Roaming;
        state            = AgentState.Reacting;
        repathTimer      = 0f;
        reactingTimer    = 0f;
        return true;
    }
    // True when the player is within petRadius AND their horizontal forward aligns with the
    // direction from the player to this creature (petLookAngle cone). Uses player.forward
    // (the body yaw set by Move(), always horizontal) — no camera pitch issues, no creature
    // forward dependency.
    public bool IsPlayerFacingMe()
    {
        if (player == null) return false;

        Vector3 toMe = transform.position - player.position; toMe.y = 0f;
        if (toMe.sqrMagnitude > petRadius * petRadius) return false;
        if (toMe.sqrMagnitude < 0.001f) return true;

        Vector3 playerFwd = player.forward; playerFwd.y = 0f;
        if (playerFwd.sqrMagnitude < 0.001f) return true;

        float threshold = Mathf.Cos(petLookAngle * Mathf.Deg2Rad);
        return Vector3.Dot(playerFwd.normalized, toMe.normalized) >= threshold;
    }

    // IInteractable — tap E while facing a creature in a friendly reaction to pet it.
    // Gives an Affect boost, starts the cooldown so it won't immediately follow again,
    // and sends the creature back to its own business.
    public void Interact()
    {
        if (!CanBePetted) return;
        dna?.Needs.AddAffect(affectOnPet);
        reactCooldownTimer  = reactCooldown;
        pettingDisplayTimer = 1.5f;   // NameTag shows "Petting…" for 1.5 s
        onPet?.Invoke();
        EnterRoaming();
    }
}
}
