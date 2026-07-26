using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

internal class AgentPhysics
{
    private readonly MoriMochiAgent owner;
    private readonly AgentContext   ctx;

    private float     settleTimer;
    private float     thrownTimer;    // time since the last release/throw (drives the safety timeout)
    private Vector3   lastVelocity;   // velocity captured each FixedUpdate while airborne, reflected on impact
    private int       bounceCount;    // reflections used this flight
    private float     offMeshGrace;   // seconds spent stuck kinematic + off-mesh (cold-start recovery)

    // Get-up animation (Recovering): lerp from the tumbled pose back to upright.
    // Effective timings are per-throw (personality scale + jitter), cached at BeginGetUp.
    private float      recoverTimer;
    private float      effDownedDelay;
    private float      effGetUpDuration;
    private Quaternion getUpFrom;
    private Quaternion getUpTo;
    private Vector3    getUpFromPos;
    private Vector3    getUpToPos;

    private Vector3 lastNavAnchor;
    private bool    hasNavAnchor;
    private int     voidRescues;

    internal AgentPhysics(MoriMochiAgent owner, AgentContext ctx)
    {
        this.owner = owner;
        this.ctx   = ctx;
    }

    internal void CaptureNavAnchor(Vector3 pos)
    {
        lastNavAnchor = pos;
        hasNavAnchor  = true;
    }

    internal void FixedTick()
    {
        // Carried: chase the anchor by velocity so it stays a solid physics body.
        if (ctx.State == AgentState.Carried && ctx.HoldAnchor != null)
            ctx.Rb.linearVelocity = (ctx.HoldAnchor.position - ctx.Rb.position) * owner.followSpeed;
        // In flight after a throw: remember the pre-impact velocity so OnCollisionEnter
        // can reflect it (rb.velocity there is already mangled by the contact response).
        else if (ctx.State == AgentState.Thrown)
            lastVelocity = ctx.Rb.linearVelocity;
    }

    // While flying after a throw: reflect off surfaces (plushie bounce) and slam any
    // OTHER throwable it hits into a flying ragdoll too (chain reaction). Normal
    // gameplay collisions (kinematic roaming, held) are ignored.
    internal void HandleCollisionEnter(Collision collision)
    {
        if (ctx.State != AgentState.Thrown) return;

        float impact = lastVelocity.magnitude;
        if (impact < owner.minBounceSpeed) return;

        // A hard knock is stressful.
        if (impact >= owner.hardImpactThreshold) ctx.Dna?.Needs.AddAffect(-owner.affectOnHardCollision);

        // Hit another throwable → knock it flying away from us, with an upward pop.
        var other = collision.collider.GetComponentInParent<IThrowable>();
        if (other != null && !ReferenceEquals(other, ctx.Owner) && !ctx.IsBreeding)
        {
            Vector3 push = collision.transform.position - ctx.Body.position; push.y = 0f;
            push = (push.normalized + Vector3.up * owner.knockUpBias).normalized;
            other.Knock(push * impact * owner.knockTransfer);
        }

        // Bounce off whatever we hit (floor, wall, or that other creature).
        if (bounceCount < owner.maxBounces)
        {
            Vector3 normal = collision.GetContact(0).normal;
            ctx.Rb.linearVelocity = Vector3.Reflect(lastVelocity, normal) * owner.bounciness;
            if (owner.bounceSpin > 0f)
                ctx.Rb.AddTorque(Random.insideUnitSphere * owner.bounceSpin, ForceMode.Impulse);

            bounceCount++;
            settleTimer = 0f;   // a bounce isn't "resting" — restart the settle clock
            owner.onBounce?.Invoke();
        }
    }

