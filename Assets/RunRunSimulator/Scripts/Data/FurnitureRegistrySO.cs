using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

// The placed-furniture instances (mirror of CreatureRegistrySO). Keyed by anchor
// cell so a cell holds at most one piece. Source of truth for the world — the
// FurnitureSpawner rebuilds meshes from here, occupancy lives in PlacementGrid.
[CreateAssetMenu(menuName = "RunRunSimulator/Furniture Registry")]
public class FurnitureRegistrySO : SerializedScriptableObject
{
    [InfoBox("Reflejo del estado de muebles colocados. Mutar vía FurnitureService, no a mano.", InfoMessageType.Warning)]
    [OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel = "CellKey", ValueLabel = "Placed",
        DisplayMode = DictionaryDisplayOptions.CollapsedFoldout)]
    private Dictionary<string, PlacedFurniture> placed = new Dictionary<string, PlacedFurniture>();

    public bool Place(PlacedFurniture f)
    {
        if (f == null || string.IsNullOrEmpty(f.DefId))
        {
            Debug.LogError("[FurnitureRegistry] Cannot place: null piece or empty DefId.");
            return false;
        }
        if (placed.ContainsKey(f.CellKey))
        {
            Debug.LogWarning($"[FurnitureRegistry] Cell {f.CellKey} already occupied.");
            return false;
        }
        placed[f.CellKey] = f;
        MarkDirty();
        return true;
    }

    public bool RemoveAt(string cellKey)
    {
        if (!placed.Remove(cellKey)) return false;
        MarkDirty();
        return true;
    }

    public bool TryGet(string cellKey, out PlacedFurniture f) => placed.TryGetValue(cellKey, out f);

    public Dictionary<string, PlacedFurniture> GetAll() => new Dictionary<string, PlacedFurniture>(placed);

    public void LoadFrom(Dictionary<string, PlacedFurniture> data)
    {
        placed = data ?? new Dictionary<string, PlacedFurniture>();
        MarkDirty();
    }

    public int Count => placed.Count;

    private void MarkDirty()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
