using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

// Dictionary-keyed catalog of furniture definitions (mirror of PartDatabaseSO): the
// DICTIONARY KEY is the canonical id — a definition never authors its own id, the slot
// it lives in dictates it. "Validate & Sync IDs" reindexes every entry and stamps the
// key back onto the def's Id, so ids can never drift or collide and stay '-' free. The
// spawner resolves a placed piece's def here by id.
[CreateAssetMenu(fileName = "FurnitureDatabase", menuName = "RunRunSimulator/Furniture Database")]
public class FurnitureDatabaseSO : SerializedScriptableObject
{
    // Namespaces furniture ids and keeps them '-' free (the save/DNA separator), like
    // the single-letter prefixes the part databases use ("A"/"E"/"M"/"BS").
    private const string IDPrefix = "F";

    [Title("Furniture Dictionary", "Primary data source — the key IS the id; the value is the definition.")]
    [Searchable]
    [DictionaryDrawerSettings(
        KeyLabel = "ID",
        ValueLabel = "Definition",
        DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
    [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    [OdinSerialize]
    private Dictionary<string, FurnitureDefinitionSO> items = new Dictionary<string, FurnitureDefinitionSO>();

    // ── Public API ────────────────────────────────────────────────

    public FurnitureDefinitionSO GetById(string id)
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
    [InfoBox("Arrastra varias Furniture Definition y pulsa Populate para añadirlas y sincronizar ids.")]
    [OdinSerialize, AssetsOnly]
    [ListDrawerSettings(DraggableItems = false)]
    private List<FurnitureDefinitionSO> dropBuffer = new List<FurnitureDefinitionSO>();

    [Button("Populate from Buffer", ButtonSizes.Large), GUIColor(0.4f, 1f, 0.6f)]
    private void PopulateFromBuffer()
    {
        if (dropBuffer == null || dropBuffer.Count == 0)
        {
            Debug.LogWarning("[FurnitureDB] El buffer está vacío — arrastra definiciones primero.");
            return;
        }

        var existing = new HashSet<FurnitureDefinitionSO>(items.Values);
        int added = 0;
        foreach (var def in dropBuffer)
        {
            if (def == null || existing.Contains(def)) continue;
            items[$"_tmp_{System.Guid.NewGuid():N}"] = def;
            existing.Add(def);
            added++;
        }

        dropBuffer.Clear();

        if (added == 0) { Debug.LogWarning("[FurnitureDB] Todas las definiciones ya estaban registradas."); return; }

        SyncIds();
        Debug.Log($"[FurnitureDB] Populate: {added} definición(es) añadidas.");
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
        Debug.Log($"[FurnitureDB] Synced {ordered.Count} id(s) — prefix '{IDPrefix}'.");
    }
}
