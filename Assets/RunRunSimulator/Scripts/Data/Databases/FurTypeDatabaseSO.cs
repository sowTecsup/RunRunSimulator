using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "FurTypeDatabase", menuName = "RunRunSimulator/Databases/Fur Type Database")]
public class FurTypeDatabaseSO : SerializedScriptableObject
{
    [Title("Fur Type → CartoonShader Material")]
    [InfoBox("Each FurType maps to one material (CartoonShader). Per-creature colors are applied at runtime via MaterialPropertyBlock — leave the material's color fields at neutral defaults.")]
    [OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel = "Fur Type", ValueLabel = "Material")]
    private Dictionary<FurType, Material> materials = new Dictionary<FurType, Material>();

    [Title("Mint Weights")]
    [InfoBox("Peso relativo de cada patrón al mintear una criatura nueva (mayor = más común). La herencia en breeding NO usa esta tabla.")]
    [OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel = "Fur Type", ValueLabel = "Weight")]
    private Dictionary<FurType, float> mintWeights = new Dictionary<FurType, float>();

    public Material GetMaterial(FurType type)
    {
        if (materials != null && materials.TryGetValue(type, out var mat) && mat != null)
            return mat;
        Debug.LogWarning($"[FurTypeDatabaseSO] No material assigned for FurType '{type}'.");
        return null;
    }

    public FurType RollMintFurType()
    {
        var allValues = (FurType[])Enum.GetValues(typeof(FurType));
        float total = 0f;
        if (mintWeights != null)
        {
            foreach (var kvp in mintWeights)
                if (kvp.Value > 0f)
                    total += kvp.Value;
        }

        if (mintWeights == null || mintWeights.Count == 0 || total <= 0f)
            return allValues[UnityEngine.Random.Range(0, allValues.Length)];

        float roll = UnityEngine.Random.Range(0f, total);
        float cumulative = 0f;
        foreach (var kvp in mintWeights)
        {
            if (kvp.Value <= 0f)
                continue;
            cumulative += kvp.Value;
            if (roll <= cumulative)
                return kvp.Key;
        }

        return allValues[UnityEngine.Random.Range(0, allValues.Length)];
    }

    [Button("Populate from Enum", ButtonSizes.Large), GUIColor(0.4f, 1f, 0.6f)]
    private void PopulateFromEnum()
    {
        materials ??= new Dictionary<FurType, Material>();
        mintWeights ??= new Dictionary<FurType, float>();
        foreach (FurType type in Enum.GetValues(typeof(FurType)))
        {
            if (!materials.ContainsKey(type))
                materials[type] = null;
            if (!mintWeights.ContainsKey(type))
                mintWeights[type] = 1f;
        }
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
}
