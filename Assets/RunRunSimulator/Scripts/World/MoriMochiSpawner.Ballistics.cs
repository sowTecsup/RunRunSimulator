using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

public partial class MoriMochiSpawner
{
    // ── Ballistics ────────────────────────────────────────────────

    // A random landing target on the floor plane, inside spawnRadius of the spawn area.
    private Vector3 RandomLandingPoint()
    {
        Vector3 center = spawnArea != null ? spawnArea.position : transform.position;
        Vector2 disk   = Random.insideUnitCircle * spawnRadius;
        return new Vector3(center.x + disk.x, center.y, center.z + disk.y);
    }

    // Solves the launch velocity to hit 'target' from 'origin' at elevation 'angleRad', accounting
    // for the height difference between muzzle and floor. Classic projectile range equation rearranged
    // for v:  y = d·tanθ − g·d² / (2·v²·cos²θ)  ⇒  v² = g·d² / (2·cos²θ·(d·tanθ − y)).
    // Falls back to a gentle arc if the target is unreachable at this angle (too close/high).
    private Vector3 SolveLaunchVelocity(Vector3 origin, Vector3 target, float angleRad)
    {
        Vector3 flat = target - origin; flat.y = 0f;
        float   d    = flat.magnitude;
        float   y    = target.y - origin.y;          // negative when the floor is below the muzzle
        float   g    = Mathf.Abs(Physics.gravity.y); if (g < 0.01f) g = 9.81f;

        float cos = Mathf.Cos(angleRad), sin = Mathf.Sin(angleRad);
        Vector3 dir;
        if (d > 0.001f) dir = flat / d;
        else { Vector2 r = Random.insideUnitCircle.normalized; dir = new Vector3(r.x, 0f, r.y); }

        float denom = 2f * cos * cos * (d * (sin / cos) - y);
        if (denom <= 0.001f)
        {
            float vFallback = Mathf.Sqrt(g * Mathf.Max(d, 2f));   // safe lob toward the target
            return dir * (vFallback * cos) + Vector3.up * (vFallback * sin);
        }

        float v = Mathf.Sqrt(g * d * d / denom);
        return dir * (v * cos) + Vector3.up * (v * sin);
    }

    // A valid NavMesh point near the cannon, used as the safe activation spot before a launch
    // (NavMeshAgent.OnEnable must fire on-mesh). Samples around the muzzle, then the spawn area.
    private Vector3 ResolveActivationPoint()
    {
        Vector3 probe = launchPoint != null ? launchPoint.position
                      : spawnArea  != null ? spawnArea.position
                      : transform.position;
        if (NavMesh.SamplePosition(probe, out var hit, 50f, NavMesh.AllAreas)) return hit.position;
        if (spawnArea != null && NavMesh.SamplePosition(spawnArea.position, out var hit2, 50f, NavMesh.AllAreas))
            return hit2.position;
        return probe;
    }

    private static Transform ResolvePlayer()
    {
        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null) return tagged.transform;
        return Camera.main != null ? Camera.main.transform : null;
    }
}
