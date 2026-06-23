using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

[Serializable]
public class QueueAvailabilityHandler
{
    public bool IsAvailable(Vector3 from, Vector3 candidate, int areaMask, float sampleRadius, float maxSnap,
                            IReadOnlyList<Vector3> occupied, float minSeparation, out Vector3 snapped)
    {
        snapped = candidate;

        if (!NavMesh.SamplePosition(candidate, out var hit, sampleRadius, areaMask)) return false;
        snapped = hit.position;

        if ((snapped - candidate).sqrMagnitude > maxSnap * maxSnap) return false;

        if (NavMesh.Raycast(from, snapped, out _, areaMask)) return false;

        float minSqr = minSeparation * minSeparation;
        for (int i = 0; i < occupied.Count; i++)
            if ((occupied[i] - snapped).sqrMagnitude < minSqr) return false;

        return true;
    }
}
}
