using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

[RequireComponent(typeof(BoxCollider))]
public class MoriMochiContainer : MonoBehaviour
{
    [Tooltip("Trigger volume = the pen interior. Auto-grabbed from this object if left empty.")]
    [SerializeField] private BoxCollider area;

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

    [ShowInInspector, ReadOnly, PropertyOrder(10)]
    [LabelText("Occupants"), TableList(IsReadOnly = true, ShowIndexLabels = true)]
    public List<OccupantInfo> OccupantInfos => occupants
        .Where(a => a != null && a.DNA != null)
        .Select(a => new OccupantInfo { Name = a.DNA.CustomName, Gender = a.DNA.Gender, Role = a.DNA.Role })
        .ToList();

    public struct OccupantInfo
    {
        [ReadOnly] public string         Name;
        [ReadOnly] public CreatureGender Gender;
        [ReadOnly] public Role           Role;
    }

    private void Reset() => area = GetComponent<BoxCollider>();
    protected virtual void Awake() { if (area == null) area = GetComponent<BoxCollider>(); }

    public void SetAnchorKey(string key)
    {
        anchorKey = key;
        AnchorRegistry.Register(this);
    }

    protected virtual void Start()
    {
        if (string.IsNullOrEmpty(anchorKey))
        {
            var marker = GetComponentInParent<PlacedFurnitureMarker>();
            anchorKey = marker != null ? $"{marker.AnchorCell.x}_{marker.AnchorCell.y}" : name;
            AnchorRegistry.Register(this);
        }
    }
    protected virtual void OnDestroy()
    {
        AnchorRegistry.Unregister(this);
    }

    public virtual Vector3 AnchorPosition(int slot) => Center;
    public virtual bool    TryReclaim(MoriMochiAgent agent, int slot) => Claim(agent);

    private void OnTriggerEnter(Collider other)
    {
        var agent = other.GetComponentInParent<MoriMochiAgent>();
        if (agent == null || occupants.Contains(agent) || !agent.IsAirborne) return;

        if (IsFull) BounceOut(agent);
        else        Admit(agent);
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsFull) return;
        var agent = other.GetComponentInParent<MoriMochiAgent>();
        if (agent == null || occupants.Contains(agent) || !agent.IsAirborne) return;
        Admit(agent);
    }

    private void Admit(MoriMochiAgent agent)
    {
        if (!Claim(agent))
        {
            Debug.LogWarning($"[{name}] '{agent.name}' NO admitido. ¿El piso del corral está pintado con el área de cría y horneado (bake)?");
            return;
        }
        Debug.Log($"[{name}] Admitido \"{agent.DNA?.CustomName}\" — ocupantes: {occupants.Count}/{capacity}.");

        if (agent.DNA != null)
        {
            agent.DNA.LocationKey  = anchorKey;
            agent.DNA.LocationSlot = -1;
            if (GameManager.Instance != null && GameManager.Instance.Registry != null)
                GameEvents.RegistryChanged(GameManager.Instance.Registry);
        }
    }

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

    public void DetachOccupant(MoriMochiAgent agent) => occupants.Remove(agent);
}
}
