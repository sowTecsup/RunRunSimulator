using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "CombatManager", menuName = "RunRunSimulator/Combat Manager")]
public class CombatManagerSO : SerializedScriptableObject
{
    public static CombatManagerSO Current { get; private set; }
    private void OnEnable() => Current = this;

    [Title("Combat Settings")]
    [InfoBox("EvolutionChance y DeathChance son valores 0–1 (ej: 0.3 = 30%).")]

    [LabelWidth(160)] public float EvolutionChance = 0.30f;
    [LabelWidth(160)] public float DeathChance     = 0.15f;

    [Title("Hit Settings")]
    [LabelWidth(160)] public float CritChance     = 0.20f;
    [LabelWidth(160)] public float CritMultiplier = 3f;

    [Title("Safety")]
    [LabelWidth(160)] public int MaxRounds    = 50;
    [LabelWidth(160)] public int MaxFightCount = 5;
}
