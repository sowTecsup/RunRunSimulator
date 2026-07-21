using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

internal class AgentConfinement
{
    private enum CourtRole { Tend, Orbit }

    private readonly MoriMochiAgent owner;
    private readonly AgentContext   ctx;

    private MoriMochiAgent courtPartner;
    private Vector3        courtAnchor;
    private float          courtAngle;
    private float          courtRepathTimer;
    private CourtRole      courtRole;

    internal AgentConfinement(MoriMochiAgent owner, AgentContext ctx)
    {
        this.owner = owner;
        this.ctx   = ctx;
    }

    // Called by a MoriMochiContainer when a creature lands in a pen with room. Cuts the ragdoll,
    // snaps onto the breeding-area floor at the pen center, and restricts the areaMask so from now
    // on it can only walk inside breeding floor (released when the player grabs it). Returns false
    // (without confining) if the pen floor isn't on the breeding NavMesh — so the pen doesn't
    // register an occupant it never actually caught, and we never call ResetPath off-mesh.
    internal bool EnterConfinement(MoriMochiContainer pen)
    {
        ctx.Agent.areaMask = ctx.ConfinedAreaMask;

        // Warp/ResetPath throw on an agent not placed on a NavMesh — bail (back to physics) if the
        // pen floor isn't painted+baked as the breeding area, or the area name doesn't match.
        if (!owner.RequestRejoinNavMesh(pen.Center, ctx.ConfinedAreaMask))
        {
            Debug.LogWarning($"[MoriMochiAgent] '{ctx.Owner.name}' couldn't enter the pen — is its floor painted '{owner.breedingAreaName}' and baked?");
            owner.RequestDetachToPhysics();
            ctx.Rb.useGravity  = true;
            ctx.Agent.areaMask = ctx.FreeAreaMask;
            return false;
        }

        ctx.CurrentContainer = pen;
        ctx.HoldAnchor        = null;

        Vector3 flat = ctx.Body.forward; flat.y = 0f;
        if (flat.sqrMagnitude < 0.001f) flat = Vector3.forward;
        ctx.Body.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);

        owner.RequestRoam();
        return true;
    }

    internal void EnterCourtship(MoriMochiAgent partner, Vector3 anchor)
    {
        owner.RequestReleaseStation();

        courtPartner     = partner;
        courtAnchor      = anchor;
        courtRole        = ctx.Dna != null && ctx.Dna.Gender == CreatureGender.Female ? CourtRole.Tend : CourtRole.Orbit;
        courtAngle       = Random.value * Mathf.PI * 2f;
        courtRepathTimer = 0f;

        ctx.Agent.speed          = ctx.BaseSpeed * owner.courtSpeedMultiplier;
        ctx.Agent.updateRotation = false;
        ctx.SetStopped(false);

        ctx.State = AgentState.Courting;
    }

    internal void ExitCourtship()
    {
        if (ctx.State != AgentState.Courting) return;
        courtPartner = null;
        owner.RequestRoam();
    }

    internal void TickCourting()
    {
        if (courtPartner == null || courtPartner.DNA == null ||
            courtPartner.DNA.BusyState != BusyReason.Breeding)
        {
            ExitCourtship();
            return;
        }

        if (courtRole == CourtRole.Orbit) TickOrbit();
        else                              TickTend();

        FacePartner();
    }

    private void TickOrbit()
    {
        courtAngle += owner.courtAngularSpeed * Mathf.Deg2Rad * Time.deltaTime;

        courtRepathTimer -= Time.deltaTime;
        if (courtRepathTimer > 0f) return;
        courtRepathTimer = owner.courtRepath;

        float   a      = courtAngle + owner.courtLookahead * Mathf.Deg2Rad;
        Vector3 center = courtPartner.transform.position;
        Vector3 target = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * owner.courtOrbitRadius;
        ctx.SetDestinationSafe(target);
    }

    private void TickTend()
    {
        courtRepathTimer -= Time.deltaTime;
        if (courtRepathTimer > 0f) return;
        courtRepathTimer = owner.courtTendInterval;

        Vector2 d = Random.insideUnitCircle * owner.courtTendRadius;
        ctx.SetDestinationSafe(courtAnchor + new Vector3(d.x, 0f, d.y));
    }

    private void FacePartner()
    {
        if (courtPartner == null) return;
        Vector3 dir = courtPartner.transform.position - ctx.Body.position; dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        ctx.Body.rotation = Quaternion.Slerp(
            ctx.Body.rotation, Quaternion.LookRotation(dir.normalized, Vector3.up), 10f * Time.deltaTime);
    }

    // A furniture-driven rebake is imminent. If we're walking the mesh, drop any reservation and
    // hand the body to PHYSICS (ragdoll). A dynamic Rigidbody isn't a NavMeshAgent, so the bake
    // can't teleport it across the room; it just rests where it stood. Physics-driven states
    // (carried / thrown / recovering) own their own re-entry — leave them.
    internal void OnNavMeshWillRebake()
    {
        if (ctx.RebakeInProgress || !ctx.IsNavMeshControlled()) return;
        ctx.RebakeInProgress = true;
        owner.RequestReleaseStation();
        owner.RequestEnterRagdoll();
    }

    // The fresh mesh is live. Release the ragdoll: TickThrown now settles it and BeginGetUp
    // re-anchors onto the new mesh — staggered per-personality, so the whole crowd doesn't snap
    // upright in the same frame (the old "tilting" pop). Off-mesh creatures just keep ragdolling
    // until they settle somewhere valid.
    internal void OnNavMeshRebaked()
    {
        if (!ctx.RebakeInProgress) return;
        ctx.RebakeInProgress = false;
    }

    // Lifting a penned creature is the only way out: drop it from the pen's census and hand
    // its areaMask back to free, so wherever it's next thrown it roams normally again.
    internal void ReleaseFromPen()
    {
        if (ctx.CurrentContainer == null) return;
        ctx.CurrentContainer.Release(ctx.Owner);
        ctx.CurrentContainer = null;
        ctx.Agent.areaMask   = ctx.FreeAreaMask;
    }

    // lifecycle re-init, not a player exit: drop from the census only — Release here would persist a
    // phantom anchor and, in a BreedingContainer, cancel the server egg on pool reuse.
    internal void DetachForReuse()
    {
        if (ctx.CurrentContainer == null) return;
        ctx.CurrentContainer.DetachOccupant(ctx.Owner);
        ctx.CurrentContainer = null;
    }

    internal string DescribeCourtship() =>
        ctx.State != AgentState.Courting ? "—"
        : $"{courtRole} ↔ {(courtPartner != null && courtPartner.DNA != null ? courtPartner.DNA.CustomName : "?")}";
}
}
