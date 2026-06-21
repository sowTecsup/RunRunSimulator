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

    public Material GetMaterial(FurType type)
    {
        if (materials != null && materials.TryGetValue(type, out var mat) && mat != null)
            return mat;
        Debug.LogWarning($"[FurTypeDatabaseSO] No material assigned for FurType '{type}'.");
        return null;
    }

    [Button("Populate from Enum", ButtonSizes.Large), GUIColor(0.4f, 1f, 0.6f)]
    private void PopulateFromEnum()
    {
        materials ??= new Dictionary<FurType, Material>();
        foreach (FurType type in Enum.GetValues(typeof(FurType)))
            if (!materials.ContainsKey(type))
                materials[type] = null;
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
}
