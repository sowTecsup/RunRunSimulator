using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
namespace MoriMonchiSimulator
{

// The runtime "brain" of one MoriMochi cube in the world. Drives a NavMeshAgent
// through a small behavior state machine biased by the creature's Role
// (via RoleWorldProfileSO), and hands off cleanly to physics when the player
// grabs/throws it (IThrowable) — NavMeshAgent ⇄ Rigidbody, the one real technical
// tension here.
//
// Behavior is data-driven: this script never switches on Role directly, it
// reads the resolved RoleWorldProfile. The cube is spawned and wired by
// MoriMochiSpawner, which calls Initialize().
//
// Components: NavMeshAgent drives movement while the Rigidbody stays kinematic.
// On grab the agent is disabled and the Rigidbody goes dynamic (gravity/throw);
// once it settles we sample the nearest NavMesh point, Warp the agent back and
// resume. RequireComponent guarantees both exist.
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody))]
public partial class MoriMochiAgent : MonoBehaviour, IThrowable, IInteractable
{
    // Carried = in the player's hand; Thrown = ragdoll in flight after a release/throw/knock.
    // SeekingNeed = pathing to a NeedStation; UsingStation = stopped, consuming it.
    private enum AgentState { Idle, Roaming, Reacting, Carried, Thrown, Recovering, SeekingNeed, UsingStation, Courting }
    private enum CourtRole { Tend, Orbit }

    // ── Injected ──────────────────────────────────────────────────
    private CreatureDNA    dna;
    private RoleWorldProfile profile;
    private Transform       player;
   

    // ── Components / state ────────────────────────────────────────
    private NavMeshAgent agent;
    private Rigidbody    rb;
    private Collider     col;
    private AgentState   state = AgentState.Idle;
    private float        baseSpeed;

    private Transform holdAnchor;     // non-null only while carried (state == Carried)
    private float     settleTimer;
    private float     thrownTimer;    // time since the last release/throw (drives the safety timeout)
    private Vector3   lastVelocity;   // velocity captured each FixedUpdate while airborne, reflected on impact
    private int       bounceCount;    // reflections used this flight
    private float     idleTimer;
    private float     idleDuration;
    private float     repathTimer;
    private float     offMeshGrace;       // seconds spent stuck kinematic + off-mesh (cold-start recovery)
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

    private MoriMochiAgent courtPartner;
    private Vector3        courtAnchor;
    private float          courtAngle;
    private float          courtRepathTimer;
    private CourtRole      courtRole;

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
        baseSpeed = agent.speed;

        rb.isKinematic = true;          // NavMeshAgent drives until we get thrown
        rb.useGravity  = false;

        // NavMesh-driven by default → the collider is a trigger so the kinematic body
        // doesn't push/collide while roaming (cheaper, no contact solving). It flips to a
        // solid collider only while physics owns it (carried/thrown), so bounces register.
        SetColliderTrigger(true);
    }

    // Wired by the spawner. profileTable + player may be resolved here as a fallback.
    public void Initialize(CreatureDNA creature, RoleWorldProfileSO profileTable, Transform playerTransform)
    {
        dna     = creature;
        profile = profileTable?.GetProfile(creature.Role)
                  ?? RoleWorldProfile.Neutral();
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

    public void Rebind(CreatureDNA creature, RoleWorldProfileSO profileTable)
    {
        dna     = creature;
        profile = profileTable?.GetProfile(creature.Role) ?? RoleWorldProfile.Neutral();
        if (nameTag != null) nameTag.Bind(creature, this);
    }

    private void Update()
    {
        DevTrackState();
        if (forceRagdoll && IsNavMeshControlled()) { ReleaseStation(); EnterRagdoll(); }
        RecoverIfStuckOffMesh();

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

    // isStopped throws if the agent isn't on a NavMesh — guard it.
    private void SetStopped(bool stopped)
    {
        if (agent.enabled && agent.isOnNavMesh) agent.isStopped = stopped;
    }

    // Trigger while NavMesh-driven, solid while physics-driven. Null-guarded — Collider
    // isn't RequireComponent'd, so a prefab without one degrades gracefully.
    private void SetColliderTrigger(bool isTrigger)
    {
        if (col != null) col.isTrigger = isTrigger;
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

        Gizmos.color = profile.Tint;                // role color tag
        Gizmos.DrawSphere(c + Vector3.up * 1.2f, 0.12f);

        if (agent != null && agent.enabled && agent.isOnNavMesh && agent.hasPath)
        {
            Gizmos.color = new Color(1f, 0.4f, 0.85f);
            Gizmos.DrawLine(c, agent.destination);  // current target
        }
    }
}
}
