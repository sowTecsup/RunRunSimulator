using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

// Dictionary-keyed catalog of item definitions (mirror of FurnitureDatabaseSO): the
// DICTIONARY KEY is the canonical id — a definition never authors its own id, the slot
// it lives in dictates it. "Validate & Sync IDs" reindexes every entry and stamps the
// key back onto the def's Id, so ids can never drift or collide and stay '-' free.
// StorageContainer / DeliveryBox resolve a stored prop's def here by id.
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "RunRunSimulator/Item Database")]
public class ItemDatabaseSO : SerializedScriptableObject
{
    // Namespaces item ids and keeps them '-' free (the save/DNA separator). Distinct
    // from furniture's "F" prefix so the two id spaces never collide.
    private const string IDPrefix = "I";

    [Title("Item Dictionary", "Primary data source — the key IS the id; the value is the definition.")]
    [Searchable]
    [DictionaryDrawerSettings(
        KeyLabel = "ID",
        ValueLabel = "Definition",
        DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
    [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    [OdinSerialize]
    private Dictionary<string, ItemDefinitionSO> items = new Dictionary<string, ItemDefinitionSO>();

    // ── Public API ────────────────────────────────────────────────

    public ItemDefinitionSO GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        items.TryGetValue(id, out var def);
        return def;
    }

    [ShowInInspector, ReadOnly, LabelText("Total")]
    public int Count => items?.Count ?? 0;

    // ── Editor: bulk add + id sync ────────────────────────────────

#if UNITY_EDITOR
    [Title("Bulk Add")]
    [InfoBox("Arrastra varias Item Definition y pulsa Populate para añadirlas y sincronizar ids.")]
    [OdinSerialize, AssetsOnly]
    [ListDrawerSettings(DraggableItems = false)]
    private List<ItemDefinitionSO> dropBuffer = new List<ItemDefinitionSO>();

    [Button("Populate from Buffer", ButtonSizes.Large), GUIColor(0.4f, 1f, 0.6f)]
    private void PopulateFromBuffer()
    {
        if (dropBuffer == null || dropBuffer.Count == 0)
        {
            Debug.LogWarning("[ItemDB] El buffer está vacío — arrastra definiciones primero.");
            return;
        }

        var existing = new HashSet<ItemDefinitionSO>(items.Values);
        int added = 0;
        foreach (var def in dropBuffer)
        {
            if (def == null || existing.Contains(def)) continue;
            items[$"_tmp_{System.Guid.NewGuid():N}"] = def;
            existing.Add(def);
            added++;
        }

        dropBuffer.Clear();

        if (added == 0) { Debug.LogWarning("[ItemDB] Todas las definiciones ya estaban registradas."); return; }

        SyncIds();
        Debug.Log($"[ItemDB] Populate: {added} definición(es) añadidas.");
    }
#endif

    // The key dictates the id: reindex every entry to '{prefix}{i}' and stamp that id
    // back onto the def. Unique keys + a '-' free prefix make duplicate/illegal ids
    // impossible by construction — that's what "validate" means here.
    [Button("Validate & Sync IDs", ButtonSizes.Large), GUIColor(1f, 0.8f, 0.2f)]
    private void SyncIds()
    {
        var ordered = items.Values.Where(d => d != null).ToList();
        items.Clear();

        for (int i = 0; i < ordered.Count; i++)
        {
            string key    = $"{IDPrefix}{i}";
            ordered[i].Id = key;
            items[key]    = ordered[i];
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(ordered[i]);
#endif
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log($"[ItemDB] Synced {ordered.Count} id(s) — prefix '{IDPrefix}'.");
    }
}
