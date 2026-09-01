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
        if (creatures.ContainsKey(dna.UniqueID))
        {
            Debug.LogWarning($"[CreatureRegistrySO] ID collision — '{dna.UniqueID}' already registered.");
            return false;
        }
        creatures[dna.UniqueID] = dna;
        MarkDirty();
        return true;
    }

    public bool TryGet(string uniqueID, out CreatureDNA dna) =>
        creatures.TryGetValue(uniqueID, out dna);

    public Dictionary<string, CreatureDNA> GetAll() =>
        new Dictionary<string, CreatureDNA>(creatures);

    public void LoadFrom(Dictionary<string, CreatureDNA> data)
    {
        creatures = data ?? new Dictionary<string, CreatureDNA>();
        ReconcileColors();
        MarkDirty();
    }

    private void ReconcileColors()
    {
        foreach (var pair in creatures)
        {
            var dna = pair.Value;
            if (dna == null) continue;
            if (!TryColorFromKey(pair.Key, out var keyColor)) continue;
            if (ColorUtility.ToHtmlStringRGB(dna.BaseColor) != ColorUtility.ToHtmlStringRGB(keyColor))
                dna.BaseColor = keyColor;
            dna.SecondaryColor = ColorGenetics.DeriveSecondary(dna.BaseColor);
        }
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
