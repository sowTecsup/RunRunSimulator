using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Bridges furniture DATA → PRESENCE: rebuilds the placed-furniture meshes from the
// registry, event-driven (mirror of MoriMochiSpawner). Owns only meshes — the
// placement math/occupancy live in PlacementGrid, the flow in FurnitureService.
public class FurnitureSpawner : MonoBehaviour
{
    [SerializeField] private PlacementGrid grid;
    [SerializeField] private FurnitureDatabaseSO database;
    [SerializeField] private FurnitureRegistrySO registry;

    private readonly Dictionary<string, GameObject> spawned = new Dictionary<string, GameObject>();

    private void OnEnable()
    {
        GameEvents.OnFurnitureChanged  += OnChanged;
        GameEvents.OnFurnitureReloaded += OnReloaded;
    }

    private void OnDisable()
    {
        GameEvents.OnFurnitureChanged  -= OnChanged;
        GameEvents.OnFurnitureReloaded -= OnReloaded;
    }

    private void Start()
    {
        if (registry != null) Sync(registry);
    }

    private void OnChanged(FurnitureRegistrySO r)  => Sync(r);
    private void OnReloaded(FurnitureRegistrySO r) => StartCoroutine(ReloadRoutine(r));

    // ClearAll uses Destroy() which is deferred to end-of-frame. Waiting one frame
    // ensures old colliders are gone from the physics world before TrySampleFloor
    // raycasts downward — otherwise it hits the top of a still-alive mesh and spawns
    // every piece elevated by the height of the previous object in that cell.
    private IEnumerator ReloadRoutine(FurnitureRegistrySO r)
    {
        ClearAll();
        yield return null;
        Sync(r);
    }

    // Incremental: spawn newly placed keys, despawn removed ones.
    private void Sync(FurnitureRegistrySO r)
    {
        var all = r.GetAll();

        foreach (var kv in all)
            if (!spawned.ContainsKey(kv.Key)) SpawnOne(kv.Key, kv.Value);

        var stale = spawned.Keys.Where(k => !all.ContainsKey(k)).ToList();
        foreach (var k in stale) Despawn(k);
    }

    private void SpawnOne(string key, PlacedFurniture f)
    {
        var def = database != null ? database.GetById(f.DefId) : null;
        if (def == null || def.Prefab == null)
        {
            Debug.LogError($"[FurnitureSpawner] No def/prefab for '{f.DefId}'.");
            return;
        }
        if (grid == null) { Debug.LogError("[FurnitureSpawner] No grid assigned."); return; }

        // The prefab's ROOT pivot must sit at the footprint center-base (bake it once with the
        // FurniturePivotAligner editor helper); we just place that pivot and rotate around it.
        var anchor = new Vector2Int(f.CellX, f.CellY);
        Vector3 pos = grid.FootprintCenter(anchor, def.Footprint, f.Rotation);
        // Option B: snap the base to the real floor under the cell (irregular terrain). Y is never
        // stored — the terrain is the source of truth, so a piece re-seats if the ground changes.
        if (grid.TrySampleFloor(anchor, def.Footprint, f.Rotation, out float floorY, out _)) pos.y = floorY;
        var go = Instantiate(def.Prefab, pos, Quaternion.Euler(0f, f.Rotation, 0f), transform);
        // Placed furniture is static decoration — any Rigidbody left dynamic would let
        // physics float the piece off the floor (depenetration against the floor collider).
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true))
            rb.isKinematic = true;
        go.name = $"{def.Id}@{key}";
        go.AddComponent<PlacedFurnitureMarker>().AnchorCell = anchor;   // build mode picks pieces by this
        spawned[key] = go;
    }

    private void Despawn(string key)
    {
        if (spawned.TryGetValue(key, out var go) && go != null) Destroy(go);
        spawned.Remove(key);
    }

    private void ClearAll()
    {
        foreach (var go in spawned.Values) if (go != null) Destroy(go);
        spawned.Clear();
    }
}
