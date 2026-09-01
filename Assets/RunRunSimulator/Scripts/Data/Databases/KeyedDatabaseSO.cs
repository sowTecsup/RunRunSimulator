using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

public abstract class KeyedDatabaseSO<T> : SerializedScriptableObject where T : ScriptableObject
{
    protected abstract Dictionary<string, T> Entries { get; }

    protected abstract string IDPrefix { get; }

    protected abstract void SetEntryID(T entry, string id);

    protected virtual void OnPopulated(int added) { }

#if UNITY_EDITOR
    [Title("Bulk Add")]
    [InfoBox("Arrastra aquí varios assets de una vez y pulsá Populate para añadirlos y sincronizar IDs.")]
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

        var existing = new HashSet<T>(Entries.Values);
        int added = 0;
        foreach (var entry in dropBuffer)
        {
            if (entry == null || existing.Contains(entry)) continue;
            Entries[$"_tmp_{System.Guid.NewGuid():N}"] = entry;
            existing.Add(entry);
            added++;
        }

        dropBuffer.Clear();

        if (added == 0)
        {
            Debug.LogWarning($"[{GetType().Name}] Todas las entradas ya estaban registradas.");
            return;
        }

        SyncAllIDs();
        OnPopulated(added);
        Debug.Log($"[{GetType().Name}] Populate: {added} entradas añadidas, IDs sincronizados.");
    }
#endif

    [ButtonGroup("Admin")]
    [Button("Sync All IDs"), GUIColor(1f, 0.85f, 0.3f)]
    private void SyncAllIDs()
    {
        var ordered = Entries.Values.Where(e => e != null).ToList();
        Entries.Clear();

        for (int i = 0; i < ordered.Count; i++)
        {
            string newKey = $"{IDPrefix}{i}";
            SetEntryID(ordered[i], newKey);
            Entries[newKey] = ordered[i];
#if UNITY_EDITOR
            UnityEditor.EditorUtility.SetDirty(ordered[i]);
#endif
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
        Debug.Log($"[{GetType().Name}] Synced {ordered.Count} IDs — prefix '{IDPrefix}'.");
    }

    public T GetByID(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        Entries.TryGetValue(id, out T entry);
        return entry;
    }

    public List<string> GetAllIDs() => Entries?.Keys.ToList() ?? new List<string>();

    [ShowInInspector, ReadOnly, LabelText("Total")]
    public int Count => Entries?.Count ?? 0;
}
}
