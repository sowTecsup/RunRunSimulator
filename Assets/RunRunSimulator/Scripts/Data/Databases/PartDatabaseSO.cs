using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

public abstract class PartDatabaseSO<T> : SerializedScriptableObject where T : BodyPart
{
    // Each concrete database provides its prefix: "A", "E", "M", "BS"
    protected abstract string IDPrefix { get; }

    // ── Private Fields ────────────────────────────────────────────

    [Title("Parts Dictionary", "Primary data source — add and edit entries here.")]
    [Searchable]
    [DictionaryDrawerSettings(
        KeyLabel = "ID",
        ValueLabel = "Part",
        DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
    [OdinSerialize]
    [PreviouslySerializedAs("_parts")]
    private Dictionary<string, T> parts = new Dictionary<string, T>();

    // ── Private Methods ───────────────────────────────────────────

#if UNITY_EDITOR
    [Title("Bulk Add")]
    [InfoBox("Arrastra aquí varios assets de una vez y pulsa Populate para añadirlos, sincronizar IDs y generar nombres.")]
    [OdinSerialize, AssetsOnly]
    [ListDrawerSettings(DraggableItems = false, HideAddButton = false, HideRemoveButton = false)]
    private List<T> dropBuffer = new List<T>();

    [Button("Populate from Buffer", ButtonSizes.Large), GUIColor(0.4f, 1f, 0.6f)]
    private void PopulateFromBuffer()
    {
        if (dropBuffer == null || dropBuffer.Count == 0)
        {
            Debug.LogWarning($"[{GetType().Name}] El buffer está vacío — arrastra assets primero.");
            return;
        }

        var existing = new HashSet<T>(parts.Values);
        int added = 0;
        foreach (var part in dropBuffer)
        {
            if (part == null || existing.Contains(part)) continue;
            parts[$"_tmp_{System.Guid.NewGuid():N}"] = part;
            existing.Add(part);
            added++;
        }

        dropBuffer.Clear();

        if (added == 0)
        {
            Debug.LogWarning($"[{GetType().Name}] Todas las partes ya estaban registradas.");
            return;
        }

        SyncAllIDs();
        RollAllNames();
        Debug.Log($"[{GetType().Name}] Populate: {added} partes añadidas, IDs sincronizados, nombres generados.");
    }
#endif

    [ButtonGroup("Admin")]
    [Button("Sync All IDs"), GUIColor(1f, 0.85f, 0.3f)]
    private void SyncAllIDs()
    {
        var orderedParts = parts.Values.Where(p => p != null).ToList();
        parts.Clear();

        for (int i = 0; i < orderedParts.Count; i++)
        {
            string newKey        = $"{IDPrefix}{i}";
            orderedParts[i].ID   = newKey;
            parts[newKey]        = orderedParts[i];
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(orderedParts[i]);
#endif
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log($"[{GetType().Name}] Synced {orderedParts.Count} IDs — prefix '{IDPrefix}'.");
    }

    [ButtonGroup("Admin")]
    [Button("Roll All Names"), GUIColor(0.5f, 0.85f, 1f)]
    private void RollAllNames()
    {
        int count = 0;
        foreach (var part in parts.Values.Where(p => p != null))
        {
            part.Name = PartNameBank.GetRandomName(part.Set, part.GetPartRole());
            count++;
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(part);
#endif
        }
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log($"[{GetType().Name}] Rolled names for {count} parts.");
    }

    // ── Public Methods ────────────────────────────────────────────

    public T GetPartByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        parts.TryGetValue(id, out T part);
        return part;
    }

    public T GetRandomPart(Rarity? rarityFilter = null, PartSet? setFilter = null)
    {
        var pool = parts.Values.Where(p => p != null);

        if (rarityFilter.HasValue) pool = pool.Where(p => p.Rarity == rarityFilter.Value);
        if (setFilter.HasValue)    pool = pool.Where(p => p.Set    == setFilter.Value);

        var list = pool.ToList();
        return list.Count > 0 ? list[Random.Range(0, list.Count)] : null;
    }

    public List<T>      GetBySet(PartSet set) => parts.Values.Where(p => p != null && p.Set == set).ToList();
    public List<string> GetAllIDs()           => parts?.Keys.ToList() ?? new List<string>();

    // ── Getters ───────────────────────────────────────────────────

    public Dictionary<string, T> Parts => parts;

    [ShowInInspector, ReadOnly, LabelText("Total Parts")]
    public int PartCount => parts?.Count ?? 0;

    [Title("Parts Overview")]
    [ShowInInspector, ReadOnly]
    [TableList(AlwaysExpanded = false, DrawScrollView = true, MaxScrollViewHeight = 300)]
    public List<T> PartsTable => parts?.Values.Where(p => p != null).ToList() ?? new List<T>();
}
