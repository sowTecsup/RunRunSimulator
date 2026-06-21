using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

// A world object MoriMonchis walk to and use to refill ONE need (Feeder/RestZone/PlayZone derive from
// this). Self-registers with the NeedStationRegistry so agents can find the closest available one.
// Same World domain as the agent → direct calls, no GameEvents.
//
// Goes on a furniture prefab (spawned by FurnitureSpawner); OnEnable/OnDisable handle (un)registering
// automatically when the piece is placed/removed. (Future: stations deplete a resource the player
// must replenish — for now they refill to full.)
//
// Capacity = number of use points. Because the piece is furniture and we can't assume its orientation,
// the station offers SEVERAL stand-here points; each one is a SLOT. An agent reserves the closest free,
// reachable slot and holds it until it finishes (full) or is interrupted (grabbed). So N use points →
// up to N MoriMonchis served at once, one per point. No points → a single implicit slot at the
// transform.
public abstract class NeedStation : MonoBehaviour
{
    [Tooltip("Stand-here points (= slots). Capacity is how many of these there are; each serving agent holds one. Several let agents use it from any reachable side regardless of the furniture's rotation. Empty → one slot at this transform.")]
    [SerializeField] private List<Transform> usePoints = new List<Transform>();
    [Tooltip("Stat points restored per second while an agent is using it (continuous, fills to 100).")]
    [SerializeField, Min(0f)] private float fillPerSecond = 25f;

    public abstract NeedType Need { get; }

    // One occupant per slot (null = free). Sized to the use points (min 1). Built lazily so it's
    // valid whether queried in edit or play mode.
    private MoriMochiAgent[] occupants;
    private int SlotCount => (usePoints != null && usePoints.Count > 0) ? usePoints.Count : 1;

    private void EnsureSlots()
    {
        if (occupants == null || occupants.Length != SlotCount)
            occupants = new MoriMochiAgent[SlotCount];
    }

    private Vector3 SlotPosition(int i) =>
        (usePoints != null && i < usePoints.Count && usePoints[i] != null) ? usePoints[i].position : transform.position;

    // Representative position for the registry's distance ranking: first configured point, else self.
    public Vector3 UsePosition
    {
        get
        {
            if (usePoints != null)
                foreach (var p in usePoints)
                    if (p != null) return p.position;
            return transform.position;
        }
    }

    // True while at least one slot is free.
    public bool IsAvailable
    {
        get
        {
            EnsureSlots();
            foreach (var o in occupants) if (o == null) return true;
            return false;
        }
    }

    private void OnEnable()  { EnsureSlots(); NeedStationRegistry.Register(this); }
    private void OnDisable() => NeedStationRegistry.Unregister(this);

    // Reserves the closest FREE slot 'agent' can actually reach (its stand point snapped onto the
    // NavMesh within sampleRadius on areaMask) and returns where to stand. If the agent already holds
    // a slot here, keeps it. Returns false if every free slot is unreachable / the station is full —
    // the agent then skips it. The slot stays held until Release (full, grabbed, or any interruption).
    public bool TryReserve(MoriMochiAgent agent, Vector3 from, int areaMask, float sampleRadius, out Vector3 usePos)
    {
        usePos = transform.position;
        EnsureSlots();

        // Re-entrant: already holding a slot → reuse it (re-snap its position for the destination).
        for (int i = 0; i < occupants.Length; i++)
            if (occupants[i] == agent)
            {
                usePos = NavMesh.SamplePosition(SlotPosition(i), out var h, sampleRadius, areaMask) ? h.position : SlotPosition(i);
                return true;
            }

        int best = -1; float bestSqr = float.MaxValue;
        for (int i = 0; i < occupants.Length; i++)
        {
            if (occupants[i] != null) continue;
            if (!NavMesh.SamplePosition(SlotPosition(i), out var hit, sampleRadius, areaMask)) continue;
            float d = (hit.position - from).sqrMagnitude;
            if (d < bestSqr) { bestSqr = d; best = i; usePos = hit.position; }
        }
        if (best < 0) return false;

        occupants[best] = agent;
        return true;
    }

    public void Release(MoriMochiAgent agent)
    {
        if (occupants == null) return;
        for (int i = 0; i < occupants.Length; i++)
            if (occupants[i] == agent) occupants[i] = null;
    }

    // Refills this station's need on the agent. Returns true once the stat is full (≥ 100).
    public bool Refill(NeedsState needs, float dt)
    {
        needs.Restore(Need, fillPerSecond * dt);
        return needs.Get(Need) >= 100f;
    }

    // ── Gizmos (slots + occupancy) ────────────────────────────────
    // One marker per slot + a line to the station, colored by need. In play mode a held slot is
    // drawn solid red so occupancy is visible at a glance.
    private void OnDrawGizmos()
    {
        Color c = Need switch
        {
            NeedType.Health => new Color(0.3f, 0.9f, 0.4f),   // green
            NeedType.Energy => new Color(0.3f, 0.6f, 1f),     // blue
            _               => new Color(1f, 0.5f, 0.7f),     // pink (Affect)
        };

        // No points configured → show the implicit single slot at the transform.
        if (usePoints == null || usePoints.Count == 0)
        {
            Gizmos.color = c;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            return;
        }

        for (int i = 0; i < usePoints.Count; i++)
        {
            var p = usePoints[i];
            if (p == null) continue;

            bool occupied = occupants != null && i < occupants.Length && occupants[i] != null;
            Gizmos.color = occupied ? new Color(1f, 0.3f, 0.3f) : c;
            Gizmos.DrawWireSphere(p.position, 0.3f);
            Gizmos.DrawLine(transform.position, p.position);
            Gizmos.color = new Color(Gizmos.color.r, Gizmos.color.g, Gizmos.color.b, 0.35f);
            Gizmos.DrawSphere(p.position, 0.12f);
        }
    }
}
}
