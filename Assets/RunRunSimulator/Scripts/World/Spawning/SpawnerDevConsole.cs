using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

public class SpawnerDevConsole : MonoBehaviour
{
    [BoxGroup("Setup"), Required]
    [SerializeField] private MoriMochiSpawner spawner;

    private readonly List<GameObject> debugShots = new List<GameObject>();

    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private int DebugShotCount => debugShots.Count;

    [Button("Respawn All", ButtonSizes.Large), GUIColor(0.55f, 1f, 0.7f)]
    private void RespawnAll()
    {
        if (spawner == null) { Debug.LogError("[SpawnerDevConsole] Spawner not assigned."); return; }
        if (!Application.isPlaying) { Debug.LogWarning("[SpawnerDevConsole] Enter Play mode to respawn."); return; }
        spawner.ClearAll();
        if (GameManager.Instance != null) spawner.Sync(GameManager.Instance.Registry);
    }

    [Button("Fire Debug Shot"), GUIColor(1f, 0.85f, 0.35f)]
    private void FireDebugShot()
    {
        if (spawner == null) { Debug.LogError("[SpawnerDevConsole] Spawner not assigned."); return; }
        if (!Application.isPlaying) { Debug.LogWarning("[SpawnerDevConsole] Enter Play mode."); return; }
        if (spawner.CreaturePrefab == null) return;

        Vector3 muzzle = spawner.MuzzlePosition;
        var go  = Instantiate(spawner.CreaturePrefab.gameObject, muzzle, Quaternion.identity);
        go.name = "DebugShot";

        var nav = go.GetComponent<NavMeshAgent>();
        if (nav != null) nav.enabled = false;

        var rb = go.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = false; rb.useGravity = true; rb.linearDamping = 1.2f; rb.angularDamping = 2f; }

        var col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        Vector3 target = spawner.RandomLandingPoint();
        Vector2 angleRange = spawner.LaunchAngleRange;
        float   angle  = Random.Range(angleRange.x, angleRange.y) * Mathf.Deg2Rad;
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
        if (spawner == null) { Debug.LogError("[SpawnerDevConsole] Spawner not assigned."); return; }
        if (!Application.isPlaying) { Debug.LogWarning("[SpawnerDevConsole] Enter Play mode."); return; }
        Debug.Log($"[SpawnerDevConsole] spawned={spawner.SpawnedCount}  queued={spawner.QueuedCount}  " +
                  $"prewarmed={spawner.PrewarmedCount}  pooled={spawner.PooledCount}  worldReady={spawner.WorldReady}");
        foreach (var kv in spawner.SpawnedEntries)
            Debug.Log($"  • {kv.Key} → {(kv.Value != null ? kv.Value.name : "null")}");
    }
}
}
