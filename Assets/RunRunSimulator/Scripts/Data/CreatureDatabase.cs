using System.Collections.Generic;
using UnityEngine;

// In-memory registry of all known MoriMonchis, keyed by CreatureDNA.UniqueID.
// Not a Unity object — lives as a plain C# instance owned by GameManager.
// Persistence is handled by SaveSystem.
public class CreatureDatabase
{
    private readonly Dictionary<string, CreatureDNA> _registry = new Dictionary<string, CreatureDNA>();

    public int Count => _registry.Count;

    // Registers a stamped CreatureDNA. Returns false on collision or missing stamp.
    public bool Register(CreatureDNA dna)
    {
        if (dna == null || string.IsNullOrEmpty(dna.UniqueID))
        {
            Debug.LogError("[CreatureDatabase] Cannot register: DNA is null or not stamped. Call Stamp() first.");
            return false;
        }
        if (_registry.ContainsKey(dna.UniqueID))
        {
            Debug.LogWarning($"[CreatureDatabase] ID collision — '{dna.UniqueID}' already registered.");
            return false;
        }
        _registry[dna.UniqueID] = dna;
        return true;
    }

    // O(1) lookup by UniqueID.
    public bool TryGet(string uniqueID, out CreatureDNA dna) =>
        _registry.TryGetValue(uniqueID, out dna);

    // Returns a snapshot copy — safe to iterate without locking.
    public Dictionary<string, CreatureDNA> GetAll() =>
        new Dictionary<string, CreatureDNA>(_registry);
}
