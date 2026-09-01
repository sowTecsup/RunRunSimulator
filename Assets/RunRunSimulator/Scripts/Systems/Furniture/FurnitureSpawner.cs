using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace MoriMonchiSimulator
{

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

    private IEnumerator ReloadRoutine(FurnitureRegistrySO r)
    {
        ClearAll();
        yield return null;
        Sync(r);
    }

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
        var def = database != null ? database.GetByID(f.DefId) : null;
        if (def == null || def.Prefab == null)
        {
            Debug.LogError($"[FurnitureSpawner] No def/prefab for '{f.DefId}'.");
            return;
        }
        if (grid == null) { Debug.LogError("[FurnitureSpawner] No grid assigned."); return; }

        var anchor = new Vector2Int(f.CellX, f.CellY);
        Vector3 pos = grid.FootprintCenter(anchor, def.Footprint, f.Rotation);
        if (grid.TrySampleFloor(anchor, def.Footprint, f.Rotation, out float floorY, out _)) pos.y = floorY;
        var go = Instantiate(def.Prefab, pos, Quaternion.Euler(0f, f.Rotation, 0f), transform);
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true))
            rb.isKinematic = true;
        go.name = $"{def.Id}@{key}";
        go.AddComponent<PlacedFurnitureMarker>().AnchorCell = anchor;
        foreach (var c in go.GetComponentsInChildren<MoriMochiContainer>(true))
            c.SetAnchorKey($"{anchor.x}_{anchor.y}");
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
}
