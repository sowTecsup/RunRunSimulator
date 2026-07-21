using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

internal enum AgentState { Idle, Roaming, Reacting, Carried, Thrown, Recovering, SeekingNeed, UsingStation, Courting }

internal class AgentContext
{
    internal readonly MoriMochiAgent Owner;
    internal readonly Transform      Body;
    internal readonly NavMeshAgent   Agent;
    internal readonly Rigidbody      Rb;
    internal readonly Collider       Col;

    internal float BaseSpeed;

    internal AgentState         State = AgentState.Idle;
    internal CreatureDNA        Dna;
    internal RoleWorldProfile   Profile;
    internal Transform          Player;
    internal Transform          HoldAnchor;
    internal MoriMochiContainer CurrentContainer;
    internal int  FreeAreaMask;
    internal int  ConfinedAreaMask;
    internal bool RebakeInProgress;

    internal AgentContext(MoriMochiAgent owner, NavMeshAgent agent, Rigidbody rb, Collider col)
    {
        Owner = owner;
        Body  = owner.transform;
        Agent = agent;
        Rb    = rb;
        Col   = col;
    }

    internal bool IsNavMeshControlled() =>
        State == AgentState.Idle        || State == AgentState.Roaming      || State == AgentState.Reacting ||
        State == AgentState.SeekingNeed || State == AgentState.UsingStation || State == AgentState.Courting;

    internal bool IsBreeding => Dna != null && Dna.BusyState == BusyReason.Breeding;

    internal bool IsMoving =>
        Agent != null && Agent.enabled && Agent.isOnNavMesh && !Agent.isStopped &&
        Agent.velocity.sqrMagnitude > 0.01f;

    internal void SetStopped(bool stopped)
    {
        if (Agent.enabled && Agent.isOnNavMesh) Agent.isStopped = stopped;
    }

    internal void SetColliderTrigger(bool isTrigger)
    {
        if (Col != null) Col.isTrigger = isTrigger;
    }

    internal void SetDestinationSafe(Vector3 desired)
    {
        if (!Agent.enabled || !Agent.isOnNavMesh) return;
        if (NavMesh.SamplePosition(desired, out var hit, Owner.sampleRadius, Agent.areaMask))
            Agent.SetDestination(hit.position);
    }

    internal float PlanarDistanceToPlayer()
    {
        if (Player == null) return float.MaxValue;
        Vector3 d = Player.position - Body.position; d.y = 0f;
        return d.magnitude;
    }

    internal static Vector3 RandomPointInBounds(Bounds b) => new Vector3(
        Random.Range(b.min.x, b.max.x), b.center.y, Random.Range(b.min.z, b.max.z));
}
}
