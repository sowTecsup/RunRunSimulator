using UnityEngine;
using UnityEngine.AI;

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
    private enum AgentState { Idle, Roaming, Reacting, Held }

    [Header("Hold feel (while carried)")]
    [Tooltip("How snappily the body chases the hold anchor while carried.")]
    [SerializeField] private float followSpeed = 15f;
    [Tooltip("Below this speed (and grounded) after a throw, the agent re-joins the NavMesh.")]
    [SerializeField] private float settleSpeed = 0.15f;
    [SerializeField] private float settleDelay = 0.4f;

    [Header("NavMesh sampling")]
    [Tooltip("Max distance to snap a desired point onto the NavMesh.")]
    [SerializeField] private float sampleRadius = 4f;
    [Tooltip("How often (s) Reacting/Roaming recomputes its destination.")]
    [SerializeField] private float repathInterval = 0.35f;

    // ── Injected ──────────────────────────────────────────────────
    private CreatureDNA        dna;
    private PersonalityProfile profile;
    private Transform          player;
    private NameTag            nameTag;

    // ── Components / state ────────────────────────────────────────
    private NavMeshAgent agent;
    private Rigidbody    rb;
    private AgentState   state = AgentState.Idle;

    private Transform holdAnchor;     // non-null only while carried
    private bool      heldByPlayer;
    private float     settleTimer;
    private float     idleTimer;
    private float     idleDuration;
    private float     repathTimer;
    private AgentState stateBeforeReact = AgentState.Roaming;

    public bool IsHeld => heldByPlayer;
    public CreatureDNA DNA => dna;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        rb    = GetComponent<Rigidbody>();
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

        agent.speed    = profile.MoveSpeed;
        agent.areaMask = profile.ConfineToArea ? AreaMaskFor(profile.PreferredArea) : NavMesh.AllAreas;

        EnterRoaming();
    }

    private void Update()
    {
        switch (state)
        {
            case AgentState.Held:     TickHeld();     break;
            case AgentState.Idle:     TickIdle();     break;
            case AgentState.Roaming:  TickRoaming();  break;
            case AgentState.Reacting: TickReacting(); break;
        }
    }

    private void FixedUpdate()
    {
        // Carried: chase the anchor by velocity so it stays a solid physics body.
        if (heldByPlayer && holdAnchor != null)
            rb.linearVelocity = (holdAnchor.position - rb.position) * followSpeed;
    }

    // ── States ────────────────────────────────────────────────────

    private void TickIdle()
    {
        if (ReactIfPlayerNear()) return;

        idleTimer += Time.deltaTime;
        if (idleTimer >= idleDuration) EnterRoaming();
    }

    private void TickRoaming()
    {
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

        switch (profile.Reaction)
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

    private void TickHeld()
    {
        if (heldByPlayer) return;   // still in hand — wait for release/throw

        // Thrown/dropped: once it stops moving on the ground, re-join the NavMesh.
        if (rb.linearVelocity.sqrMagnitude < settleSpeed * settleSpeed)
        {
            settleTimer += Time.deltaTime;
            if (settleTimer >= settleDelay) Recover();
        }
        else settleTimer = 0f;
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
        state = AgentState.Roaming;
        Vector3 random = transform.position + Random.insideUnitSphere * profile.RoamRadius;
        SetDestinationSafe(random);
    }

    // Enters the reaction state if the player is within range and the personality
    // cares. Returns true if it took over this frame.
    private bool ReactIfPlayerNear()
    {
        if (profile.Reaction == ProximityReaction.Ignore || player == null) return false;
        if (PlanarDistanceToPlayer() > profile.ProximityRadius) return false;

        stateBeforeReact = state == AgentState.Idle ? AgentState.Idle : AgentState.Roaming;
        state            = AgentState.Reacting;
        repathTimer      = 0f;
        return true;
    }

    // ── IThrowable (physics handoff) ──────────────────────────────

    public void OnGrab(Transform anchor)
    {
        holdAnchor   = anchor;
        heldByPlayer = true;
        state        = AgentState.Held;
        settleTimer  = 0f;

        if (agent.enabled) agent.enabled = false;   // stop steering
        rb.isKinematic     = false;
        rb.useGravity      = false;                 // floats to the hand while held
        rb.angularVelocity = Vector3.zero;
    }

    public void OnRelease()
    {
        holdAnchor    = null;
        heldByPlayer  = false;
        rb.useGravity = true;                       // physics owns it until it settles
        settleTimer   = 0f;
        // stays in Held state → TickHeld watches for it to settle, then Recover()
    }

    public void OnThrow(Vector3 force)
    {
        OnRelease();
        rb.AddForce(force, ForceMode.Impulse);
    }

    // Snaps back onto the NavMesh after being thrown and re-enables steering.
    private void Recover()
    {
        Vector3 point = transform.position;
        if (NavMesh.SamplePosition(point, out var hit, sampleRadius * 2f, agent.areaMask))
            point = hit.position;

        rb.isKinematic = true;
        rb.useGravity  = false;

        agent.enabled = true;
        if (agent.isOnNavMesh || agent.Warp(point))
            EnterRoaming();
        else
            // Couldn't rejoin (landed far off-mesh) — try again shortly.
            state = AgentState.Held;
    }

    // ── Helpers ───────────────────────────────────────────────────

    private void SetDestinationSafe(Vector3 desired)
    {
        if (!agent.enabled || !agent.isOnNavMesh) return;
        if (NavMesh.SamplePosition(desired, out var hit, sampleRadius, agent.areaMask))
            agent.SetDestination(hit.position);
    }

    private float PlanarDistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        Vector3 d = player.position - transform.position; d.y = 0f;
        return d.magnitude;
    }

    // Bitmask for one WorldArea, by NavMesh Area name. Falls back to all areas if
    // the area isn't configured in the Navigation window (logs once).
    private int AreaMaskFor(WorldArea area)
    {
        int idx = NavMesh.GetAreaFromName(area.ToString());
        if (idx < 0)
        {
            Debug.LogWarning($"[MoriMochiAgent] NavMesh Area '{area}' not found — create it in Navigation > Areas. Falling back to all areas.");
            return NavMesh.AllAreas;
        }
        return 1 << idx;
    }
}
