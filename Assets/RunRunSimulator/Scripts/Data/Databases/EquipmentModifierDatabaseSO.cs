using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

// Single source of truth for every modifier effect and its tiers. Equipment stores
// only (Kind, Tier); this resolves it to a ModifierTierDef for display now and the
// combat procs in Stage 2. Mirror of EquipmentDatabaseSO/PartDatabaseSO: a local table
// the game (and the async JS motor, by mirroring the same numbers) reads at generation
// time. Assign it on GameManager like the other databases.
[CreateAssetMenu(fileName = "EquipmentModifierDatabase", menuName = "RunRunSimulator/Databases/Equipment Modifier Database")]
public class EquipmentModifierDatabaseSO : SerializedScriptableObject
{
    [Title("Modifier Catalog", "Effect kind → tier → configured values.")]
    [DictionaryDrawerSettings(KeyLabel = "Effect", ValueLabel = "Tiers",
        DisplayMode = DictionaryDisplayOptions.ExpandedFoldout)]
    [OdinSerialize]
    private Dictionary<ModifierEffectKind, Dictionary<ModifierTier, ModifierTierDef>> catalog =
        new Dictionary<ModifierEffectKind, Dictionary<ModifierTier, ModifierTierDef>>();

    public bool TryResolve(EquipmentModifierRef r, out ModifierTierDef def) =>
        TryResolve(r.Kind, r.Tier, out def);

    public bool TryResolve(ModifierEffectKind kind, ModifierTier tier, out ModifierTierDef def)
    {
        def = default;
        return catalog.TryGetValue(kind, out var tiers) && tiers != null && tiers.TryGetValue(tier, out def);
    }

    public string Summary(EquipmentModifierRef r)
    {
        if (!TryResolve(r, out var def)) return $"{KindLabel(r.Kind)} (tier {r.Tier}?)";
        string label = string.IsNullOrEmpty(def.Label) ? KindLabel(r.Kind) : def.Label;
        return r.Kind switch
        {
            ModifierEffectKind.ReturnDamage => $"{label}: regresa {def.Magnitude:0.##} de daño",
            ModifierEffectKind.Heal         => $"{label}: cura {def.Magnitude:0.##}",
            ModifierEffectKind.ApplyStatus  => $"{label}: {def.Status} por {def.DurationTurns} turno(s)",
            _ => label,
        };
    }

    public static string KindLabel(ModifierEffectKind k) => k switch
    {
        ModifierEffectKind.ReturnDamage => "Regresa daño",
        ModifierEffectKind.Heal         => "Cura",
        ModifierEffectKind.ApplyStatus  => "Aplica estado",
        _ => k.ToString(),
    };

    [ShowInInspector, ReadOnly, LabelText("Effect Kinds")]
    public int KindCount => catalog?.Count ?? 0;

#if UNITY_EDITOR
    private static EquipmentModifierDatabaseSO editorInstance;

    // Editor-only resolver so an EquipmentSO's modifier summary can resolve tiers
    // without a live GameManager (e.g. editing the item asset directly).
    public static EquipmentModifierDatabaseSO Editor
    {
        get
        {
            if (editorInstance != null) return editorInstance;
            var guids = UnityEditor.AssetDatabase.FindAssets("t:EquipmentModifierDatabaseSO");
            if (guids.Length > 0)
                editorInstance = UnityEditor.AssetDatabase.LoadAssetAtPath<EquipmentModifierDatabaseSO>(
                    UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]));
            return editorInstance;
        }
    }
#endif
}
}