    internal void HandleTriggerEnter(Collider other)
    {
        if (ctx.State != AgentState.Thrown) return;

        float impact = lastVelocity.magnitude;
        if (impact < owner.minBounceSpeed) return;

        var hit = other.GetComponentInParent<IThrowable>();
        if (hit == null || ReferenceEquals(hit, ctx.Owner) || ctx.IsBreeding) return;

        Vector3 push = other.transform.position - ctx.Body.position; push.y = 0f;
        if (push.sqrMagnitude < 0.001f) push = ctx.Body.forward;
        push = (push.normalized + Vector3.up * owner.knockUpBias).normalized;
        hit.Knock(push * impact * owner.knockTransfer);

        if (impact >= owner.hardImpactThreshold) ctx.Dna?.Needs.AddAffect(-owner.affectOnHardCollision);
    }

    // Knocked by another thrown object (IThrowable contract). If currently NavMesh-
    // controlled, hand off to physics like a throw; then apply the impulse so it
    // ragdolls away and can bounce / chain into others.
    internal void Knock(Vector3 force) => Knock(force, true);

    internal void Knock(Vector3 force, bool stress)
    {
        if (ctx.State == AgentState.Carried) return;   // in the player's hand — don't yank it out
        if (ctx.CurrentContainer != null || ctx.IsBreeding) return;   // penned or incubating: tackle-proof — only the player can take it out

        // A knock mid-flight must NOT reset the safety timeout: a cluster of creatures knocking each
        // other every contact would otherwise reset it forever and hang "in the air" — preserve it.
        bool  wasAirborne = ctx.State == AgentState.Thrown;
        float keepThrown  = thrownTimer;

        owner.RequestReleaseStation();
        EnterRagdoll();
        if (wasAirborne) thrownTimer = keepThrown;

        if (stress) ctx.Dna?.Needs.AddAffect(-owner.affectOnThrow);   // being slammed around is stressful
        ctx.Rb.AddForce(force, ForceMode.Impulse);
        owner.onThrow?.Invoke();
    }

    // Cannon spawn: disables the NavMeshAgent BEFORE teleporting to the muzzle so the agent never
    // fires OnEnable off-mesh (which would error, snap the transform to the floor, and fight
    // physics). Initialize() must have been called first on a valid NavMesh point — this just
    // handles the "pop out of the machine" movement. It then arcs as a ragdoll, lands, and gets up
    // onto the mesh via the normal throw pipeline (TickThrown → BeginGetUp).
    internal void Launch(Vector3 launchPos, Vector3 launchVelocity)
    {
        DetachToPhysics();
        ctx.Body.position = launchPos;
        ApplyThrownPhysics();
        ctx.HoldAnchor = null;
        ctx.State      = AgentState.Thrown;
        // VelocityChange (not Impulse) so the body gets EXACTLY the computed ballistic velocity
        // regardless of mass — the spawner solved this to land inside the cannon's radius.
        ctx.Rb.AddForce(launchVelocity, ForceMode.VelocityChange);
    }

    internal void EnterRagdoll()
    {
        DetachToPhysics();
        ApplyThrownPhysics();
        ctx.HoldAnchor = null;
        ctx.State      = AgentState.Thrown;
    }

    internal void TickThrown()
    {
        // Held as a ragdoll through a navmesh rebake — don't settle/rejoin until the bake finishes
        // (Rebaked clears this), so we never try to anchor onto a mesh that's mid-rebuild.
        if (ctx.RebakeInProgress) return;
        if (owner.forceRagdoll) return;

        if (hasNavAnchor && ctx.Body.position.y < lastNavAnchor.y - owner.voidFallDrop)
        {
            voidRescues++;
            if (voidRescues >= 2 && RejoinNavMesh(lastNavAnchor, ctx.Agent.areaMask))
            {
                owner.RequestRoam();
                return;
            }
            ctx.Rb.linearVelocity  = Vector3.zero;
            ctx.Rb.angularVelocity = Vector3.zero;
            ctx.Body.position      = lastNavAnchor + Vector3.up * 1f;
            settleTimer = 0f;
            thrownTimer = 0f;
            return;
        }

        thrownTimer += Time.deltaTime;

        // Settle only when it's actually slow AND resting on the floor — a low velocity
        // mid-bounce or while sliding off a ledge shouldn't count.
        bool resting = ctx.Rb.linearVelocity.sqrMagnitude < owner.settleSpeed * owner.settleSpeed && IsGrounded();
        if (resting) settleTimer += Time.deltaTime;
        else         settleTimer  = 0f;

        // Recover when it has rested long enough, or as a safety net if it never stops
        // (frictionless floor, wedged against geometry) so it can't slide/hang forever.
        if (settleTimer >= owner.settleDelay || thrownTimer >= owner.maxThrownTime)
            BeginGetUp();
    }

