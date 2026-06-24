using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

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
public class MoriMochiContainer : MonoBehaviour, IAnchorPlace
{
    [Tooltip("Trigger volume = the pen interior. Auto-grabbed from this object if left empty.")]
    [SerializeField] private BoxCollider area;

    // Anchor key = the furniture cell this pen sits on. Lets a creature persist "I live here" (DNA
    // LocationKey) so the spawner drops it straight back in on load instead of cannon-firing it.
    protected string anchorKey = "";
    public string AnchorKey => anchorKey;
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

    // The PlacedFurnitureMarker is stamped by the FurnitureSpawner AFTER the Instantiate, so it isn't
    // on us yet in Awake — derive the anchor key in Start (one frame later) once the marker exists.
    // No marker (e.g. a hand-placed test pen) → fall back to the object name as the key.
    protected virtual void Start()
    {
        var marker = GetComponentInParent<PlacedFurnitureMarker>();
        anchorKey = marker != null ? $"{marker.AnchorCell.x}_{marker.AnchorCell.y}" : name;
        AnchorRegistry.Register(this);
    }
    protected virtual void OnDestroy()
    {
        AnchorRegistry.Unregister(this);
    }

    // IAnchorPlace — where the spawner drops a reclaimed body, and the reclaim itself. Reclaim happens
    // on LOAD (the DNA was already stamped with this key), so it confines but does NOT persist.
    public virtual Vector3 AnchorPosition(int slot) => Center;
    public virtual bool    TryReclaim(MoriMochiAgent agent, int slot) => Claim(agent);

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
        if (!Claim(agent))
        {
            Debug.LogWarning($"[{name}] '{agent.name}' NO admitido. ¿El piso del corral está pintado con el área de cría y horneado (bake)?");
            return;
        }
        Debug.Log($"[{name}] Admitido \"{agent.DNA?.CustomName}\" — ocupantes: {occupants.Count}/{capacity}.");

        // This is the real player entry (a MoriMochi thrown in) — stamp the anchor on the save record
        // and persist via the bus so the next load drops it back here. Slot stays unassigned; a place
        // that uses fixed slots (e.g. breeding when a pair forms) assigns it later.
        if (agent.DNA != null)
        {
            agent.DNA.LocationKey  = anchorKey;
            agent.DNA.LocationSlot = -1;
            if (GameManager.Instance != null && GameManager.Instance.Registry != null)
                GameEvents.RegistryChanged(GameManager.Instance.Registry);
        }
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

    // Called by the agent when the player lifts it out — the only PLAYER exit. Virtual so a pen can
    // react (BreedingContainer cancels any in-progress pairing for the creature leaving). Clears the
    // anchor off the save record and persists, so the next load won't re-drop the creature back here.
    public virtual void Release(MoriMochiAgent agent)
    {
        occupants.Remove(agent);
        if (agent != null && agent.DNA != null && agent.DNA.LocationKey == anchorKey)
        {
            agent.DNA.LocationKey  = "";
            agent.DNA.LocationSlot = -1;
            if (GameManager.Instance != null && GameManager.Instance.Registry != null)
                GameEvents.RegistryChanged(GameManager.Instance.Registry);
        }
    }

    // Pulls an occupant out for the agent's own lifecycle (pool recycle / re-init) — NOT a player
    // exit, so it leaves the anchor on the DNA and does not persist. Silent census fix only.
    public void DetachOccupant(MoriMochiAgent agent) => occupants.Remove(agent);
}
}
