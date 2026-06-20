using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public partial class MoriMochiAgent
{
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

        // A knock mid-flight must NOT reset the safety timeout: a cluster of creatures knocking each
        // other every contact would otherwise reset it forever and hang "in the air" — preserve it.
        bool  wasAirborne = state == AgentState.Thrown;
        float keepThrown  = thrownTimer;

        ReleaseStation();
        EnterRagdoll();
        if (wasAirborne) thrownTimer = keepThrown;

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
}
