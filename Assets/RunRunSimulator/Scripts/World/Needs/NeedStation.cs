using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

public abstract class NeedStation : MonoBehaviour
{
    [Tooltip("Stand-here points (= slots). Capacity is how many of these there are; each serving agent holds one. Several let agents use it from any reachable side regardless of the furniture's rotation. Empty → one slot at this transform.")]
    [SerializeField] private List<Transform> usePoints = new List<Transform>();
    [Tooltip("Stat points restored per second while an agent is using it (continuous, fills to 100).")]
    [SerializeField, Min(0f)] private float fillPerSecond = 25f;

    public abstract NeedType Need { get; }

    private MoriMochiAgent[] occupants;
    private int SlotCount => (usePoints != null && usePoints.Count > 0) ? usePoints.Count : 1;

    private void EnsureSlots()
    {
        if (occupants == null || occupants.Length != SlotCount)
            occupants = new MoriMochiAgent[SlotCount];
    }

    private Vector3 SlotPosition(int i) =>
        (usePoints != null && i < usePoints.Count && usePoints[i] != null) ? usePoints[i].position : transform.position;

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

    public bool TryReserve(MoriMochiAgent agent, Vector3 from, int areaMask, float sampleRadius, out Vector3 usePos)
    {
        usePos = transform.position;
        EnsureSlots();

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

    public bool Refill(NeedsState needs, float dt)
    {
        needs.Restore(Need, fillPerSecond * dt);
        return needs.Get(Need) >= 100f;
    }

    private void OnDrawGizmos()
    {
        Color c = Need switch
        {
            NeedType.Health => new Color(0.3f, 0.9f, 0.4f),
            NeedType.Energy => new Color(0.3f, 0.6f, 1f),
            _               => new Color(1f, 0.5f, 0.7f),
        };

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
