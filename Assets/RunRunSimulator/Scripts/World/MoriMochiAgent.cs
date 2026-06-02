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
    private enum AgentState { Idle, Roaming, Reacting, Held, Recovering }

    [Header("Visuals")]
    [Tooltip("Renderer to tint by personality. The root has no mesh — this is the renderer under the 'Model' child. Auto-found if left empty.")]
    [SerializeField] private Renderer bodyRenderer;

    [Header("Hold feel (while carried)")]
    [Tooltip("How snappily the body chases the hold anchor while carried.")]
    [SerializeField] private float followSpeed = 15f;
    [Tooltip("Below this speed (and grounded) after a throw, the agent re-joins the NavMesh.")]
    [SerializeField] private float settleSpeed = 0.15f;
    [SerializeField] private float settleDelay = 0.4f;

    [Header("Throw physics (after release)")]
    [Tooltip("Linear drag applied while airborne/sliding after a throw, so it slows to a stop instead of gliding forever.")]
    [SerializeField] private float thrownLinearDamping = 1.2f;
    [Tooltip("Angular drag applied after a throw so it stops spinning.")]
    [SerializeField] private float thrownAngularDamping = 2f;
    [Tooltip("Extra ray length below the body used to confirm it's resting on the floor before it settles.")]
    [SerializeField] private float groundCheckDistance = 0.2f;
    [Tooltip("Safety net: it recovers no matter what this many seconds after a throw, even if still sliding.")]
    [SerializeField] private float maxThrownTime = 6f;

    [Header("Bounce (plushie throw)")]
    [Tooltip("Fraction of speed kept on each bounce (0 = dead drop, 1 = no energy loss). Lower settles sooner.")]
    [Range(0f, 1f)]
    [SerializeField] private float bounciness = 0.55f;
    [Tooltip("How many reflections it gets before it's allowed to stop bouncing and settle.")]
    [SerializeField] private int maxBounces = 3;
    [Tooltip("Impacts slower than this don't count as a bounce — it just settles (avoids endless micro-bounces).")]
    [SerializeField] private float minBounceSpeed = 1.5f;
    [Tooltip("Random tumble added on each bounce so it reads as a soft plushie, not a billiard ball. 0 = none.")]
    [SerializeField] private float bounceSpin = 4f;

    [Header("Knock (throwable vs throwable)")]
    [Tooltip("Fraction of impact speed transferred to a creature you slam into. ~1 = full, higher = explosive ragdoll.")]
    [SerializeField] private float knockTransfer = 1.2f;
    [Tooltip("Upward pop blended into the knock so the creature you hit launches a bit instead of just sliding.")]
    [Range(0f, 1f)]
    [SerializeField] private float knockUpBias = 0.35f;

    [Header("Recovery (after being thrown)")]
    [Tooltip("Seconds it stays down/dazed where it landed before standing up. Scaled per-personality by RecoverySpeed.")]
    [SerializeField] private float downedDelay = 0.6f;
    [Tooltip("How long the get-up takes — it rotates from its tumbled pose back upright before the agent resumes. Scaled per-personality by RecoverySpeed.")]
    [SerializeField] private float getUpDuration = 0.5f;
    [Tooltip("Random ±jitter (fraction) on the get-up timing so even same-personality creatures don't rise in lockstep.")]
    [Range(0f, 0.5f)]
    [SerializeField] private float getUpJitter = 0.15f;

    [Header("NavMesh sampling")]
    [Tooltip("Max distance to snap a desired point onto the NavMesh.")]
    [SerializeField] private float sampleRadius = 4f;
    [Tooltip("How often (s) Reacting/Roaming recomputes its destination.")]
    [SerializeField] private float repathInterval = 0.35f;

    // ── Feedbacks (Feel-ready) ────────────────────────────────────
    // Juice hook points. They fire UnityEvents now (compiles without Feel installed).
    // When Feel lands: drop an MMF_Player on the prefab and wire its PlayFeedbacks()
    // into the matching event in the inspector — zero code coupling. These are the
    // template every future "has visual juice" script should follow.
    [Header("Feedbacks (Feel-ready — wire MMF_Player.PlayFeedbacks here)")]
    [SerializeField] private UnityEvent onGrab;     // player picked it up
    [SerializeField] private UnityEvent onThrow;    // player threw it
    [SerializeField] private UnityEvent onBounce;   // each reflection off a surface mid-flight
    [SerializeField] private UnityEvent onLand;     // settled on the ground (before the get-up beat)
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

    private Transform holdAnchor;     // non-null only while carried
    private bool      heldByPlayer;
    private float     settleTimer;
    private float     thrownTimer;    // time since the last release/throw (drives the safety timeout)
    private Vector3   lastVelocity;   // velocity captured each FixedUpdate while airborne, reflected on impact
    private int       bounceCount;    // reflections used this flight
    private float     idleTimer;
    private float     idleDuration;
    private float     repathTimer;
    private AgentState stateBeforeReact = AgentState.Roaming;

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

    public bool IsHeld => heldByPlayer;
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

        agent.speed    = profile.MoveSpeed;
        agent.areaMask = NavMesh.AllAreas;   // free movement — personality is a preference, never a fence

        ApplyTint(profile.Tint);

        EnterRoaming();
    }

    private void Update()
    {
        switch (state)
        {
            case AgentState.Held:       TickHeld();       break;
            case AgentState.Idle:       TickIdle();       break;
            case AgentState.Roaming:    TickRoaming();    break;
            case AgentState.Reacting:   TickReacting();   break;
            case AgentState.Recovering: TickRecovering(); break;
        }
    }

    private void FixedUpdate()
    {
        // Carried: chase the anchor by velocity so it stays a solid physics body.
        if (heldByPlayer && holdAnchor != null)
            rb.linearVelocity = (holdAnchor.position - rb.position) * followSpeed;
        // In flight after a throw: remember the pre-impact velocity so OnCollisionEnter
        // can reflect it (rb.velocity there is already mangled by the contact response).
        else if (state == AgentState.Held && !rb.isKinematic)
            lastVelocity = rb.linearVelocity;
    }

    // While flying after a throw: reflect off surfaces (plushie bounce) and slam any
    // OTHER throwable it hits into a flying ragdoll too (chain reaction). Normal
    // gameplay collisions (kinematic roaming, held) are ignored.
    private void OnCollisionEnter(Collision collision)
    {
        if (state != AgentState.Held || heldByPlayer || rb.isKinematic) return;

        float impact = lastVelocity.magnitude;
        if (impact < minBounceSpeed) return;

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
        if (heldByPlayer) return;   // in the player's hand — don't yank it out

        if (rb.isKinematic)
        {
            if (agent.enabled) agent.enabled = false;
            rb.isKinematic = false;
        }
        rb.useGravity     = true;
        rb.linearDamping  = thrownLinearDamping;
        rb.angularDamping = thrownAngularDamping;
        holdAnchor        = null;
        state             = AgentState.Held;
        settleTimer       = 0f;
        thrownTimer       = 0f;
        bounceCount       = 0;

        rb.AddForce(force, ForceMode.Impulse);
        onThrow?.Invoke();          // reuse the throw juice for the knock
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
        state = AgentState.Roaming;
        agent.updateRotation = true;        // hand rotation back to the agent (Recovering turns it off)

        // Most of the time wander nearby; with AreaPreference odds, head toward the
        // preferred area instead — a soft pull home, not a fence.
        Vector3 dest = (Random.value < profile.AreaPreference && TryGetPreferredPoint(out var pref))
            ? pref
            : transform.position + Random.insideUnitSphere * profile.RoamRadius;
        SetDestinationSafe(dest);
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
        rb.isKinematic       = false;
        rb.useGravity        = false;               // floats to the hand while held
        rb.angularVelocity   = Vector3.zero;
        rb.linearDamping     = 0f;                  // crisp follow while carried
        rb.angularDamping    = 0.05f;

        onGrab?.Invoke();
    }

    public void OnRelease()
    {
        holdAnchor        = null;
        heldByPlayer      = false;
        rb.useGravity     = true;                   // physics owns it until it settles
        rb.linearDamping  = thrownLinearDamping;    // bleed off momentum so it can't glide forever
        rb.angularDamping = thrownAngularDamping;
        settleTimer       = 0f;
        thrownTimer       = 0f;
        bounceCount       = 0;
        // stays in Held state → TickHeld watches for it to settle, then BeginGetUp()
    }

    public void OnThrow(Vector3 force)
    {
        OnRelease();
        rb.AddForce(force, ForceMode.Impulse);
        onThrow?.Invoke();
    }

    // After a throw settles: snap back onto the NavMesh but DON'T steer yet. It
    // stays where it landed (still tumbled) and enters the get-up beat — TickRecovering
    // animates it upright before the agent brain takes over.
    private void BeginGetUp()
    {
        Vector3 point = transform.position;
        if (NavMesh.SamplePosition(point, out var hit, sampleRadius * 2f, agent.areaMask))
            point = hit.position;

        rb.isKinematic = true;
        rb.useGravity  = false;

        agent.enabled = true;
        if (!(agent.isOnNavMesh || agent.Warp(point)))
        {
            // Couldn't rejoin (landed far off-mesh) — stay down and retry shortly.
            state = AgentState.Held;
            return;
        }

        agent.ResetPath();
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
