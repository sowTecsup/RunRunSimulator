using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

public partial class MoriMochiSpawner
{
    // ── Dev ───────────────────────────────────────────────────────

    [Button("Respawn All", ButtonSizes.Large), GUIColor(0.55f, 1f, 0.7f)]
    private void RespawnAll()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[MoriMochiSpawner] Enter Play mode to respawn."); return; }
        ClearAll();
        if (GameManager.Instance != null) Sync(GameManager.Instance.Registry);
    }

    // Fires a single plain-physics projectile from the cannon: no NavMeshAgent, no AI, no DNA.
    // Use this in Play mode to verify the arc and landing zone without NavMesh interference.
    [Button("Fire Debug Shot"), GUIColor(1f, 0.85f, 0.35f)]
    private void FireDebugShot()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[MoriMochiSpawner] Enter Play mode."); return; }
        if (creaturePrefab == null) return;

        Vector3 muzzle = launchPoint != null ? launchPoint.position : transform.position;
        var go  = Instantiate(creaturePrefab.gameObject, muzzle, Quaternion.identity);
        go.name = "DebugShot";

        var nav = go.GetComponent<NavMeshAgent>();
        if (nav != null) nav.enabled = false;

        var rb = go.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = false; rb.useGravity = true; rb.linearDamping = 1.2f; rb.angularDamping = 2f; }

        var col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        Vector3 target = RandomLandingPoint();
        float   angle  = Random.Range(launchAngle.x, launchAngle.y) * Mathf.Deg2Rad;
        if (rb != null) rb.AddForce(SpawnBallistics.SolveLaunchVelocity(muzzle, target, angle), ForceMode.VelocityChange);

        debugShots.Add(go);
    }

    [Button("Clear Debug Shots"), GUIColor(1f, 0.5f, 0.5f)]
    private void ClearDebugShots()
    {
        foreach (var s in debugShots) if (s != null) Destroy(s);
        debugShots.Clear();
    }

    [Button("Dump Spawn State"), GUIColor(0.6f, 0.8f, 1f)]
    private void DumpSpawnState()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[MoriMochiSpawner] Enter Play mode."); return; }
        Debug.Log($"[MoriMochiSpawner] spawned={spawned.Count}  queued={spawnQueue.Count + anchoredQueue.Count}  " +
                  $"prewarmed={prewarmed.Count}  pooled={controllerPool?.Count ?? 0}  worldReady={worldReady}");
        foreach (var kv in spawned)
            Debug.Log($"  • {kv.Key} → {(kv.Value != null ? kv.Value.name : "null")}");
    }

    // ── Gizmos ───────────────────────────────────────────────────

    // Always-visible: muzzle, landing zone, and the line between them.
    private void OnDrawGizmos()
    {
        Vector3 muzzle = launchPoint != null ? launchPoint.position : transform.position;
        Vector3 center = spawnArea  != null ? spawnArea.position    : transform.position;

        Gizmos.color = new Color(1f, 0.85f, 0f);
        Gizmos.DrawWireSphere(muzzle, 0.3f);

        Gizmos.color = new Color(0.4f, 1f, 0.5f);
        DrawRing(center, spawnRadius, 48);

        Gizmos.color = new Color(1f, 0.85f, 0f, 0.4f);
        Gizmos.DrawLine(muzzle, center);
    }

    // Selected: simulated trajectories to the ring edge, at both the min (cyan) and max (orange)
    // elevation, in 8 directions — shows the real arcs the cannon produces.
    private void OnDrawGizmosSelected()
    {
        Vector3 muzzle = launchPoint != null ? launchPoint.position : transform.position;
        Vector3 center = spawnArea  != null ? spawnArea.position    : transform.position;
        float   g      = Mathf.Abs(Physics.gravity.y); if (g < 0.01f) g = 9.81f;

        for (int d = 0; d < 8; d++)
        {
            float   a    = d * (Mathf.PI / 4f);
            Vector3 edge = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * spawnRadius;

            Gizmos.color = new Color(1f, 0.55f, 0.15f, 0.6f);
            DrawSimulatedArc(muzzle, SpawnBallistics.SolveLaunchVelocity(muzzle, edge, launchAngle.y * Mathf.Deg2Rad), center.y, g);

            Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.5f);
            DrawSimulatedArc(muzzle, SpawnBallistics.SolveLaunchVelocity(muzzle, edge, launchAngle.x * Mathf.Deg2Rad), center.y, g);
        }
    }

    // Steps a projectile (gravity only — ignores drag) and draws it until it drops back to groundY.
    private static void DrawSimulatedArc(Vector3 origin, Vector3 vel, float groundY, float g)
    {
        const int   steps = 48;
        const float dt    = 0.04f;
        Vector3 p = origin, v = vel, prev = origin;
        for (int i = 0; i < steps; i++)
        {
            v += Vector3.down * g * dt;
            p += v * dt;
            Gizmos.DrawLine(prev, p);
            prev = p;
            if (p.y <= groundY && i > 1) break;
        }
    }

    private static void DrawRing(Vector3 center, float radius, int segments)
    {
        if (radius < 0.05f) return;
        float   step = 2f * Mathf.PI / segments;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float   a    = i * step;
            Vector3 curr = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            Gizmos.DrawLine(prev, curr);
            prev = curr;
        }
    }
}
}
