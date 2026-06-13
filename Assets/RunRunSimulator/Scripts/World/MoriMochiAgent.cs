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
public class MoriMochiAgent : MonoBehaviour, IThrowable, IInteractable
{
    // Carried = in the player's hand; Thrown = ragdoll in flight after a release/throw/knock.
    // SeekingNeed = pathing to a NeedStation; UsingStation = stopped, consuming it.
    private enum AgentState { Idle, Roaming, Reacting, Carried, Thrown, Recovering, SeekingNeed, UsingStation, Courting }

    // ── Tuning (Odin tabs) ────────────────────────────────────────
    // Grouped to mirror the two concerns this component juggles — the NavMesh "brain"
    // (Movement) and the physics/throwable layer (Physics) — plus Presentation.

    [TabGroup("Tuning", "References"), Title("References")]
    [SerializeField] private NameTag nameTag;
    // ── Movement (NavMesh brain) ──
    [TabGroup("Tuning", "Movement"), Title("NavMesh sampling")]
    [Tooltip("Max distance to snap a desired point onto the NavMesh.")]
    [SerializeField] private float sampleRadius = 4f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("How often (s) Reacting/Roaming recomputes its destination.")]
    [SerializeField] private float repathInterval = 0.35f;

    [TabGroup("Tuning", "Movement"), Title("Player proximity")]
    [Tooltip("Max seconds a friendly reaction (Approach/Follow/Retreat) lasts before the creature resumes its own business.")]
    [SerializeField, Min(1f)] private float followDuration = 10f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("Seconds the creature waits before reacting to the player again after a reaction ends (timer or petting).")]
    [SerializeField, Min(0f)] private float reactCooldown = 15f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("Max distance (m) at which the pet hint appears and petting is valid.")]
    [SerializeField, Min(0.5f)] private float petRadius = 2.5f;
    [TabGroup("Tuning", "Movement")]
    [Tooltip("Half-angle of the camera cone (degrees) within which the player must be aiming at this creature for the pet hint to appear. 20° = ±20° from dead-center.")]
    [SerializeField, Range(5f, 60f)] private float petLookAngle = 20f;

    [TabGroup("Tuning", "Movement"), Title("Personality radii (runtime — from PersonalityProfileSO)")]
    [ShowInInspector, ReadOnly] private float ProfileProximityRadius => profile?.ProximityRadius ?? 0f;
    [TabGroup("Tuning", "Movement")]
    [ShowInInspector, ReadOnly] private float ProfileRoamRadius      => profile?.RoamRadius      ?? 0f;
    [TabGroup("Tuning", "Movement")]
    [ShowInInspector, ReadOnly] private float ProfileFollowDistance  => profile?.FollowDistance  ?? 0f;

    [TabGroup("Tuning", "Movement"), Title("Breeding pen confinement")]
    [Tooltip("NavMesh Area that breeding pens paint their floor with. Free agents EXCLUDE it (so they route around every pen); a penned creature is RESTRICTED to it. Pick the exact Area from Navigation → Areas.")]
    [ValueDropdown(nameof(EditorNavMeshAreaNames))]
    [SerializeField] private string breedingAreaName = "BreedingRoom";

    // ── Needs (decay + thresholds) ──
    // Live readout of this creature's current needs (the values in dna.Needs, mutated each frame).
    // Editor-only window into runtime state — drives nothing.
    [TabGroup("Tuning", "Needs"), Title("Live values (play mode)")]
    [ShowInInspector, ProgressBar(0f, 100f, 0.3f, 0.9f, 0.4f)]
    private float Health => dna != null ? dna.Needs.Health : 0f;
    [TabGroup("Tuning", "Needs")]
    [ShowInInspector, ProgressBar(0f, 100f, 0.3f, 0.6f, 1f)]
    private float Energy => dna != null ? dna.Needs.Energy : 0f;
    [TabGroup("Tuning", "Needs")]
    [ShowInInspector, ProgressBar(-100f, 100f, 1f, 0.5f, 0.7f)]
    private float Affect => dna != null ? dna.Needs.Affect : 0f;

    // Overall wellbeing, DERIVED from the needs against the critical thresholds below (never stored —
    // always in sync). Sick = Health critical (survival emergency); InNeed = Energy/Affect critical;
    // Healthy = none. Gates whether it can afford to react to the player (see ReactIfPlayerNear).
    [TabGroup("Tuning", "Needs"), ShowInInspector, EnumToggleButtons, ReadOnly]
    public CreatureCondition Condition
    {
        get
        {
            if (dna == null) return CreatureCondition.Healthy;
            if (dna.Needs.Health <= criticalHealth) return CreatureCondition.Sick;
            if (dna.Needs.Energy <= criticalEnergy || dna.Needs.Affect <= criticalAffect) return CreatureCondition.InNeed;
            return CreatureCondition.Healthy;
        }
    }

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
    [Tooltip("Energy at/below this → seek a RestZone.")]
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

    [TabGroup("Tuning", "Needs"), Title("Player interaction")]
    [Tooltip("Affect boost granted when the player pets this creature from the front.")]
    [SerializeField, Min(0f)] private float affectOnPet = 20f;

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
    [TabGroup("Tuning", "Presentation")]
    [SerializeField] private UnityEvent onPet;      // player petted it from the front

    [TabGroup("Tuning", "Dev"), Title("Dev Tools (Play mode only)")]
    [Button("Force Ragdoll")]
    private void DevForceRagdoll()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[MoriMochiAgent] Enter Play mode first."); return; }
        ReleaseStation();
        EnterRagdoll();
        rb.AddForce(Vector3.up * 4f, ForceMode.VelocityChange);
    }

    [TabGroup("Tuning", "Dev")]
    [Button("Force Roam")]
    private void DevForceRoam()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[MoriMochiAgent] Enter Play mode first."); return; }
        EnterRoaming();
    }

    // ── Injected ──────────────────────────────────────────────────
    private CreatureDNA        dna;
    private PersonalityProfile profile;
    private Transform          player;
   

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
    private float     reactingTimer;      // seconds spent in the current friendly reaction; exits when ≥ followDuration
    private float     reactCooldownTimer; // counts down after a reaction ends; creature ignores the player while > 0
    private float     pettingDisplayTimer; // drives the "Petting…" label on the NameTag; decays to 0 on its own
    private AgentState stateBeforeReact = AgentState.Roaming;

    // True while this creature is dropped to physics (ragdoll) through a furniture-driven NavMesh
    // rebake. WillRebake hands it to physics so the bake can't snap it; Rebaked clears this and
    // TickThrown resumes — it settles + gets up onto the fresh mesh, staggered by personality.
    private bool rebakeInProgress;

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
    private Vector3    getUpFromPos;
    private Vector3    getUpToPos;

    public bool IsHeld => state == AgentState.Carried;
    // True only while ragdolling after a throw — not while carried, not while NavMesh-driven.
    // A container admits only creatures for which this is true (thrown in), never walk-ins.
    public bool IsAirborne => state == AgentState.Thrown;
    public CreatureDNA DNA => dna;

    // True while this creature is confined to a pen (breeding/store container). The NameTag
    // reads it to swap to the pen layout (gender + name + personality, plus heart/timer if breeding).
    public bool IsPenned => currentContainer != null;
    public bool IsCourting => state == AgentState.Courting;

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

    // ── Lifecycle ─────────────────────────────────────────────────

    // A furniture rebake snaps every active agent — we react to bracket events so this one
    // detaches before the bake and re-anchors after. Subscribe/unsubscribe with activation so a
    // pooled (inactive) instance never reacts (it's off the mesh anyway).
    private void OnEnable()
    {
        GameEvents.OnNavMeshWillRebake += OnNavMeshWillRebake;
        GameEvents.OnNavMeshRebaked    += OnNavMeshRebaked;
    }

    private void OnDisable()
    {
        GameEvents.OnNavMeshWillRebake -= OnNavMeshWillRebake;
        GameEvents.OnNavMeshRebaked    -= OnNavMeshRebaked;
    }

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb    = GetComponent<Rigidbody>();
        col   = GetComponent<Collider>();

        rb.isKinematic = true;          // NavMeshAgent drives until we get thrown
        rb.useGravity  = false;

        // NavMesh-driven by default → the collider is a trigger so the kinematic body
        // doesn't push/collide while roaming (cheaper, no contact solving). It flips to a
        // solid collider only while physics owns it (carried/thrown), so bounces register.
        SetColliderTrigger(true);
    }

    // Wired by the spawner. profileTable + player may be resolved here as a fallback.
    public void Initialize(CreatureDNA creature, PersonalityProfileSO profileTable, Transform playerTransform)
    {
        dna     = creature;
        profile = (profileTable != null ? profileTable : PersonalityProfileSO.Current)?.GetProfile(creature.Personality)
                  ?? PersonalityProfile.Neutral();
        player  = playerTransform;

        RestoreNavMeshControl();   // pooling: an instance reused from the pool keeps last life's state — reset it

        if (nameTag != null) nameTag.Bind(creature, this);

       // agent.speed = profile.MoveSpeed;(la velocidad del morimonchi no debe depender de su adn)

        // Penned creatures are gated to the BreedingRoom area; free ones get everything EXCEPT it,
        // so they route around every pen (cost wouldn't fence — only the mask does). If the Area
        // isn't set up yet (-1), fall back to AllAreas so behavior degrades gracefully.
        int breeding     = NavMesh.GetAreaFromName(breedingAreaName);
        confinedAreaMask = breeding >= 0 ? 1 << breeding : NavMesh.AllAreas;
        freeAreaMask     = breeding >= 0 ? NavMesh.AllAreas & ~(1 << breeding) : NavMesh.AllAreas;
        agent.areaMask   = freeAreaMask;     // free movement — personality is a preference, never a fence

        EnterRoaming();
    }

    private void Update()
    {
        TickNeeds(Time.deltaTime);
        if (reactCooldownTimer  > 0f) reactCooldownTimer  -= Time.deltaTime;
        if (pettingDisplayTimer > 0f) pettingDisplayTimer -= Time.deltaTime;
        switch (state)
        {
            case AgentState.Idle:         TickIdle();         break;
            case AgentState.Roaming:      TickRoaming();      break;
            case AgentState.Reacting:     TickReacting();     break;
            case AgentState.Thrown:       TickThrown();       break;
            case AgentState.Recovering:   TickRecovering();   break;
            case AgentState.SeekingNeed:  TickSeekingNeed();  break;
            case AgentState.UsingStation: TickUsingStation(); break;
            case AgentState.Courting:     TickCourting();     break;
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

    private void OnTriggerEnter(Collider other)
    {
        if (state != AgentState.Thrown) return;

        float impact = lastVelocity.magnitude;
        if (impact < minBounceSpeed) return;

        var hit = other.GetComponentInParent<IThrowable>();
        if (hit == null || ReferenceEquals(hit, this)) return;

        Vector3 push = other.transform.position - transform.position; push.y = 0f;
        if (push.sqrMagnitude < 0.001f) push = transform.forward;
        push = (push.normalized + Vector3.up * knockUpBias).normalized;
        hit.Knock(push * impact * knockTransfer);

        if (impact >= hardImpactThreshold) dna?.Needs.AddAffect(-affectOnHardCollision);
    }

    // Knocked by another thrown object (IThrowable contract). If currently NavMesh-
    // controlled, hand off to physics like a throw; then apply the impulse so it
    // ragdolls away and can bounce / chain into others.
    public void Knock(Vector3 force)
    {
        if (state == AgentState.Carried) return;   // in the player's hand — don't yank it out
        if (currentContainer != null) return;      // penned: tackle-proof — only the player can take it out

        ReleaseStation();
        EnterRagdoll();

        dna?.Needs.AddAffect(-affectOnThrow);   // being slammed around is stressful
        rb.AddForce(force, ForceMode.Impulse);
        onThrow?.Invoke();
    }

    // Cannon spawn: disables the NavMeshAgent BEFORE teleporting to the muzzle so the agent never
    // fires OnEnable off-mesh (which would error, snap the transform to the floor, and fight
    // physics). Initialize() must have been called first on a valid NavMesh point — this just
    // handles the "pop out of the machine" movement. It then arcs as a ragdoll, lands, and gets up
    // onto the mesh via the normal throw pipeline (TickThrown → BeginGetUp).
    public void Launch(Vector3 launchPos, Vector3 launchVelocity)
    {
        DetachToPhysics();
        transform.position = launchPos;
        ApplyThrownPhysics();
        holdAnchor = null;
        state      = AgentState.Thrown;
        // VelocityChange (not Impulse) so the body gets EXACTLY the computed ballistic velocity
        // regardless of mass — the spawner solved this to land inside the cannon's radius.
        rb.AddForce(launchVelocity, ForceMode.VelocityChange);
    }

    private void EnterRagdoll()
    {
        DetachToPhysics();
        ApplyThrownPhysics();
        holdAnchor = null;
        state      = AgentState.Thrown;
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

    private void TickCourting() { }

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

    private void TickThrown()
    {
        // Held as a ragdoll through a navmesh rebake — don't settle/rejoin until the bake finishes
        // (Rebaked clears this), so we never try to anchor onto a mesh that's mid-rebuild.
        if (rebakeInProgress) return;

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
        EnterRagdoll();
    }

    public void OnThrow(Vector3 force)
    {
        EnterRagdoll();
        rb.AddForce(force, ForceMode.Impulse);
        dna?.Needs.AddAffect(-affectOnThrow);
        onThrow?.Invoke();
    }

    // ── Pooling (reuse) ───────────────────────────────────────────

    // Reset to a clean NavMesh-driven body. Awake runs once per instance, NOT on pool
    // reactivation, so a creature reused from the pool would otherwise resume last life's
    // state (mid-throw velocity, disabled agent, a still-reserved station). Called at the
    // top of Initialize; idempotent for fresh (non-pooled) instances.
    private void RestoreNavMeshControl()
    {
        ReleaseStation();
        if (currentContainer != null) { currentContainer.Release(this); currentContainer = null; }

        if (!rb.isKinematic)            // only clear on a dynamic body — setting velocity on a kinematic one warns
        {
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        rb.isKinematic = true;
        rb.useGravity  = false;

        if (!agent.enabled) agent.enabled = true;
        agent.updateRotation = true;
        SetColliderTrigger(true);

        holdAnchor       = null;
        settleTimer      = thrownTimer = idleTimer = recoverTimer = repathTimer
                         = reactingTimer = reactCooldownTimer = pettingDisplayTimer = 0f;
        bounceCount      = 0;
        rebakeInProgress = false;
        state            = AgentState.Idle;
    }

    // Called by the spawner before this instance goes back to the pool (deactivated): free any
    // held station/pen so it doesn't hog them while inert, and stop steering. Reuse re-inits via
    // Initialize → RestoreNavMeshControl.
    public void PrepareForPool()
    {
        ReleaseStation();
        if (currentContainer != null) { currentContainer.Release(this); currentContainer = null; }
        if (agent.enabled && agent.isOnNavMesh) agent.ResetPath();
    }

    // ── NavMesh rebake survival ───────────────────────────────────

    // A furniture-driven rebake is imminent. If we're walking the mesh, drop any reservation and
    // hand the body to PHYSICS (ragdoll). A dynamic Rigidbody isn't a NavMeshAgent, so the bake
    // can't teleport it across the room; it just rests where it stood. Physics-driven states
    // (carried / thrown / recovering) own their own re-entry — leave them.
    private void OnNavMeshWillRebake()
    {
        if (rebakeInProgress || !IsNavMeshControlled()) return;
        rebakeInProgress = true;
        ReleaseStation();
        DetachToPhysics();          // agent off, Rigidbody dynamic + collider solid
        ApplyThrownPhysics();
        holdAnchor = null;
        state      = AgentState.Thrown;   // gated in TickThrown until Rebaked clears the flag
    }

    // The fresh mesh is live. Release the ragdoll: TickThrown now settles it and BeginGetUp
    // re-anchors onto the new mesh — staggered per-personality, so the whole crowd doesn't snap
    // upright in the same frame (the old "tilting" pop). Off-mesh creatures just keep ragdolling
    // until they settle somewhere valid.
    private void OnNavMeshRebaked()
    {
        if (!rebakeInProgress) return;
        rebakeInProgress = false;
    }

    // NavMesh-driven states (vs. physics/animation-driven: carried, thrown, recovering).
    private bool IsNavMeshControlled() =>
        state == AgentState.Idle    || state == AgentState.Roaming     || state == AgentState.Reacting ||
        state == AgentState.SeekingNeed || state == AgentState.UsingStation || state == AgentState.Courting;

    // ── Physics handoff (NavMeshAgent ⇄ Rigidbody) ────────────────

    // Stop NavMesh steering and let physics own the body (carry/throw/knock). Idempotent — safe to
    // call when already detached. Callers set the gravity/damping for their specific case after.
    private void DetachToPhysics()
    {
        if (agent.enabled) agent.enabled = false;
        rb.isKinematic = false;
        SetColliderTrigger(false);      // physics owns it now → solid, so it collides/bounces
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
        SetColliderTrigger(true);       // back on the mesh → trigger again (caller kept it solid on failure)
        return true;
    }

    // Trigger while NavMesh-driven, solid while physics-driven. Null-guarded — Collider
    // isn't RequireComponent'd, so a prefab without one degrades gracefully.
    private void SetColliderTrigger(bool isTrigger)
    {
        if (col != null) col.isTrigger = isTrigger;
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

    public void EnterCourtship(Vector3 position, Vector3 lookAt)
    {
        ReleaseStation();

        if (agent.enabled && agent.isOnNavMesh)
        {
            Vector3 point = position;
            if (NavMesh.SamplePosition(position, out var hit, sampleRadius, agent.areaMask))
                point = hit.position;
            agent.Warp(point);
            agent.ResetPath();
        }

        agent.updateRotation = false;
        SetStopped(true);

        Vector3 dir = lookAt - position; dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);

        state = AgentState.Courting;
    }

    public void ExitCourtship()
    {
        if (state != AgentState.Courting) return;
        agent.updateRotation = true;
        EnterRoaming();
    }

    // After a throw settles: snap back onto the NavMesh but DON'T steer yet. It
    // stays where it landed (still tumbled) and enters the get-up beat — TickRecovering
    // animates it upright before the agent brain takes over.
    private void BeginGetUp()
    {
        if (!NavMesh.SamplePosition(transform.position, out var hit, sampleRadius * 2f, agent.areaMask))
        {
            state = AgentState.Thrown;
            return;
        }

        getUpFromPos = transform.position;
        getUpToPos   = hit.position;

        rb.isKinematic = true;
        rb.useGravity  = false;
        SetColliderTrigger(true);

        agent.updateRotation = false;

        float scale  = Mathf.Max(0.1f, profile.RecoverySpeed) * Random.Range(1f - getUpJitter, 1f + getUpJitter);
        effDownedDelay   = downedDelay   / scale;
        effGetUpDuration = getUpDuration / scale;

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
        float smoothT = Mathf.SmoothStep(0f, 1f, t);
        transform.position = Vector3.Lerp(getUpFromPos, getUpToPos, smoothT);
        transform.rotation = Quaternion.Slerp(getUpFrom, getUpTo, smoothT);

        if (recoverTimer >= effDownedDelay + effGetUpDuration)
        {
            if (!agent.enabled) agent.enabled = true;
            agent.Warp(getUpToPos);
            agent.ResetPath();
            onGetUp?.Invoke();
            EnterRoaming();
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

    private float PlanarDistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        Vector3 d = player.position - transform.position; d.y = 0f;
        return d.magnitude;
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
        Gizmos.color = Color.purple;   // pet radius
        Gizmos.DrawWireSphere(c, petRadius);
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
