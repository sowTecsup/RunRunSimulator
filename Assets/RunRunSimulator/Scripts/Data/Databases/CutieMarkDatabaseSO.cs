using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "CutieMarkDatabase", menuName = "RunRunSimulator/Databases/Cutie Mark Database")]
public class CutieMarkDatabaseSO : SerializedScriptableObject
{
    private const string IDPrefix = "CM";

    [Title("Cutie Mark Dictionary", "Primary data source — the key IS the id; the value is the definition.")]
    [Searchable]
    [DictionaryDrawerSettings(
        KeyLabel = "ID",
        ValueLabel = "Cutie Mark",
        DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
    [InlineEditor(InlineEditorObjectFieldModes.Foldout)]
    [OdinSerialize]
    private Dictionary<string, CutieMarkSO> cutieMarks = new Dictionary<string, CutieMarkSO>();

    // ── Public API ────────────────────────────────────────────────

    public CutieMarkSO GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        cutieMarks.TryGetValue(id, out var mark);
        return mark;
    }

    [ShowInInspector, ReadOnly, LabelText("Total")]
    public int Count => cutieMarks?.Count ?? 0;

    // ── Editor: bulk add + id sync ────────────────────────────────

#if UNITY_EDITOR
    [Title("Bulk Add")]
    [InfoBox("Arrastra varias Cutie Mark y pulsa Populate para añadirlas y sincronizar ids.")]
    [OdinSerialize, AssetsOnly]
    [ListDrawerSettings(DraggableItems = false)]
    private List<CutieMarkSO> dropBuffer = new List<CutieMarkSO>();

    [Button("Populate from Buffer", ButtonSizes.Large), GUIColor(0.4f, 1f, 0.6f)]
    private void PopulateFromBuffer()
    {
        if (dropBuffer == null || dropBuffer.Count == 0)
        {
            Debug.LogWarning("[CutieMarkDB] El buffer está vacío — arrastra definiciones primero.");
            return;
        }

        var existing = new HashSet<CutieMarkSO>(cutieMarks.Values);
        int added = 0;
        foreach (var mark in dropBuffer)
        {
            if (mark == null || existing.Contains(mark)) continue;
            cutieMarks[$"_tmp_{System.Guid.NewGuid():N}"] = mark;
            existing.Add(mark);
            added++;
        }

        dropBuffer.Clear();

        if (added == 0) { Debug.LogWarning("[CutieMarkDB] Todas las definiciones ya estaban registradas."); return; }

        SyncIds();
        Debug.Log($"[CutieMarkDB] Populate: {added} definición(es) añadidas.");
    }
#endif

    [Button("Validate & Sync IDs", ButtonSizes.Large), GUIColor(1f, 0.8f, 0.2f)]
    private void SyncIds()
    {
        var ordered = cutieMarks.Values.Where(d => d != null).ToList();
        cutieMarks.Clear();

        for (int i = 0; i < ordered.Count; i++)
        {
            string key   = $"{IDPrefix}{i}";
            ordered[i].Id = key;
            cutieMarks[key] = ordered[i];
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(ordered[i]);
#endif
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log($"[CutieMarkDB] Synced {ordered.Count} id(s) — prefix '{IDPrefix}'.");
    }
}
}
