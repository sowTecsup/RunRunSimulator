using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

// Indexes every EquipmentSO by ID ("EQ0", "EQ1"…). Mirror of PartDatabaseSO: the
// single source the rest of the game resolves equipment IDs against (load, cloud,
// grid, the DNA drag-drop). Assign it on GameManager like the other databases.
[CreateAssetMenu(fileName = "EquipmentDatabase", menuName = "RunRunSimulator/Databases/Equipment Database")]
public class EquipmentDatabaseSO : SerializedScriptableObject
{
    private const string IDPrefix = "EQ";

    [Title("Equipment Dictionary", "Primary data source — add and edit entries here.")]
    [Searchable]
    [DictionaryDrawerSettings(KeyLabel = "ID", ValueLabel = "Item",
        DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
    [OdinSerialize]
    private Dictionary<string, EquipmentSO> equipment = new Dictionary<string, EquipmentSO>();

#if UNITY_EDITOR
    [Title("Bulk Add")]
    [InfoBox("Arrastra aquí varios EquipmentSO y pulsa Populate para añadirlos y sincronizar IDs.")]
    [OdinSerialize, AssetsOnly]
    [ListDrawerSettings(DraggableItems = false, HideAddButton = false, HideRemoveButton = false)]
    private List<EquipmentSO> dropBuffer = new List<EquipmentSO>();

    [Button("Populate from Buffer", ButtonSizes.Large), GUIColor(0.4f, 1f, 0.6f)]
    private void PopulateFromBuffer()
    {
        if (dropBuffer == null || dropBuffer.Count == 0)
        {
            Debug.LogWarning("[EquipmentDatabaseSO] El buffer está vacío — arrastra assets primero.");
            return;
        }

        var existing = new HashSet<EquipmentSO>(equipment.Values);
        int added = 0;
        foreach (var item in dropBuffer)
        {
            if (item == null || existing.Contains(item)) continue;
            equipment[$"_tmp_{System.Guid.NewGuid():N}"] = item;
            existing.Add(item);
            added++;
        }

        dropBuffer.Clear();

        if (added == 0)
        {
            Debug.LogWarning("[EquipmentDatabaseSO] Todos los objetos ya estaban registrados.");
            return;
        }

        SyncAllIDs();
        Debug.Log($"[EquipmentDatabaseSO] Populate: {added} objetos añadidos, IDs sincronizados.");
    }

    [Button("Sync All IDs"), GUIColor(1f, 0.85f, 0.3f)]
    private void SyncAllIDs()
    {
        var ordered = equipment.Values.Where(e => e != null).ToList();
        equipment.Clear();

        for (int i = 0; i < ordered.Count; i++)
        {
            string newKey = $"{IDPrefix}{i}";
            ordered[i].ID = newKey;
            equipment[newKey] = ordered[i];
            UnityEditor.EditorUtility.SetDirty(ordered[i]);
        }

        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[EquipmentDatabaseSO] Synced {ordered.Count} IDs — prefix '{IDPrefix}'.");
    }

    private static EquipmentDatabaseSO editorInstance;

    // Editor-only resolver so CreatureDNA's drag-drop can turn an equipped ID back
    // into its asset without a live GameManager (e.g. editing in the registry asset).
    public static EquipmentDatabaseSO Editor
    {
        get
        {
            if (editorInstance != null) return editorInstance;
            var guids = UnityEditor.AssetDatabase.FindAssets("t:EquipmentDatabaseSO");
            if (guids.Length > 0)
                editorInstance = UnityEditor.AssetDatabase.LoadAssetAtPath<EquipmentDatabaseSO>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
            return editorInstance;
        }
    }
#endif

    public EquipmentSO GetByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        equipment.TryGetValue(id, out var item);
        return item;
    }

    public List<EquipmentSO> GetBySlot(EquipmentSlot slot) =>
        equipment.Values.Where(e => e != null && e.Slot == slot).ToList();

    public List<string> GetAllIDs() => equipment?.Keys.ToList() ?? new List<string>();

    public Dictionary<string, EquipmentSO> Equipment => equipment;

    [ShowInInspector, ReadOnly, LabelText("Total Equipment")]
    public int EquipmentCount => equipment?.Count ?? 0;
}
}
