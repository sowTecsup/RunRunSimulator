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
    // ── Private Fields ────────────────────────────────────────────

    [InfoBox("Reflejo visual del JSON — no editar manualmente. Usar Sync para recargar desde creature_database.json.", InfoMessageType.Warning)]
    [OdinSerialize]
    [PreviouslySerializedAs("_creatures")]
    [DictionaryDrawerSettings(KeyLabel = "UniqueID", ValueLabel = "DNA",
        DisplayMode = DictionaryDisplayOptions.CollapsedFoldout)]
    private Dictionary<string, CreatureDNA> creatures = new Dictionary<string, CreatureDNA>();

    // ── Private Methods ───────────────────────────────────────────

    private void MarkDirty()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

#if UNITY_EDITOR
    [System.Serializable]
    private class IDEntry
    {
        [DisplayAsString, HideLabel, HorizontalGroup]
        public string id;

        [HorizontalGroup(Width = 55), Button("Copy"), GUIColor(0.6f, 0.9f, 1f)]
        private void CopyToClipboard() => GUIUtility.systemCopyBuffer = id;
    }

    [Button("Sync from JSON", ButtonSizes.Large), GUIColor(0.5f, 0.85f, 1f)]
    private void SyncFromJson() => SaveSystem.LoadInto(this);

    // Reasigna una Personality aleatoria a cada MoriMochi del registro y guarda el JSON
    // local. NO sube a la nube: dispara RegistryReloaded (UI-only, sin push) para refrescar
    // la escena, y deja la subida en tus manos → pulsá "Push to Cloud" en CloudSyncService.
    [Button("Randomize Personalities (current)", ButtonSizes.Large), GUIColor(1f, 0.8f, 0.4f)]
    [PropertyTooltip("Personality aleatoria para cada criatura. Guarda JSON local + refresca escena. Subí con 'Push to Cloud' (CloudSyncService).")]
    private void RandomizePersonalities()
    {
        if (creatures.Count == 0)
        {
            Debug.LogWarning("[CreatureRegistrySO] No hay criaturas para randomizar.");
            return;
        }

        var values = (Personality[])System.Enum.GetValues(typeof(Personality));
        foreach (var dna in creatures.Values)
            dna.Personality = values[UnityEngine.Random.Range(0, values.Length)];

        MarkDirty();
        SaveSystem.SaveDatabase(this);                 // persiste local (scoped al usuario logueado en Play)

        if (Application.isPlaying)
            GameEvents.RegistryReloaded(this);         // re-spawnea con nuevos colores/rangos, SIN push

        Debug.Log($"[CreatureRegistrySO] {creatures.Count} personalidades randomizadas. " +
                  "Pulsá 'Push to Cloud' en CloudSyncService para subir a Cloud Save.");
    }

    [Button("Wipe Registry (DEV)", ButtonSizes.Large), GUIColor(1f, 0.45f, 0.45f)]
    [PropertyTooltip("Borra TODAS las criaturas. Guarda JSON local vacío + refresca escena. Subí con 'Push to Cloud' (CloudSyncService) para limpiar también la nube.")]
    private void WipeRegistry()
    {
        int had = creatures.Count;
        creatures = new Dictionary<string, CreatureDNA>();

        MarkDirty();
        SaveSystem.SaveDatabase(this);

        if (Application.isPlaying)
            GameEvents.RegistryReloaded(this);

        Debug.Log($"[CreatureRegistrySO] Registro borrado ({had} criaturas). " +
                  "Pulsá 'Push to Cloud' en CloudSyncService para limpiar Cloud Save.");
    }
#endif

    // ── Public Methods ────────────────────────────────────────────

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

    // ── Getters ───────────────────────────────────────────────────

    public int Count => creatures.Count;

#if UNITY_EDITOR
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
