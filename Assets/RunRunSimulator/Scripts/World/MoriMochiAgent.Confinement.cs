using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

public partial class MoriMochiAgent
{
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
}
