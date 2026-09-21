using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(menuName = "RunRunSimulator/Genetics/Creature Registry")]
public class CreatureRegistrySO : SerializedScriptableObject
{
    [InfoBox("Reflejo visual del JSON — no editar manualmente. Usar Sync para recargar desde creature_database.json.", InfoMessageType.Warning)]
    [OdinSerialize]
    [PreviouslySerializedAs("_creatures")]
    [DictionaryDrawerSettings(KeyLabel = "UniqueID", ValueLabel = "DNA",
        DisplayMode = DictionaryDisplayOptions.CollapsedFoldout)]
    private Dictionary<string, CreatureDNA> creatures = new Dictionary<string, CreatureDNA>();

    [OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel = "UniqueID", ValueLabel = "DNA",
        DisplayMode = DictionaryDisplayOptions.CollapsedFoldout)]
    private Dictionary<string, CreatureDNA> departed = new Dictionary<string, CreatureDNA>();

    public const int MaxDeparted = 300;

    private void MarkDirty()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    public void RerollRolesAndElements()
    {
        var roleValues    = (Role[])System.Enum.GetValues(typeof(Role));
        var elementValues = (Element[])System.Enum.GetValues(typeof(Element));
        foreach (var dna in creatures.Values)
        {
            dna.Role    = roleValues[UnityEngine.Random.Range(0, roleValues.Length)];
            dna.Element = elementValues[UnityEngine.Random.Range(0, elementValues.Length)];
        }

        MarkDirty();
    }

    public int Wipe()
    {
        int had = creatures.Count;
        creatures = new Dictionary<string, CreatureDNA>();
        departed = new Dictionary<string, CreatureDNA>();
        MarkDirty();
        return had;
    }

    public bool Register(CreatureDNA dna)
    {
        if (dna == null || string.IsNullOrEmpty(dna.UniqueID))
        {
            Debug.LogError("[CreatureRegistrySO] Cannot register: DNA is null or not stamped. Call Stamp() first.");
            return false;
        }
        if (creatures.ContainsKey(dna.UniqueID) || departed.ContainsKey(dna.UniqueID))
        {
            Debug.LogWarning($"[CreatureRegistrySO] ID collision — '{dna.UniqueID}' already registered.");
            return false;
        }
        creatures[dna.UniqueID] = dna;
        MarkDirty();
        return true;
    }

    public bool TryGet(string uniqueID, out CreatureDNA dna)
    {
        if (creatures.TryGetValue(uniqueID, out dna)) return true;
        return departed.TryGetValue(uniqueID, out dna);
    }

    public Dictionary<string, CreatureDNA> GetAll() =>
        new Dictionary<string, CreatureDNA>(creatures);

    public IReadOnlyDictionary<string, CreatureDNA> Departed => departed;

    public Dictionary<string, CreatureDNA> GetAllKnown()
    {
        var all = new Dictionary<string, CreatureDNA>(creatures);
        foreach (var pair in departed)
            all[pair.Key] = pair.Value;
        return all;
    }

    public bool Depart(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        if (!creatures.TryGetValue(id, out var dna)) return false;
        creatures.Remove(id);
        departed[id] = dna;
        TrimDeparted();
        MarkDirty();
        return true;
    }

    public RegistryData GetData() => new RegistryData
    {
        Alive    = new Dictionary<string, CreatureDNA>(creatures),
        Departed = new Dictionary<string, CreatureDNA>(departed),
    };

    public void LoadFrom(RegistryData data)
    {
        creatures = data?.Alive ?? new Dictionary<string, CreatureDNA>();
        departed  = data?.Departed ?? new Dictionary<string, CreatureDNA>();
        TrimDeparted();
        ReconcileColors();
        MarkDirty();
    }

    private void TrimDeparted()
    {
        if (departed.Count <= MaxDeparted) return;

        var protectedIds = new HashSet<string>();
        foreach (var dna in creatures.Values)
        {
            if (dna == null) continue;
            if (!string.IsNullOrEmpty(dna.MotherID)) protectedIds.Add(dna.MotherID);
            if (!string.IsNullOrEmpty(dna.FatherID)) protectedIds.Add(dna.FatherID);
        }

        var removable = departed
            .Where(pair => pair.Value != null && !protectedIds.Contains(pair.Key))
            .OrderBy(pair => pair.Value.Timestamp)
            .Select(pair => pair.Key)
            .ToList();

        int excess = departed.Count - MaxDeparted;
        for (int i = 0; i < excess && i < removable.Count; i++)
            departed.Remove(removable[i]);
    }

    private IEnumerable<KeyValuePair<string, CreatureDNA>> AllPairs()
    {
        foreach (var pair in creatures) yield return pair;
        foreach (var pair in departed) yield return pair;
    }

    private void ReconcileColors()
    {
        var generationCache = new Dictionary<string, int>();
        foreach (var pair in AllPairs())
        {
            var dna = pair.Value;
            if (dna == null) continue;
            if (TryColorFromKey(pair.Key, out var keyColor))
            {
                if (ColorUtility.ToHtmlStringRGB(dna.BaseColor) != ColorUtility.ToHtmlStringRGB(keyColor))
                    dna.BaseColor = keyColor;
                dna.SecondaryColor = ColorGenetics.DeriveSecondary(dna.BaseColor);
            }
            if (dna.Generation == 0)
                ComputeGeneration(pair.Key, generationCache, new HashSet<string>());
        }
    }

    private int ComputeGeneration(string id, Dictionary<string, int> cache, HashSet<string> visiting)
    {
        if (string.IsNullOrEmpty(id)) return 0;
        if (cache.TryGetValue(id, out var cached)) return cached;
        if (!TryGet(id, out var dna) || dna == null) return 0;
        if (dna.Generation > 0)
        {
            cache[id] = dna.Generation;
            return dna.Generation;
        }
        if (visiting.Contains(id))
        {
            cache[id] = 1;
            return 1;
        }

        visiting.Add(id);
        int motherGen = ComputeGeneration(dna.MotherID, cache, visiting);
        int fatherGen = ComputeGeneration(dna.FatherID, cache, visiting);
        visiting.Remove(id);

        int generation = Mathf.Max(motherGen, fatherGen) + 1;
        dna.Generation = generation;
        cache[id] = generation;
        return generation;
    }

    private static bool TryColorFromKey(string key, out Color color)
    {
        color = Color.white;
        if (string.IsNullOrEmpty(key)) return false;
        var tokens = key.Split('-');
        if (tokens.Length < 2) return false;
        return ColorUtility.TryParseHtmlString("#" + tokens[tokens.Length - 2], out color);
    }

    public int Count => creatures.Count;

#if UNITY_EDITOR
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
        get => creatures.Keys.Select(k => new IDEntry { id = k }).ToList();
        set { }
    }
#endif
}
}
