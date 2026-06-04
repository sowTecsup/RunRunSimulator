using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

// The runtime "brain" of one MoriMochi cube in the world. Drives a NavMeshAgent
// through a small behavior state machine biased by the creature's Personality
// (via PersonalityProfileSO), and hands off cleanly to physics when the player
// grabs/throws it (IThrowable) — NavMeshAgent ⇄ Rigidbody, the one real technical
// tension here.
//
// Behavior is data-driven: this script never switches on Personality directly, it
// reads the resolved PersonalityProfile. The cube is spawned and wired by
// MoriMochiSpawner, which calls Initialize().
//
// Components: NavMeshAgent drives movement while the Rigidbody stays kinematic.
// On grab the agent is disabled and the Rigidbody goes dynamic (gravity/throw);
// once it settles we sample the nearest NavMesh point, Warp the agent back and
// resume. RequireComponent guarantees both exist.
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public class MoriMochiAgent : MonoBehaviour, IThrowable
{
    // Carried = in the player's hand; Thrown = ragdoll in flight after a release/throw/knock.
    // SeekingNeed = pathing to a NeedStation; UsingStation = stopped, consuming it.
    private enum AgentState { Idle, Roaming, Reacting, Carried, Thrown, Recovering, SeekingNeed, UsingStation }

    // ── Tuning (Odin tabs) ────────────────────────────────────────
    // Grouped to mirror the two concerns this component juggles — the NavMesh "brain"
    // (Movement) and the physics/throwable layer (Physics) — plus Presentation.

    // ── Movement (NavMesh brain) ──
    [TabGroup("Tuning", "Movement"), Title("NavMesh sampling")]
    [Tooltip("Max distance to snap a desired point onto the NavMesh.")]
    [SerializeField] private float sampleRadius = 4f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("How often (s) Reacting/Roaming recomputes its destination.")]
    [SerializeField] private float repathInterval = 0.35f;

    [TabGroup("Tuning", "Movement"), Title("Breeding pen confinement")]
    [Tooltip("NavMesh Area that breeding pens paint their floor with. Free agents EXCLUDE it (so they route around every pen); a penned creature is RESTRICTED to it. Pick the exact Area from Navigation → Areas.")]
    [ValueDropdown(nameof(EditorNavMeshAreaNames))]
    [SerializeField] private string breedingAreaName = "BreedingRoom";

    // ── Needs (decay + thresholds) ──
    [TabGroup("Tuning", "Needs"), Title("Decay per second (only while spawned)")]
    [Tooltip("Health lost per second — passive hunger.")]
    [SerializeField, Min(0f)] private float healthDecayPerSecond = 0.5f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Energy lost per second WHILE MOVING (active life).")]
    [SerializeField, Min(0f)] private float energyDecayPerSecond = 1f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Affect lost per second — drifts toward stress (negative) when neglected.")]
    [SerializeField, Min(0f)] private float affectDecayPerSecond = 0.5f;

    [TabGroup("Tuning", "Needs"), Title("Critical thresholds (seek a station, else degrade)")]
    [Tooltip("Health at/below this → seek a Feeder.")]
    [SerializeField, Range(0f, 100f)] private float criticalHealth = 25f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Energy at/below this → seek a RestZone (and walk slower if none exists).")]
    [SerializeField, Range(0f, 100f)] private float criticalEnergy = 25f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Affect AT OR BELOW this → stressed: seek a PlayZone (and flee the player if none).")]
    [SerializeField, Range(-100f, 100f)] private float criticalAffect = -75f;

    [TabGroup("Tuning", "Needs"), Title("Stress events (affect penalties)")]
    [Tooltip("Affect lost each time the player throws or knocks it.")]
    [SerializeField, Min(0f)] private float affectOnThrow = 8f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Affect lost on a hard collision (impact speed ≥ threshold).")]
    [SerializeField, Min(0f)] private float affectOnHardCollision = 5f;
    [TabGroup("Tuning", "Needs")]
    [Tooltip("Impact speed (m/s) at/above which a collision counts as 'hard' for stress.")]
    [SerializeField, Min(0f)] private float hardImpactThreshold = 4f;

    [TabGroup("Tuning", "Needs"), Title("Degraded behavior (no station available)")]
    [Tooltip("Speed multiplier while energy is critical.")]
    [SerializeField, Range(0.1f, 1f)] private float degradedSpeedMultiplier = 0.5f;

    // ── Physics (throwable layer) ──
    [TabGroup("Tuning", "Physics"), Title("Hold feel (while carried)")]
    [Tooltip("How snappily the body chases the hold anchor while carried.")]
    [SerializeField] private float followSpeed = 15f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Below this speed (and grounded) after a throw, the agent re-joins the NavMesh.")]
    [SerializeField] private float settleSpeed = 0.15f;
    [TabGroup("Tuning", "Physics")]
    [SerializeField] private float settleDelay = 0.4f;

    [TabGroup("Tuning", "Physics"), Title("Throw physics (after release)")]
    [Tooltip("Linear drag applied while airborne/sliding after a throw, so it slows to a stop instead of gliding forever.")]
    [SerializeField] private float thrownLinearDamping = 1.2f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Angular drag applied after a throw so it stops spinning.")]
    [SerializeField] private float thrownAngularDamping = 2f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Extra ray length below the body used to confirm it's resting on the floor before it settles.")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Safety net: it recovers no matter what this many seconds after a throw, even if still sliding.")]
    [SerializeField] private float maxThrownTime = 6f;

    [TabGroup("Tuning", "Physics"), Title("Bounce (plushie throw)")]
    [Tooltip("Fraction of speed kept on each bounce (0 = dead drop, 1 = no energy loss). Lower settles sooner.")]
    [Range(0f, 1f)]
    [SerializeField] private float bounciness = 0.55f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("How many reflections it gets before it's allowed to stop bouncing and settle.")]
    [SerializeField] private int maxBounces = 3;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Impacts slower than this don't count as a bounce — it just settles (avoids endless micro-bounces).")]
    [SerializeField] private float minBounceSpeed = 1.5f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Random tumble added on each bounce so it reads as a soft plushie, not a billiard ball. 0 = none.")]
    [SerializeField] private float bounceSpin = 4f;

    [TabGroup("Tuning", "Physics"), Title("Knock (throwable vs throwable)")]
    [Tooltip("Fraction of impact speed transferred to a creature you slam into. ~1 = full, higher = explosive ragdoll.")]
    [SerializeField] private float knockTransfer = 1.2f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Upward pop blended into the knock so the creature you hit launches a bit instead of just sliding.")]
    [Range(0f, 1f)]
    [SerializeField] private float knockUpBias = 0.35f;

    [TabGroup("Tuning", "Physics"), Title("Recovery (after being thrown)")]
    [Tooltip("Seconds it stays down/dazed where it landed before standing up. Scaled per-personality by RecoverySpeed.")]
    [SerializeField] private float downedDelay = 0.6f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("How long the get-up takes — it rotates from its tumbled pose back upright before the agent resumes. Scaled per-personality by RecoverySpeed.")]
    [SerializeField] private float getUpDuration = 0.5f;
    [TabGroup("Tuning", "Physics")]
    [Tooltip("Random ±jitter (fraction) on the get-up timing so even same-personality creatures don't rise in lockstep.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float getUpJitter = 0.15f;

    // ── Presentation (visuals + juice) ──
    [TabGroup("Tuning", "Presentation"), Title("Visuals")]
    [Tooltip("Renderer to tint by personality. The root has no mesh — this is the renderer under the 'Model' child. Auto-found if left empty.")]
    [SerializeField] private Renderer bodyRenderer;

    // Juice hook points. They fire UnityEvents now (compiles without Feel installed).
    // When Feel lands: drop an MMF_Player on the prefab and wire its PlayFeedbacks()
    // into the matching event in the inspector — zero code coupling. These are the
    // template every future "has visual juice" script should follow.
    [TabGroup("Tuning", "Presentation"), Title("Feedbacks (Feel-ready — wire MMF_Player.PlayFeedbacks here)")]
    [SerializeField] private UnityEvent onGrab;     // player picked it up
    [TabGroup("Tuning", "Presentation")]
    [SerializeField] private UnityEvent onThrow;    // player threw it
    [TabGroup("Tuning", "Presentation")]
    [SerializeField] private UnityEvent onBounce;   // each reflection off a surface mid-flight
    [TabGroup("Tuning", "Presentation")]
    [SerializeField] private UnityEvent onLand;     // settled on the ground (before the get-up beat)
    [TabGroup("Tuning", "Presentation")]
    [SerializeField] private UnityEvent onGetUp;    // finished standing up, resumes roaming

    // ── Injected ──────────────────────────────────────────────────
    private CreatureDNA        dna;
    private PersonalityProfile profile;
    private Transform          player;
    private NameTag            nameTag;

    // ── Components / state ────────────────────────────────────────
    private NavMeshAgent agent;
    private Rigidbody    rb;
    private Collider     col;
    private AgentState   state = AgentState.Idle;

    private Transform holdAnchor;     // non-null only while carried (state == Carried)
    private float     settleTimer;
    private float     thrownTimer;    // time since the last release/throw (drives the safety timeout)
    private Vector3   lastVelocity;   // velocity captured each FixedUpdate while airborne, reflected on impact
    private int       bounceCount;    // reflections used this flight
    private float     idleTimer;
    private float     idleDuration;
    private float     repathTimer;
    private AgentState stateBeforeReact = AgentState.Roaming;

    // Confinement (breeding pen): non-null only while penned. While confined the areaMask is
    // restricted to the BreedingRoom area (can't path onto normal floor) and roam destinations
    // are sampled inside the pen's bounds. Set by EnterConfinement, cleared on grab.
    private MoriMochiContainer currentContainer;
    private int freeAreaMask;       // AllAreas minus BreedingRoom — the normal roaming mask
    private int confinedAreaMask;   // only BreedingRoom — applied while penned

    // Need-seeking: the station reserved while SeekingNeed/UsingStation (released on arrival-full,
    // grab, or any transition out). activeReaction = the reaction in play this Reacting beat (lets
    // stress force a Flee over the personality's default).
    private NeedStation       reservedStation;
    private ProximityReaction activeReaction;

    // Get-up animation (Recovering): lerp from the tumbled pose back to upright.
    // Effective timings are per-throw (personality scale + jitter), cached at BeginGetUp.
    private float      recoverTimer;
    private float      effDownedDelay;
    private float      effGetUpDuration;
    private Quaternion getUpFrom;
    private Quaternion getUpTo;

    // Shader color slots — set both so the tint works on URP (_BaseColor) and built-in (_Color).
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId     = Shader.PropertyToID("_Color");

    public bool IsHeld => state == AgentState.Carried;
    // True only while ragdolling after a throw — not while carried, not while NavMesh-driven.
    // A container admits only creatures for which this is true (thrown in), never walk-ins.
    public bool IsAirborne => state == AgentState.Thrown;
    public CreatureDNA DNA => dna;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb    = GetComponent<Rigidbody>();
        col   = GetComponent<Collider>();

        // The mesh lives under the "Model" child, not the root. Resolve it if not wired.
        if (bodyRenderer == null)
        {
            var model    = transform.Find("Model");
            bodyRenderer = model != null ? model.GetComponentInChildren<Renderer>() : null;
        }

        rb.isKinematic = true;          // NavMeshAgent drives until we get thrown
        rb.useGravity  = false;
    }

    // Wired by the spawner. profileTable + player may be resolved here as a fallback.
    public void Initialize(CreatureDNA creature, PersonalityProfileSO profileTable, Transform playerTransform)
    {
        dna     = creature;
        profile = (profileTable != null ? profileTable : PersonalityProfileSO.Current)?.GetProfile(creature.Personality)
                  ?? PersonalityProfile.Neutral();
        player  = playerTransform;

        nameTag = GetComponent<NameTag>();
        if (nameTag != null) nameTag.Bind(creature);

        agent.speed = profile.MoveSpeed;

        // Penned creatures are gated to the BreedingRoom area; free ones get everything EXCEPT it,
        // so they route around every pen (cost wouldn't fence — only the mask does). If the Area
        // isn't set up yet (-1), fall back to AllAreas so behavior degrades gracefully.
        int breeding     = NavMesh.GetAreaFromName(breedingAreaName);
        confinedAreaMask = breeding >= 0 ? 1 << breeding : NavMesh.AllAreas;
        freeAreaMask     = breeding >= 0 ? NavMesh.AllAreas & ~(1 << breeding) : NavMesh.AllAreas;
        agent.areaMask   = freeAreaMask;     // free movement — personality is a preference, never a fence

        ApplyTint(profile.Tint);

        EnterRoaming();
    }

    private void Update()
    {
        TickNeeds(Time.deltaTime);
        switch (state)
        {
            case AgentState.Idle:         TickIdle();         break;
            case AgentState.Roaming:      TickRoaming();      break;
            case AgentState.Reacting:     TickReacting();     break;
            case AgentState.Thrown:       TickThrown();       break;
            case AgentState.Recovering:   TickRecovering();   break;
            case AgentState.SeekingNeed:  TickSeekingNeed();  break;
            case AgentState.UsingStation: TickUsingStation(); break;
            // Carried: nothing to tick — the carry-follow runs in FixedUpdate.
        }
    }

    private void FixedUpdate()
    {
        // Carried: chase the anchor by velocity so it stays a solid physics body.
        if (state == AgentState.Carried && holdAnchor != null)
            rb.linearVelocity = (holdAnchor.position - rb.position) * followSpeed;
        // In flight after a throw: remember the pre-impact velocity so OnCollisionEnter
        // can reflect it (rb.velocity there is already mangled by the contact response).
        else if (state == AgentState.Thrown)
            lastVelocity = rb.linearVelocity;
    }

    // While flying after a throw: reflect off surfaces (plushie bounce) and slam any
    // OTHER throwable it hits into a flying ragdoll too (chain reaction). Normal
    // gameplay collisions (kinematic roaming, held) are ignored.
    private void OnCollisionEnter(Collision collision)
    {
        if (state != AgentState.Thrown) return;

        float impact = lastVelocity.magnitude;
        if (impact < minBounceSpeed) return;

        // A hard knock is stressful.
        if (impact >= hardImpactThreshold) dna?.Needs.AddAffect(-affectOnHardCollision);

        // Hit another throwable → knock it flying away from us, with an upward pop.
        var other = collision.collider.GetComponentInParent<IThrowable>();
        if (other != null && !ReferenceEquals(other, this))
        {
            Vector3 push = collision.transform.position - transform.position; push.y = 0f;
            push = (push.normalized + Vector3.up * knockUpBias).normalized;
            other.Knock(push * impact * knockTransfer);
        }

        // Bounce off whatever we hit (floor, wall, or that other creature).
        if (bounceCount < maxBounces)
        {
            Vector3 normal = collision.GetContact(0).normal;
            rb.linearVelocity = Vector3.Reflect(lastVelocity, normal) * bounciness;
            if (bounceSpin > 0f)
                rb.AddTorque(Random.insideUnitSphere * bounceSpin, ForceMode.Impulse);

            bounceCount++;
            settleTimer = 0f;   // a bounce isn't "resting" — restart the settle clock
            onBounce?.Invoke();
        }
    }

    // Knocked by another thrown object (IThrowable contract). If currently NavMesh-
    // controlled, hand off to physics like a throw; then apply the impulse so it
    // ragdolls away and can bounce / chain into others.
    public void Knock(Vector3 force)
    {
        if (state == AgentState.Carried) return;   // in the player's hand — don't yank it out
        if (currentContainer != null) return;      // penned: tackle-proof — only the player can take it out

        ReleaseStation();           // interrupt any need-seeking/using cleanly
        DetachToPhysics();
        ApplyThrownPhysics();
        holdAnchor = null;
        state      = AgentState.Thrown;

        dna?.Needs.AddAffect(-affectOnThrow);   // being slammed around is stressful
        rb.AddForce(force, ForceMode.Impulse);
        onThrow?.Invoke();          // reuse the throw juice for the knock
    }

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

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            // Reached the waypoint — maybe pause, depending on personality.
            if (Random.value < profile.IdleChance) EnterIdle();
            else                                    EnterRoaming();
        }
    }

    private void TickReacting()
    {
        float dist = PlanarDistanceToPlayer();

        // Player left (with hysteresis) → resume previous behavior.
        if (player == null || dist > profile.ProximityRadius * 1.25f)
        {
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

        ApplyDegradedSpeed();   // crawl when out of energy (degraded behavior if no RestZone)
    }

    private bool IsMoving =>
        agent != null && agent.enabled && agent.isOnNavMesh && !agent.isStopped &&
        agent.velocity.sqrMagnitude > 0.01f;

    // If a need is critical, reserve the closest available station and head there (SeekingNeed).
    // Returns true if it took over this frame. No station free → returns false and the agent keeps
    // roaming DEGRADED (slower / fleeing — handled in TickNeeds + ReactIfPlayerNear).
    private bool TryEnterNeedSeeking()
    {
        if (currentContainer != null) return false;        // penned creatures can't wander to stations
        if (!TryGetCriticalNeed(out var need)) return false;

        var station = NeedStationRegistry.GetClosest(transform.position, need);
        if (station == null || !station.TryReserve(this)) return false;

        reservedStation      = station;
        state                = AgentState.SeekingNeed;
        agent.updateRotation = true;
        SetStopped(false);
        SetDestinationSafe(station.UsePosition);
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

    // Drops the reserved station (if any). Called on every transition out of seeking/using.
    private void ReleaseStation()
    {
        if (reservedStation == null) return;
        reservedStation.Release(this);
        reservedStation = null;
    }

    // isStopped throws if the agent isn't on a NavMesh — guard it.
    private void SetStopped(bool stopped)
    {
        if (agent.enabled && agent.isOnNavMesh) agent.isStopped = stopped;
    }

    // Degraded movement: crawl when energy is critical (no RestZone pulled it into SeekingNeed).
    private void ApplyDegradedSpeed()
    {
        agent.speed = profile.MoveSpeed * (dna.Needs.Energy <= criticalEnergy ? degradedSpeedMultiplier : 1f);
    }

    private void TickThrown()
    {
        thrownTimer += Time.deltaTime;

        // Settle only when it's actually slow AND resting on the floor — a low velocity
        // mid-bounce or while sliding off a ledge shouldn't count.
        bool resting = rb.linearVelocity.sqrMagnitude < settleSpeed * settleSpeed && IsGrounded();
        if (resting) settleTimer += Time.deltaTime;
        else         settleTimer  = 0f;

        // Recover when it has rested long enough, or as a safety net if it never stops
        // (frictionless floor, wedged against geometry) so it can't slide/hang forever.
        if (settleTimer >= settleDelay || thrownTimer >= maxThrownTime)
            BeginGetUp();
    }

    // Down-ray from the body center. A ray starting inside a convex collider doesn't
    // report that collider, so any hit within reach means there's floor under us.
    private bool IsGrounded()
    {
        float reach = (col != null ? col.bounds.extents.y : 0.5f) + groundCheckDistance;
        if (Physics.Raycast(transform.position, Vector3.down, out var hit, reach, ~0, QueryTriggerInteraction.Ignore))
            return hit.collider != col;
        return false;
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

        // Stressed (no PlayZone got it out of here) → flee the player regardless of personality.
        bool stressed = dna != null && dna.Needs.Affect <= criticalAffect;
        var reaction  = stressed ? ProximityReaction.Flee : profile.Reaction;
        if (reaction == ProximityReaction.Ignore) return false;
        if (PlanarDistanceToPlayer() > profile.ProximityRadius) return false;

        activeReaction   = reaction;
        stateBeforeReact = state == AgentState.Idle ? AgentState.Idle : AgentState.Roaming;
        state            = AgentState.Reacting;
        repathTimer      = 0f;
        return true;
    }

    // ── IThrowable (physics handoff) ──────────────────────────────

    public void OnGrab(Transform anchor)
    {
        ReleaseStation();   // grabbing mid-need interrupts SeekingNeed/UsingStation cleanly

        // Lifting a penned creature is the only way out: drop it from the pen's census and hand
        // its areaMask back to free, so wherever it's next thrown it roams normally again.
        if (currentContainer != null)
        {
            currentContainer.Release(this);
            currentContainer = null;
            agent.areaMask   = freeAreaMask;
        }

        holdAnchor  = anchor;
        state       = AgentState.Carried;
        settleTimer = 0f;

        DetachToPhysics();
        rb.useGravity      = false;          // floats to the hand while held
        rb.angularVelocity = Vector3.zero;
        rb.linearDamping   = 0f;             // crisp follow while carried
        rb.angularDamping  = 0.05f;

        onGrab?.Invoke();
    }

    public void OnRelease()
    {
        holdAnchor = null;
        state      = AgentState.Thrown;   // TickThrown watches for it to settle, then BeginGetUp()
        ApplyThrownPhysics();
    }

    public void OnThrow(Vector3 force)
    {
        OnRelease();
        rb.AddForce(force, ForceMode.Impulse);
        dna?.Needs.AddAffect(-affectOnThrow);   // being thrown around is stressful
        onThrow?.Invoke();
    }

    // ── Physics handoff (NavMeshAgent ⇄ Rigidbody) ────────────────

    // Stop NavMesh steering and let physics own the body (carry/throw/knock). Idempotent — safe to
    // call when already detached. Callers set the gravity/damping for their specific case after.
    private void DetachToPhysics()
    {
        if (agent.enabled) agent.enabled = false;
        rb.isKinematic = false;
    }

    // Free-fall physics + reset the flight counters. Shared by throw / release / knock (they differ
    // only in the impulse applied afterwards).
    private void ApplyThrownPhysics()
    {
        rb.useGravity     = true;
        rb.linearDamping  = thrownLinearDamping;
        rb.angularDamping = thrownAngularDamping;
        settleTimer       = 0f;
        thrownTimer       = 0f;
        bounceCount       = 0;
    }

    // Hand the body back to the NavMeshAgent at 'desired' (snapped onto 'mask'). Returns false if it
    // couldn't be placed on the mesh — the caller decides how to recover (body stays in physics).
    private bool RejoinNavMesh(Vector3 desired, int mask)
    {
        rb.isKinematic = true;
        rb.useGravity  = false;
        if (!agent.enabled) agent.enabled = true;

        Vector3 point = desired;
        if (NavMesh.SamplePosition(desired, out var hit, sampleRadius * 2f, mask))
            point = hit.position;

        if (!agent.Warp(point) || !agent.isOnNavMesh) return false;
        agent.ResetPath();
        return true;
    }

    // Called by a MoriMochiContainer when a creature lands in a pen with room. Cuts the ragdoll,
    // snaps onto the breeding-area floor at the pen center, and restricts the areaMask so from now
    // on it can only walk inside breeding floor (released when the player grabs it). Returns false
    // (without confining) if the pen floor isn't on the breeding NavMesh — so the pen doesn't
    // register an occupant it never actually caught, and we never call ResetPath off-mesh.
    public bool EnterConfinement(MoriMochiContainer pen)
    {
        agent.areaMask = confinedAreaMask;

        // Warp/ResetPath throw on an agent not placed on a NavMesh — bail (back to physics) if the
        // pen floor isn't painted+baked as the breeding area, or the area name doesn't match.
        if (!RejoinNavMesh(pen.Center, confinedAreaMask))
        {
            Debug.LogWarning($"[MoriMochiAgent] '{name}' couldn't enter the pen — is its floor painted '{breedingAreaName}' and baked?");
            DetachToPhysics();
            rb.useGravity  = true;
            agent.areaMask = freeAreaMask;
            return false;
        }

        currentContainer = pen;
        holdAnchor       = null;
        EnterRoaming();
        return true;
    }

    // After a throw settles: snap back onto the NavMesh but DON'T steer yet. It
    // stays where it landed (still tumbled) and enters the get-up beat — TickRecovering
    // animates it upright before the agent brain takes over.
    private void BeginGetUp()
    {
        if (!RejoinNavMesh(transform.position, agent.areaMask))
        {
            state = AgentState.Thrown;      // couldn't rejoin (landed far off-mesh) — stay down, retry
            return;
        }

        agent.updateRotation = false;       // we hand-animate the get-up; the agent must not fight it

        // Personality sets the pace (lazy = groggy/slow, skittish = springs up), plus a
        // little per-throw jitter so a cluster of the same archetype doesn't rise in sync.
        float scale  = Mathf.Max(0.1f, profile.RecoverySpeed) * Random.Range(1f - getUpJitter, 1f + getUpJitter);
        effDownedDelay   = downedDelay   / scale;
        effGetUpDuration = getUpDuration / scale;

        // Target pose: keep its current heading (yaw), level out the tumble.
        Vector3 fwd = transform.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
        getUpFrom    = transform.rotation;
        getUpTo      = Quaternion.LookRotation(fwd.normalized, Vector3.up);
        recoverTimer = 0f;
        state        = AgentState.Recovering;

        onLand?.Invoke();
    }

    // Dazed-then-stand-up beat after landing. Holds still for downedDelay, then
    // slerps from the tumbled pose to upright over getUpDuration, then resumes.
    private void TickRecovering()
    {
        recoverTimer += Time.deltaTime;

        float t = effGetUpDuration <= 0f
            ? 1f
            : Mathf.InverseLerp(effDownedDelay, effDownedDelay + effGetUpDuration, recoverTimer);
        transform.rotation = Quaternion.Slerp(getUpFrom, getUpTo, Mathf.SmoothStep(0f, 1f, t));

        if (recoverTimer >= effDownedDelay + effGetUpDuration)
        {
            onGetUp?.Invoke();
            EnterRoaming();                 // restores agent.updateRotation
        }
    }

    // ── Helpers ───────────────────────────────────────────────────

    private void SetDestinationSafe(Vector3 desired)
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;
        if (NavMesh.SamplePosition(desired, out var hit, sampleRadius, agent.areaMask))
            agent.SetDestination(hit.position);
    }

    // A random point on a bounds' floor plane (y at center) — the confined roam target source.
    private static Vector3 RandomPointInBounds(Bounds b) => new Vector3(
        Random.Range(b.min.x, b.max.x), b.center.y, Random.Range(b.min.z, b.max.z));

    // Feeds the breedingAreaName dropdown with the project's real NavMesh Area names. Body is
    // editor-only, but the method itself stays compiled so nameof(...) resolves in builds.
    private static IEnumerable<string> EditorNavMeshAreaNames()
    {
#if UNITY_EDITOR
        return NavMesh.GetAreaNames();
#else
        return System.Array.Empty<string>();
#endif
    }

    // Test/debug: paint the body its personality color via a property block (no per-instance
    // material clone, so no leak). NameTag and other child renderers are untouched.
    private void ApplyTint(Color c)
    {
        if (bodyRenderer == null) return;
        var mpb = new MaterialPropertyBlock();
        bodyRenderer.GetPropertyBlock(mpb);
        mpb.SetColor(BaseColorId, c);
        mpb.SetColor(ColorId, c);
        bodyRenderer.SetPropertyBlock(mpb);
    }

    private float PlanarDistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        Vector3 d = player.position - transform.position; d.y = 0f;
        return d.magnitude;
    }

    // ── Gizmos (action ranges) ────────────────────────────────────
    // Ranges come from the resolved profile, which only exists once Initialize()
    // runs — so these draw in PLAY mode when the cube is selected, not in edit mode.

    private void OnDrawGizmos()
    {
        if (profile == null) return;     // not initialized yet (edit mode / pre-spawn)
        Vector3 c = transform.position;

        Gizmos.color = new Color(1f, 0.9f, 0.2f);   // player-detection
        Gizmos.DrawWireSphere(c, profile.ProximityRadius);
        Gizmos.color = new Color(0.3f, 0.8f, 1f);   // roam radius
        Gizmos.DrawWireSphere(c, profile.RoamRadius);
        if (profile.Reaction != ProximityReaction.Ignore)
        {
            Gizmos.color = new Color(0.4f, 1f, 0.5f);   // follow/stop distance
            Gizmos.DrawWireSphere(c, profile.FollowDistance);
        }

        Gizmos.color = profile.Tint;                // personality color tag
        Gizmos.DrawSphere(c + Vector3.up * 1.2f, 0.12f);

        if (agent != null && agent.enabled && agent.isOnNavMesh && agent.hasPath)
        {
            Gizmos.color = new Color(1f, 0.4f, 0.85f);
            Gizmos.DrawLine(c, agent.destination);  // current target
        }
    }
}
