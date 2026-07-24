using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

// A single sighting a MoriMochi's perception query returns: which Perceivable, what kind it is,
// how far (squared) and how much affinity it carries. Value type — built fresh per query, never held.
public struct Percept
{
    public Perceivable Source;
    public PerceivableKind Kind;
    public float SqrDistance;
    public float Affinity;
}

// Marks any world object (player, MoriMochi, customer, prop) as something MoriMonchis can perceive.
// Self-registers with the PerceivableRegistry in OnEnable/OnDisable, mirroring NeedStation — same
// World domain, same lightweight registry pattern, no GameEvents needed for a same-domain query.
public class Perceivable : MonoBehaviour
{
    [SerializeField] private PerceivableKind kind;
    [SerializeField] private List<string> tags = new List<string>();

    public PerceivableKind Kind => kind;
    public IReadOnlyList<string> Tags => tags;

    public bool HasTag(string tag)
    {
        if (tags == null || string.IsNullOrEmpty(tag)) return false;
        for (int i = 0; i < tags.Count; i++)
            if (string.Equals(tags[i], tag, System.StringComparison.Ordinal)) return true;
        return false;
    }

    public Vector3 Position => transform.position;

    // Null for players/props/customers — only MoriMonchi Perceivables carry an agent.
    public MoriMochiAgent Monchi { get; private set; }

    private void Awake()
    {
        Monchi = GetComponent<MoriMochiAgent>();
        if (Monchi == null) Monchi = GetComponentInParent<MoriMochiAgent>();
    }

    private void OnEnable()  => PerceivableRegistry.Register(this);
    private void OnDisable() => PerceivableRegistry.Unregister(this);
}
}