    // Dazed-then-stand-up beat after landing. Holds still for downedDelay, then
    // slerps from the tumbled pose to upright over getUpDuration, then resumes.
    internal void TickRecovering()
    {
        recoverTimer += Time.deltaTime;

        float t = effGetUpDuration <= 0f
            ? 1f
            : Mathf.InverseLerp(effDownedDelay, effDownedDelay + effGetUpDuration, recoverTimer);
        float smoothT = Mathf.SmoothStep(0f, 1f, t);
        ctx.Body.position = Vector3.Lerp(getUpFromPos, getUpToPos, smoothT);
        ctx.Body.rotation = Quaternion.Slerp(getUpFrom, getUpTo, smoothT);

        if (recoverTimer >= effDownedDelay + effGetUpDuration)
        {
            if (!ctx.Agent.enabled) ctx.Agent.enabled = true;
            ctx.Agent.Warp(getUpToPos);
            ctx.Agent.ResetPath();
            owner.onGetUp?.Invoke();
            owner.RequestRoam();
        }
    }

    // Cold-start safety net: a failed NavMesh handoff (first load before the floor/breeding area is
    // baked, a late cloud pull, a rebake) can leave a creature kinematic but OFF the mesh — frozen in
    // the air. Detect that and recover: a penned breeder is re-anchored onto its (now-baked) breeding
    // mesh WITHOUT touching the pen census or its egg (Release would cancel the breeding); a free
    // creature is dropped to physics so gravity settles it through the normal land→get-up pipeline.
    internal void RecoverIfStuckOffMesh()
    {
        bool stuck = ctx.IsNavMeshControlled() && ctx.Rb.isKinematic && (!ctx.Agent.enabled || !ctx.Agent.isOnNavMesh);
        if (!stuck) { offMeshGrace = 0f; return; }

        offMeshGrace += Time.deltaTime;
        if (offMeshGrace < owner.offMeshRecoverDelay) return;
        offMeshGrace = 0f;

        if (ctx.CurrentContainer != null)
        {
            if (RejoinNavMesh(ctx.CurrentContainer.Center, ctx.ConfinedAreaMask)) owner.RequestRoam();
            return;   // breeding mesh still not ready → keep retrying each window (stays put, not cancelled)
        }

        owner.RequestReleaseStation();
        EnterRagdoll();
    }

    // ── IThrowable (physics handoff) ──────────────────────────────

    internal void OnGrab(Transform anchor)
    {
        owner.RequestReleaseStation();   // grabbing mid-need interrupts SeekingNeed/UsingStation cleanly

        // Lifting a penned creature is the only way out: drop it from the pen's census and hand
        // its areaMask back to free, so wherever it's next thrown it roams normally again.
        owner.RequestReleaseFromPen();

        ctx.HoldAnchor = anchor;
        ctx.State      = AgentState.Carried;
        settleTimer    = 0f;

        DetachToPhysics();
        ctx.Rb.useGravity      = false;          // floats to the hand while held
        ctx.Rb.angularVelocity = Vector3.zero;
        ctx.Rb.linearDamping   = 0f;             // crisp follow while carried
        ctx.Rb.angularDamping  = 0.05f;

        owner.onGrab?.Invoke();
    }

    internal void OnRelease()
    {
        EnterRagdoll();
    }

    internal void OnThrow(Vector3 force)
    {
        EnterRagdoll();
        ctx.Rb.AddForce(force, ForceMode.Impulse);
        ctx.Dna?.Needs.AddAffect(-owner.affectOnThrow);
        owner.onThrow?.Invoke();
    }

    // ── Physics handoff (NavMeshAgent ⇄ Rigidbody) ────────────────

