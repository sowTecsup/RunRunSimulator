using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

public class StoreContainer : MoriMochiContainer
{
    [SerializeField, Min(0f)]
    [Title("Store Display")]
    private float restoreRate = 25f;

    [Tooltip("Stand-here points for browsing NPCs (one customer per point, snapped onto the NavMesh). Several let customers browse from any reachable side without overlapping. Empty → a single implicit slot at this transform.")]
    [SerializeField] private List<Transform> usePoints = new List<Transform>();

    private NpcAgent[] occupants;
    private int SlotCount => (usePoints != null && usePoints.Count > 0) ? usePoints.Count : 1;

    private void EnsureSlots()
    {
        if (occupants == null || occupants.Length != SlotCount)
            occupants = new NpcAgent[SlotCount];
    }

    private Vector3 SlotPosition(int i) =>
        (usePoints != null && i < usePoints.Count && usePoints[i] != null) ? usePoints[i].position : transform.position;

    public bool HasFreeUsePoint
    {
        get { EnsureSlots(); foreach (var o in occupants) if (o == null) return true; return false; }
    }

    private void OnEnable()  { EnsureSlots(); StoreDisplayRegistry.Register(this); }
    private void OnDisable() => StoreDisplayRegistry.Unregister(this);

    private void Update()
    {
        float delta = restoreRate * Time.deltaTime;
        for (int i = 0; i < Occupants.Count; i++)
        {
            var dna = Occupants[i]?.DNA;
            if (dna == null) continue;
            dna.Needs.AddHealth(delta);
            dna.Needs.AddEnergy(delta);
            dna.Needs.AddAffect(delta);
        }
    }

    public bool TryReserveUsePoint(NpcAgent agent, Vector3 from, int areaMask, float sampleRadius, out Vector3 usePos)
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

    public void ReleaseUsePoint(NpcAgent agent)
    {
        if (occupants == null) return;
        for (int i = 0; i < occupants.Length; i++)
            if (occupants[i] == agent) occupants[i] = null;
    }

    private void OnDrawGizmos()
    {
        if (usePoints == null) return;
        for (int i = 0; i < usePoints.Count; i++)
        {
            var p = usePoints[i];
            if (p == null) continue;
            bool occupied = occupants != null && i < occupants.Length && occupants[i] != null;
            Gizmos.color = occupied ? new Color(1f, 0.3f, 0.3f) : new Color(0.95f, 0.8f, 0.3f);
            Gizmos.DrawWireSphere(p.position, 0.3f);
            Gizmos.DrawLine(transform.position, p.position);
        }
    }
}
}
