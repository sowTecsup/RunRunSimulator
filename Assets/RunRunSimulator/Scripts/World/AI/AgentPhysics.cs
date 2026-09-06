using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

internal class AgentPhysics
{
    private readonly MoriMochiAgent owner;
    private readonly AgentContext   ctx;

    private float     settleTimer;
    private float     thrownTimer;
    private Vector3   lastVelocity;
    private int       bounceCount;
    private float     offMeshGrace;

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
        if (ctx.State == AgentState.Carried && ctx.HoldAnchor != null)
            ctx.Rb.linearVelocity = (ctx.HoldAnchor.position - ctx.Rb.position) * owner.followSpeed;
        else if (ctx.State == AgentState.Thrown)
            lastVelocity = ctx.Rb.linearVelocity;
    }

    internal void HandleCollisionEnter(Collision collision)
    {
        if (ctx.State != AgentState.Thrown) return;

        float impact = lastVelocity.magnitude;
        if (impact < owner.minBounceSpeed) return;

        if (impact >= owner.hardImpactThreshold) ctx.Dna?.Needs.AddAffect(-owner.affectOnHardCollision);

        var other = collision.collider.GetComponentInParent<IThrowable>();
        if (other != null && !ReferenceEquals(other, ctx.Owner) && !ctx.IsBreeding)
        {
            var otherAgent = collision.collider.GetComponentInParent<MoriMochiAgent>();
            if (otherAgent == null || (!ExpeditionTeams.AreAllies(owner.Team, otherAgent.Team) && !owner.IgnoresChainKnock(otherAgent)))
            {
                Vector3 push = collision.transform.position - ctx.Body.position; push.y = 0f;
                push = (push.normalized + Vector3.up * owner.knockUpBias).normalized;
                other.Knock(push * impact * owner.knockTransfer);
            }
        }

        if (bounceCount < owner.maxBounces)
        {
            Vector3 normal = collision.GetContact(0).normal;
            ctx.Rb.linearVelocity = Vector3.Reflect(lastVelocity, normal) * owner.bounciness;
            if (owner.bounceSpin > 0f)
                ctx.Rb.AddTorque(Random.insideUnitSphere * owner.bounceSpin, ForceMode.Impulse);

            bounceCount++;
            settleTimer = 0f;
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

        var hitAgent = other.GetComponentInParent<MoriMochiAgent>();
        if (hitAgent == null || (!ExpeditionTeams.AreAllies(owner.Team, hitAgent.Team) && !owner.IgnoresChainKnock(hitAgent)))
        {
            Vector3 push = other.transform.position - ctx.Body.position; push.y = 0f;
            if (push.sqrMagnitude < 0.001f) push = ctx.Body.forward;
            push = (push.normalized + Vector3.up * owner.knockUpBias).normalized;
            hit.Knock(push * impact * owner.knockTransfer);
        }

        if (impact >= owner.hardImpactThreshold) ctx.Dna?.Needs.AddAffect(-owner.affectOnHardCollision);
    }

    internal void Knock(Vector3 force) => Knock(force, true);

    internal void Knock(Vector3 force, bool stress)
    {
        if (ctx.State == AgentState.Carried) return;
        if (ctx.CurrentContainer != null || ctx.IsBreeding) return;

        bool  wasAirborne = ctx.State == AgentState.Thrown;
        float keepThrown  = thrownTimer;

        owner.NotifyKnocked();
        owner.RequestReleaseStation();
        EnterRagdoll();
        if (wasAirborne) thrownTimer = keepThrown;

        if (stress) ctx.Dna?.Needs.AddAffect(-owner.affectOnThrow);
        ctx.Rb.AddForce(force, ForceMode.Impulse);
        if (owner.knockSpin > 0f)
            ctx.Rb.AddTorque(Random.insideUnitSphere * owner.knockSpin, ForceMode.Impulse);
        owner.onThrow?.Invoke();
    }

    internal void Launch(Vector3 launchPos, Vector3 launchVelocity)
    {
        DetachToPhysics();
        ctx.Body.position = launchPos;
        ApplyThrownPhysics();
        ctx.HoldAnchor = null;
        ctx.State      = AgentState.Thrown;
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

        bool resting = ctx.Rb.linearVelocity.sqrMagnitude < owner.settleSpeed * owner.settleSpeed && IsGrounded();
        if (resting) settleTimer += Time.deltaTime;
        else         settleTimer  = 0f;

        if (settleTimer >= owner.settleDelay || thrownTimer >= owner.maxThrownTime)
            BeginGetUp();
    }

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
            owner.NotifyRecovered();
        }
    }

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
            return;
        }

        owner.RequestReleaseStation();
        EnterRagdoll();
    }

    internal void OnGrab(Transform anchor)
    {
        owner.RequestReleaseStation();

        owner.RequestReleaseFromPen();

        ctx.HoldAnchor = anchor;
        ctx.State      = AgentState.Carried;
        settleTimer    = 0f;

        DetachToPhysics();
        ctx.Rb.useGravity      = false;
        ctx.Rb.angularVelocity = Vector3.zero;
        ctx.Rb.linearDamping   = 0f;
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

    internal void DetachToPhysics()
    {
        if (ctx.Agent.enabled && ctx.Agent.isOnNavMesh) CaptureNavAnchor(ctx.Body.position);
        if (ctx.Agent.enabled) ctx.Agent.enabled = false;
        ctx.Rb.isKinematic = false;
        ctx.SetColliderTrigger(false);
    }

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
        ctx.SetColliderTrigger(true);
        return true;
    }

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
