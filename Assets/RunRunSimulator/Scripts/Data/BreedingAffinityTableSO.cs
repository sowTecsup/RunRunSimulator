using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

[CreateAssetMenu(fileName = "BreedingAffinityTable", menuName = "RunRunSimulator/Breeding Affinity Table")]
public class BreedingAffinityTableSO : SerializedScriptableObject
{
    public static BreedingAffinityTableSO Current { get; private set; }

    private void OnEnable()
    {
#if UNITY_EDITOR
        if (Current != null && Current != this)
            Debug.LogWarning($"[BreedingAffinityTableSO] Duplicate instance: '{Current.name}' already registered, overwriting with '{this.name}'. Check for duplicated assets.", this);
#endif
        Current = this;
    }

    [Title("Affinity Matrix (Personality → Personality → Chance 0..1)")]
    [InfoBox("Symmetric: set (A, B) or (B, A) — both will return the same value. Missing pairs default to 0.5.")]
    [OdinSerialize]
    [DictionaryDrawerSettings(KeyLabel = "Pair (A, B)", ValueLabel = "Affinity")]
    private Dictionary<(Personality, Personality), float> affinities = new Dictionary<(Personality, Personality), float>();

    public float GetAffinity(Personality a, Personality b)
    {
        var key = a <= b ? (a, b) : (b, a);
        return affinities != null && affinities.TryGetValue(key, out var chance) ? chance : 0.5f;
    }

    [Button("Seed Defaults", ButtonSizes.Large), GUIColor(0.55f, 1f, 0.7f)]
    private void SeedDefaults()
    {
        affinities = new Dictionary<(Personality, Personality), float>
        {
            { (Personality.Skittish,   Personality.Skittish),   0.70f },
            { (Personality.Skittish,   Personality.Aggressive), 0.10f },
            { (Personality.Skittish,   Personality.Lazy),       0.50f },
            { (Personality.Skittish,   Personality.Curious),    0.40f },
            { (Personality.Skittish,   Personality.Social),     0.35f },
            { (Personality.Skittish,   Personality.Grumpy),     0.20f },
            { (Personality.Aggressive, Personality.Aggressive), 0.60f },
            { (Personality.Aggressive, Personality.Lazy),       0.30f },
            { (Personality.Aggressive, Personality.Curious),    0.45f },
            { (Personality.Aggressive, Personality.Social),     0.50f },
            { (Personality.Aggressive, Personality.Grumpy),     0.65f },
            { (Personality.Lazy,       Personality.Lazy),       0.75f },
            { (Personality.Lazy,       Personality.Curious),    0.40f },
            { (Personality.Lazy,       Personality.Social),     0.45f },
            { (Personality.Lazy,       Personality.Grumpy),     0.55f },
            { (Personality.Curious,    Personality.Curious),    0.65f },
            { (Personality.Curious,    Personality.Social),     0.70f },
            { (Personality.Curious,    Personality.Grumpy),     0.30f },
            { (Personality.Social,     Personality.Social),     0.80f },
            { (Personality.Social,     Personality.Grumpy),     0.25f },
            { (Personality.Grumpy,     Personality.Grumpy),     0.55f },
        };
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
