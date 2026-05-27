using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

// Visual read-only registry of all registered MoriMonchis.
// JSON (creature_database.json) is the sole source of truth — this SO is populated at runtime.
// Fields are [ReadOnly] so the inspector shows data but users cannot edit it accidentally.
[CreateAssetMenu(menuName = "RunRunSimulator/Creature Registry")]
public class CreatureRegistrySO : SerializedScriptableObject
{
    [InfoBox("Read-only — populated at runtime from creature_database.json. Do not edit manually.")]
    [OdinSerialize, ReadOnly]
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
}
