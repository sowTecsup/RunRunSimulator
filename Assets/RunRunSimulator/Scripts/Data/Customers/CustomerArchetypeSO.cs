using Sirenix.OdinInspector;
using UnityEngine;

namespace MoriMonchiSimulator
{
[CreateAssetMenu(fileName = "CustomerArchetype", menuName = "RunRunSimulator/Customers/Customer Archetype")]
[InlineEditor]
public class CustomerArchetypeSO : SerializedScriptableObject
{
    [Title("Identity")]
    public string DisplayName = "";
    public Sprite Icon;
    [AssetsOnly] public GameObject AgentPrefab;

    [Title("Preference (weights for valuation)")]
    [MinValue(0)] public float WeightBreed = 1f;
    [MinValue(0)] public float WeightStats = 1f;
    [MinValue(0)] public float WeightTier = 1f;

    [Title("Budget")]
    [MinValue(0)] public float BudgetMultiplier = 1f;

    [Title("Negotiation")]
    [Range(0f, 1f)] public float RenegotiationTolerance = 0.5f;

    [Title("Browsing")]
    [MinValue(1)] public int MinInspections = 1;
    [MinValue(1)] public int MaxInspections = 3;
    [MinValue(0f)] public float InspectionDuration = 3f;

    [Title("Timeout")]
    [MinValue(0f)] public float WaitTimeoutSeconds = 60f;
}
}
