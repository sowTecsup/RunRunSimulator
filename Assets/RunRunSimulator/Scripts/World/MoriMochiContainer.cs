using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

// A breeding-room pen (a furniture piece). Catches MoriMonchis THROWN into its trigger volume up
// to `capacity`, hands each one to its agent's confinement (NavMesh areaMask = BreedingRoom +
// bounded roam), and keeps the census of who's inside. The only way out is the player lifting one
// (the agent calls Release on grab). Same World domain as the agent → direct refs, no GameEvents.
//
// Multiple pens coexist with a SINGLE "BreedingRoom" Area type: each agent's breeding-only mask
// (can't cross the normal floor between pens) plus its per-pen bounded roam keeps it in THIS pen.
// The area type just has to be excluded from free agents' masks so they route around every pen.
//
// Setup: the assigned collider must be a trigger; paint the pen floor with the BreedingRoom Area
// (NavMeshModifier set-area) and (re)bake so the floor carries that area. See 06 - Player & World.
[RequireComponent(typeof(BoxCollider))]
public class MoriMochiContainer : MonoBehaviour
{
    [Tooltip("Trigger volume = the pen interior. Auto-grabbed from this object if left empty.")]
    [SerializeField] private BoxCollider area;
    [Tooltip("How many MoriMonchis fit. A throw past this is bounced back out.")]
    [SerializeField, Min(1)] private int capacity = 2;

    [Header("Rejection (pen full)")]
    [Tooltip("Outward (horizontal) impulse popping a rejected throw back out of a full pen.")]
    [SerializeField] private float bounceOut = 5f;
    [Tooltip("Upward impulse blended into the rejection so it pops up, not just sideways.")]
    [SerializeField] private float bounceUp = 4f;

    private readonly List<MoriMochiAgent> occupants = new List<MoriMochiAgent>();

    public Vector3 Center         => area.bounds.center;
    public Bounds  InteriorBounds => area.bounds;
    public bool    IsFull         => occupants.Count >= capacity;
    public IReadOnlyList<MoriMochiAgent> Occupants => occupants;

    // The DNA of every MoriMochi currently penned — for breeding / pen UI later ("nos servirá").
    // [ShowInInspector] surfaces the live occupants in the inspector at runtime (read-only).
    // Añadimos .ToList() al final para que devuelva una lista concreta
    [ShowInInspector, ReadOnly, PropertyOrder(10)]
    [LabelText("Occupants (DNA)"), ListDrawerSettings(IsReadOnly = true, ShowItemCount = true)]
    public List<CreatureDNA> OccupantDNAs => occupants.Select(a => a.DNA).ToList();

    private void Reset() => area = GetComponent<BoxCollider>();
    protected virtual void Awake() { if (area == null) area = GetComponent<BoxCollider>(); }

    // A creature THROWN in from outside crosses the trigger boundary here.
    private void OnTriggerEnter(Collider other)
    {
        var agent = other.GetComponentInParent<MoriMochiAgent>();
        if (agent == null || occupants.Contains(agent) || !agent.IsAirborne) return;

        if (IsFull) BounceOut(agent);   // bounce only on ENTER (Stay would re-fire every frame)
        else        Admit(agent);
    }

    // Catches a creature DROPPED inside the pen: it was already overlapping the trigger, so it
    // never fired OnTriggerEnter. Never bounces on stay; a full pen simply doesn't catch a drop.
    private void OnTriggerStay(Collider other)
    {
        if (IsFull) return;
        var agent = other.GetComponentInParent<MoriMochiAgent>();
        if (agent == null || occupants.Contains(agent) || !agent.IsAirborne) return;
        Admit(agent);
    }

    // Register as an occupant only if the agent actually confined (its pen floor is on the breeding
    // NavMesh) — otherwise we'd hold a phantom occupant that isn't really inside.
    private void Admit(MoriMochiAgent agent)
    {
        if (agent.EnterConfinement(this)) occupants.Add(agent);
    }

    private void BounceOut(MoriMochiAgent agent)
    {
        Vector3 away = agent.transform.position - Center; away.y = 0f;
        away = away.sqrMagnitude > 0.01f ? away.normalized : Random.insideUnitSphere;
        agent.Knock(away * bounceOut + Vector3.up * bounceUp);
    }

    // Called by the agent when the player lifts it out — the only exit.
    public void Release(MoriMochiAgent agent) => occupants.Remove(agent);
}
