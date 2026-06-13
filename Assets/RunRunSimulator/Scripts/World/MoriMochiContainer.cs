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

    // A trimmed view of who's penned — just name, gender and personality (no full CreatureDNA dump).
    // LINQ projects each live occupant into a lightweight OccupantInfo; [ShowInInspector] surfaces it
    // read-only at runtime and re-evaluates every inspector repaint, so it tracks who's actually in.
    [ShowInInspector, ReadOnly, PropertyOrder(10)]
    [LabelText("Occupants"), TableList(IsReadOnly = true, ShowIndexLabels = true)]
    public List<OccupantInfo> OccupantInfos => occupants
        .Where(a => a != null && a.DNA != null)
        .Select(a => new OccupantInfo { Name = a.DNA.CustomName, Gender = a.DNA.Gender, Personality = a.DNA.Personality })
        .ToList();

    // Display-only row for the occupants table.
    public struct OccupantInfo
    {
        [ReadOnly] public string         Name;
        [ReadOnly] public CreatureGender Gender;
        [ReadOnly] public Personality    Personality;
    }

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
        if (Claim(agent))
            Debug.Log($"[{name}] Admitido \"{agent.DNA?.CustomName}\" — ocupantes: {occupants.Count}/{capacity}.");
        else
            Debug.LogWarning($"[{name}] '{agent.name}' NO admitido. ¿El piso del corral está pintado con el área de cría y horneado (bake)?");
    }

    // Confines an agent and registers it as an occupant if there's room and it isn't already in.
    // Shared by thrown-in admission (Admit) and startup reclaim (penned breeders restored after a
    // scene reload — see BreedingContainer). Returns false if full, already in, or confinement failed.
    protected bool Claim(MoriMochiAgent agent)
    {
        if (agent == null || occupants.Contains(agent) || IsFull) return false;
        if (!agent.EnterConfinement(this)) return false;
        occupants.Add(agent);
        return true;
    }

    private void BounceOut(MoriMochiAgent agent)
    {
        Vector3 away = agent.transform.position - Center; away.y = 0f;
        away = away.sqrMagnitude > 0.01f ? away.normalized : Random.insideUnitSphere;
        agent.Knock(away * bounceOut + Vector3.up * bounceUp);
    }

    // Called by the agent when the player lifts it out — the only exit. Virtual so a pen can react
    // (BreedingContainer cancels any in-progress pairing for the creature leaving).
    public virtual void Release(MoriMochiAgent agent) => occupants.Remove(agent);
}