    // Stop NavMesh steering and let physics own the body (carry/throw/knock). Idempotent — safe to
    // call when already detached. Callers set the gravity/damping for their specific case after.
    internal void DetachToPhysics()
    {
        if (ctx.Agent.enabled && ctx.Agent.isOnNavMesh) CaptureNavAnchor(ctx.Body.position);
        if (ctx.Agent.enabled) ctx.Agent.enabled = false;
        ctx.Rb.isKinematic = false;
        ctx.SetColliderTrigger(false);      // physics owns it now → solid, so it collides/bounces
    }

    // Free-fall physics + reset the flight counters. Shared by throw / release / knock (they differ
    // only in the impulse applied afterwards).
    private void ApplyThrownPhysics()
    {
        ctx.Rb.useGravity     = true;
        ctx.Rb.linearDamping  = owner.thrownLinearDamping;
        ctx.Rb.angularDamping = owner.thrownAngularDamping;
        settleTimer           = 0f;
        thrownTimer           = 0f;
        bounceCount           = 0;
        voidRescues           = 0;
    }

    // Hand the body back to the NavMeshAgent at 'desired' (snapped onto 'mask'). Returns false if it
    // couldn't be placed on the mesh — the caller decides how to recover (body stays in physics).
    internal bool RejoinNavMesh(Vector3 desired, int mask)
    {
        ctx.Rb.isKinematic = true;
        ctx.Rb.useGravity  = false;
        if (!ctx.Agent.enabled) ctx.Agent.enabled = true;

        Vector3 point = desired;
        if (NavMesh.SamplePosition(desired, out var hit, owner.sampleRadius * 2f, mask))
            point = hit.position;

        if (!ctx.Agent.Warp(point) || !ctx.Agent.isOnNavMesh) return false;
        ctx.Agent.ResetPath();
        ctx.SetColliderTrigger(true);       // back on the mesh → trigger again (caller kept it solid on failure)
        return true;
    }

    // After a throw settles: snap back onto the NavMesh but DON'T steer yet. It
    // stays where it landed (still tumbled) and enters the get-up beat — TickRecovering
    // animates it upright before the agent brain takes over.
    private void BeginGetUp()
    {
        if (!NavMesh.SamplePosition(ctx.Body.position, out var hit, owner.sampleRadius * 2f, ctx.Agent.areaMask))
        {
            ctx.State = AgentState.Thrown;
            return;
        }

        getUpFromPos = ctx.Body.position;
        getUpToPos   = hit.position;

        ctx.Rb.isKinematic = true;
        ctx.Rb.useGravity  = false;
        ctx.SetColliderTrigger(true);

        ctx.Agent.updateRotation = false;

        float scale  = Mathf.Max(0.1f, ctx.Profile.RecoverySpeed) * Random.Range(1f - owner.getUpJitter, 1f + owner.getUpJitter);
        effDownedDelay   = owner.downedDelay   / scale;
        effGetUpDuration = owner.getUpDuration / scale;

        Vector3 fwd = ctx.Body.forward; fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.forward;
        getUpFrom    = ctx.Body.rotation;
        getUpTo      = Quaternion.LookRotation(fwd.normalized, Vector3.up);
        recoverTimer = 0f;
        ctx.State    = AgentState.Recovering;

        owner.onLand?.Invoke();
    }

    // Down-ray from the body center. A ray starting inside a convex collider doesn't
    // report that collider, so any hit within reach means there's floor under us.
    private bool IsGrounded()
    {
        float reach = (ctx.Col != null ? ctx.Col.bounds.extents.y : 0.5f) + owner.groundCheckDistance;
        if (Physics.Raycast(ctx.Body.position, Vector3.down, out var hit, reach, ~0, QueryTriggerInteraction.Ignore))
            return hit.collider != ctx.Col;
        return false;
    }

    internal void ResetForReuse()
    {
        settleTimer = thrownTimer = recoverTimer = 0f;
        bounceCount = 0;
        voidRescues = 0;
    }
}
}
