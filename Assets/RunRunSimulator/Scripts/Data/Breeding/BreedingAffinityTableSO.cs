using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "BreedingAffinityTable", menuName = "RunRunSimulator/Breeding/Breeding Affinity Table")]
public class BreedingAffinityTableSO : SerializedScriptableObject
{
    [Title("Affinity Matrix (Role → Role → Chance 0..1)")]
    [InfoBox("Symmetric: set (A, B) or (B, A) — both will return the same value. Missing pairs default to 0.5.")]
    [OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel = "Pair (A, B)", ValueLabel = "Affinity")]
    private Dictionary<(Role, Role), float> affinities = new Dictionary<(Role, Role), float>();

    public float GetAffinity(Role a, Role b)
    {
        var key = a <= b ? (a, b) : (b, a);
        return affinities != null && affinities.TryGetValue(key, out var chance) ? chance : 0.5f;
    }

    [Button("Seed Defaults", ButtonSizes.Large), GUIColor(0.55f, 1f, 0.7f)]
    private void SeedDefaults()
    {
        affinities = new Dictionary<(Role, Role), float>
        {
            { (Role.Protector, Role.Protector), 0.60f },
            { (Role.Protector, Role.Agresivo),  0.40f },
            { (Role.Protector, Role.Empatico),  0.60f },
            { (Role.Agresivo,  Role.Agresivo),  0.55f },
            { (Role.Agresivo,  Role.Empatico),  0.50f },
            { (Role.Empatico,  Role.Empatico),  0.80f },
        };
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
}
