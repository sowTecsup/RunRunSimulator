using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

[CreateAssetMenu(menuName = "RunRunSimulator/Creature Registry")]
public class CreatureRegistrySO : SerializedScriptableObject
{
    [InfoBox("Reflejo visual del JSON — no editar manualmente. Usar Sync para recargar desde creature_database.json.", InfoMessageType.Warning)]
    [OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel = "UniqueID", ValueLabel = "DNA",
        DisplayMode = DictionaryDisplayOptions.CollapsedFoldout)]
    private Dictionary<string, CreatureDNA> _creatures = new Dictionary<string, CreatureDNA>();

    public int Count => _creatures.Count;

    public bool Register(CreatureDNA dna)
    {
        if (dna == null || string.IsNullOrEmpty(dna.UniqueID))
        {
            Debug.LogError("[CreatureRegistrySO] Cannot register: DNA is null or not stamped. Call Stamp() first.");
            return false;
        }
        if (_creatures.ContainsKey(dna.UniqueID))
        {
            Debug.LogWarning($"[CreatureRegistrySO] ID collision — '{dna.UniqueID}' already registered.");
            return false;
        }
        _creatures[dna.UniqueID] = dna;
        MarkDirty();
        return true;
    }

    public bool TryGet(string uniqueID, out CreatureDNA dna) =>
        _creatures.TryGetValue(uniqueID, out dna);

    public Dictionary<string, CreatureDNA> GetAll() =>
        new Dictionary<string, CreatureDNA>(_creatures);

    public void LoadFrom(Dictionary<string, CreatureDNA> data)
    {
        _creatures = data ?? new Dictionary<string, CreatureDNA>();
        MarkDirty();
    }

    private void MarkDirty()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

#if UNITY_EDITOR
    // ── Copy IDs ──────────────────────────────────────────────────

    [System.Serializable]
    private class IDEntry
    {
        [DisplayAsString, HideLabel, HorizontalGroup]
        public string id;

        [HorizontalGroup(Width = 55), Button("Copy"), GUIColor(0.6f, 0.9f, 1f)]
        private void CopyToClipboard() => GUIUtility.systemCopyBuffer = id;
    }

    [Title("Registered IDs")]
    [ShowInInspector]
    [ListDrawerSettings(HideAddButton = true, HideRemoveButton = true, DraggableItems = false)]
    private List<IDEntry> IDEntries
    {
        get => _creatures.Keys.Select(k => new IDEntry { id = k }).ToList();
        set { }
    }

    // ── Sync ──────────────────────────────────────────────────────

    [Button("Sync from JSON", ButtonSizes.Large), GUIColor(0.5f, 0.85f, 1f)]
    private void SyncFromJson() => SaveSystem.LoadInto(this);
#endif
}
