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

    private bool petting;
    private float petTimer;
    private float petEmoteTimer;

    private float feedCooldownTimer;
    private float feedHesitateTimer;
    private float feedEatTimer;
    private bool feedHesitated;
    private bool feedEating;

    internal AgentBrain(MoriMochiAgent owner, AgentContext ctx)
    {
        this.owner = owner;
        this.ctx   = ctx;
    }

    internal bool IsBeingPetted => pettingDisplayTimer > 0f;

    internal bool IsInFriendlyReaction =>
        ctx.State == AgentState.Reacting && activeReaction != ProximityReaction.Flee;

    internal bool CanBePetted =>
        ctx.State == AgentState.Reacting &&
        activeReaction != ProximityReaction.Flee &&
        IsPlayerFacingMe();

    internal CreatureIntent Intent => ctx.State switch
    {
        AgentState.Carried      => CreatureIntent.Held,
        AgentState.Thrown       => CreatureIntent.Tumbling,
        AgentState.Recovering   => CreatureIntent.Tumbling,
        AgentState.SeekingNeed  => SeekIntent(),
        AgentState.UsingStation => UseIntent(),
        AgentState.Reacting     => ReactIntent(),
        AgentState.Idle         => CreatureIntent.Idle,
        AgentState.HandFeed     => feedEating ? CreatureIntent.Eating : CreatureIntent.SeekingFood,
        _                       => CreatureIntent.Wandering,
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

    internal void TickIdle()
    {
        if (TryEnterNeedSeeking()) return;
        if (TryEnterHandFeed()) return;
        if (ReactIfPlayerNear()) return;

        idleTimer += Time.deltaTime;
        if (idleTimer >= idleDuration) EnterRoaming();
    }

    internal void TickRoaming()
    {
        if (TryEnterNeedSeeking()) return;
        if (TryEnterHandFeed()) return;
        if (ReactIfPlayerNear()) return;

        if (!ctx.Agent.isOnNavMesh) return;
        if (!ctx.Agent.pathPending && ctx.Agent.remainingDistance <= ctx.Agent.stoppingDistance + 0.1f)
        {
            if (Random.value < ctx.Profile.IdleChance) EnterIdle();
            else                                       EnterRoaming();
        }
    }

    internal void TickReacting()
    {
        if (petting) { TickPetting(); return; }
        if (TryEnterNeedSeeking()) return;
        if (activeReaction != ProximityReaction.Flee && owner.Condition != CreatureCondition.Healthy)
        {
            ctx.State = stateBeforeReact;
            if (ctx.State == AgentState.Idle) EnterIdle(); else EnterRoaming();
            return;
        }

        if (activeReaction != ProximityReaction.Flee)
        {
            reactingTimer += Time.deltaTime;
            if (reactingTimer >= owner.followDuration)
            {
                reactCooldownTimer = owner.reactCooldown;
                ctx.State = stateBeforeReact;
                if (ctx.State == AgentState.Idle) EnterIdle(); else EnterRoaming();
                return;
            }
        }

        float dist = ctx.PlanarDistanceToPlayer();

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
                Vector3 stop = ctx.Player.position - toPlayer.normalized * ctx.Profile.FollowDistance;
                ctx.SetDestinationSafe(stop);
                break;
        }
    }

    private void TickPetting()
    {
        if (ctx.Player == null || petTimer >= owner.petMaxDuration || !IsPlayerFacingMe())
        {
            EndPetSession();
            return;
        }
        float dt = Time.deltaTime;
        petTimer += dt;
        pettingDisplayTimer = 0.3f;
        Vector3 dir = ctx.Player.position - ctx.Body.position; dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            ctx.Body.rotation = Quaternion.Slerp(ctx.Body.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), 8f * dt);
        ctx.Dna?.Needs.AddAffect(owner.petAffectPerSecond * (1f + petTimer * owner.petRampPerSecond) * dt);
        petEmoteTimer -= dt;
        if (petEmoteTimer <= 0f)
        {
            petEmoteTimer = owner.petEmoteInterval;
            owner.EmitEmote(EmoteKind.Corazon);
        }
    }

    private void TickNeeds(float dt)
    {
        if (ctx.Profile == null || ctx.Dna == null) return;

        ctx.Dna.Needs.AddHealth(-owner.healthDecayPerSecond * dt);
        ctx.Dna.Needs.AddAffect(-owner.affectDecayPerSecond * dt);
        if (ctx.IsMoving) ctx.Dna.Needs.AddEnergy(-owner.energyDecayPerSecond * dt);
    }

    private bool TryEnterNeedSeeking()
    {
        if (ctx.CurrentContainer != null) return false;
        if (!TryGetCriticalNeed(out var need)) return false;

        var station = NeedStationRegistry.GetClosest(ctx.Body.position, need);
        if (station == null) return false;
        if (!station.TryReserve(ctx.Owner, ctx.Body.position, ctx.Agent.areaMask, owner.sampleRadius, out var usePos)) return false;

        reservedStation      = station;
        ctx.State            = AgentState.SeekingNeed;
        ctx.Agent.updateRotation = true;
        ctx.SetStopped(false);
        ctx.SetDestinationSafe(usePos);
        return true;
    }

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
        if (reservedStation == null) { EnterRoaming(); return; }

        if (!ctx.Agent.isOnNavMesh) return;
        if (!ctx.Agent.pathPending && ctx.Agent.remainingDistance <= ctx.Agent.stoppingDistance + 0.2f)
        {
            ctx.SetStopped(true);
            ctx.State = AgentState.UsingStation;
        }
    }

    internal void TickUsingStation()
    {
        if (reservedStation == null) { EnterRoaming(); return; }

        if (reservedStation.Refill(ctx.Dna.Needs, Time.deltaTime))
            EnterRoaming();
    }

    internal void ReleaseStation()
    {
        if (reservedStation == null) return;
        reservedStation.Release(ctx.Owner);
        reservedStation = null;
    }

    private bool TryEnterHandFeed()
    {
        if (ctx.CurrentContainer != null) return false;
        if (feedCooldownTimer > 0f) return false;
        if (ctx.Player == null) return false;
        var hotbar = HotbarController.Instance;
        if (hotbar == null || !hotbar.IsOfferingFood) return false;
        if (ctx.Dna == null || ctx.Dna.Needs.Health >= owner.feedHungerThreshold) return false;
        if (ctx.PlanarDistanceToPlayer() > owner.feedNoticeRadius) return false;

        feedHesitated      = ctx.Dna.Sociability >= owner.feedShyBelow;
        feedHesitateTimer  = owner.feedHesitateSeconds;
        feedEating         = false;
        feedEatTimer       = owner.feedEatSeconds;
        repathTimer        = 0f;
        ctx.State          = AgentState.HandFeed;
        ctx.Agent.updateRotation = true;
        ctx.SetStopped(false);
        owner.EmitEmote(EmoteKind.Curioso);
        return true;
    }

    internal void TickHandFeed()
    {
        var hotbar = HotbarController.Instance;
        if (ctx.Player == null || hotbar == null || !hotbar.IsOfferingFood ||
            ctx.PlanarDistanceToPlayer() > owner.feedNoticeRadius * 1.5f)
        {
            feedCooldownTimer = 5f;
            EnterRoaming();
            return;
        }

        float dt   = Time.deltaTime;
        float dist = ctx.PlanarDistanceToPlayer();
        Vector3 toPlayer = ctx.Player.position - ctx.Body.position; toPlayer.y = 0f;

        if (feedEating)
        {
            ctx.SetStopped(true);
            if (toPlayer.sqrMagnitude > 0.001f)
                ctx.Body.rotation = Quaternion.Slerp(ctx.Body.rotation, Quaternion.LookRotation(toPlayer.normalized, Vector3.up), 8f * dt);
            feedEatTimer -= dt;
            if (feedEatTimer <= 0f)
            {
                if (hotbar.TryConsumeActiveFood())
                {
                    ctx.Dna?.Needs.AddHealth(owner.feedHealthBoost);
                    ctx.Dna?.Needs.AddAffect(owner.feedAffectBoost);
                    owner.EmitEmote(EmoteKind.Feliz);
                }
                feedCooldownTimer = owner.feedCooldown;
                EnterRoaming();
            }
            return;
        }

        if (!feedHesitated)
        {
            if (dist > owner.feedShyDistance)
            {
                ctx.SetStopped(false);
                repathTimer -= dt;
                if (repathTimer <= 0f)
                {
                    repathTimer = owner.repathInterval;
                    ctx.SetDestinationSafe(ctx.Player.position - toPlayer.normalized * (owner.feedShyDistance * 0.9f));
                }
            }
            else
            {
                ctx.SetStopped(true);
                if (toPlayer.sqrMagnitude > 0.001f)
                    ctx.Body.rotation = Quaternion.Slerp(ctx.Body.rotation, Quaternion.LookRotation(toPlayer.normalized, Vector3.up), 8f * dt);
                feedHesitateTimer -= dt;
                if (feedHesitateTimer <= 0f) feedHesitated = true;
            }
            return;
        }

        if (dist > owner.feedDistance)
        {
            ctx.SetStopped(false);
            repathTimer -= dt;
            if (repathTimer <= 0f)
            {
                repathTimer = owner.repathInterval;
                ctx.SetDestinationSafe(ctx.Player.position - toPlayer.normalized * (owner.feedDistance * 0.75f));
            }
        }
        else
        {
            feedEating = true;
            ctx.SetStopped(true);
        }
    }

    private void EnterIdle()
    {
        ctx.State    = AgentState.Idle;
        idleTimer    = 0f;
        idleDuration = Random.Range(ctx.Profile.IdleMin, ctx.Profile.IdleMax);
        if (ctx.Agent.enabled && ctx.Agent.isOnNavMesh) ctx.Agent.ResetPath();
    }

    internal void EnterRoaming()
    {
        ReleaseStation();
        ctx.State = AgentState.Roaming;
        ctx.Agent.updateRotation = true;
        ctx.SetStopped(false);
        ctx.SetDestinationSafe(NextRoamDestination());
    }

    private Vector3 NextRoamDestination()
    {
        if (ctx.CurrentContainer != null)
            return AgentContext.RandomPointInBounds(ctx.CurrentContainer.InteriorBounds);

        Vector3 candidate = (Random.value < ctx.Profile.AreaPreference && TryGetPreferredPoint(out var pref))
            ? pref
            : ctx.Body.position + Random.insideUnitSphere * ctx.Profile.RoamRadius;
        return owner.AdjustRoamForAvoidance(candidate);
    }

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

    private bool ReactIfPlayerNear()
    {
        if (ctx.Player == null) return false;
        if (ctx.PlanarDistanceToPlayer() > ctx.Profile.ProximityRadius) return false;
        if (reactCooldownTimer > 0f) return false;

        if (ctx.Dna != null && ctx.Dna.Needs.Affect <= owner.criticalAffect)
            return BeginReaction(ProximityReaction.Flee);

        if (owner.Condition != CreatureCondition.Healthy) return false;
        if (ctx.Profile.Reaction == ProximityReaction.Ignore) return false;

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

    internal bool BeginPetSession()
    {
        if (petting || !CanBePetted) return false;
        petting      = true;
        petTimer     = 0f;
        petEmoteTimer = 0f;
        ctx.SetStopped(true);
        return true;
    }

    internal void EndPetSession()
    {
        if (!petting) return;
        petting             = false;
        reactCooldownTimer  = owner.reactCooldown;
        pettingDisplayTimer = 1.5f;
        owner.onPet?.Invoke();
        EnterRoaming();
    }

    internal void Interact()
    {
        BeginPetSession();
    }

    internal void TickAlways(float dt)
    {
        TickNeeds(dt);
        if (reactCooldownTimer > 0f) reactCooldownTimer -= dt;
        if (pettingDisplayTimer > 0f) pettingDisplayTimer -= dt;
        if (feedCooldownTimer > 0f) feedCooldownTimer -= dt;
    }

    internal void ResetForReuse()
    {
        idleTimer = repathTimer = reactingTimer = reactCooldownTimer = pettingDisplayTimer = 0f;
        petting = false; feedEating = false; feedHesitated = false; feedCooldownTimer = 0f;
    }
}
}
