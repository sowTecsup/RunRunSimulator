using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

public partial class MoriMochiSpawner
{
    // ── Pool ──────────────────────────────────────────────────────

    private MoriMonchiController GetController(Vector3 pos)
    {
        MoriMonchiController controller = null;
        while (controller == null && pool.Count > 0) controller = pool.Dequeue();

        if (controller == null)
        {
            if (creaturePrefab == null)
            {
                Debug.LogError("[MoriMochiSpawner] No creature prefab assigned.");
                return null;
            }
            return Instantiate(creaturePrefab, pos, Quaternion.identity);
        }

        controller.transform.SetPositionAndRotation(pos, Quaternion.identity);
        controller.gameObject.SetActive(true);
        return controller;
    }

    private void ReturnToPool(MoriMonchiController controller)
    {
        if (controller == null) return;
        controller.PrepareForPool();
        controller.gameObject.SetActive(false);
        pool.Enqueue(controller);
    }

    private void Despawn(string id)
    {
        if (spawned.TryGetValue(id, out var controller)) ReturnToPool(controller);
        spawned.Remove(id);
        birthLaunchPoints.Remove(id);

        // A creature that died before activation — return its prewarmed slot to the pool.
        if (prewarmed.TryGetValue(id, out var prewarmedController))
        {
            ReturnToPool(prewarmedController);
            prewarmed.Remove(id);
        }
    }

    private void ClearAll()
    {
        foreach (var controller in spawned.Values) ReturnToPool(controller);
        spawned.Clear();
        spawnQueue.Clear();
        breederQueue.Clear();
        queued.Clear();
    }
}
