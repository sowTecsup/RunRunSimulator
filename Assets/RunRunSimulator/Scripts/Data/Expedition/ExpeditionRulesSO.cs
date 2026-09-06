using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "ExpeditionRules", menuName = "RunRunSimulator/Expedition/Expedition Rules")]
public class ExpeditionRulesSO : SerializedScriptableObject
{
    public static ExpeditionRulesSO Current { get; private set; }

    private void OnEnable()
    {
        Current = this;
    }

    [Title("Reglas")]
    [OdinSerialize]
    [ListDrawerSettings(ShowFoldout = false, DefaultExpandedState = true)]
    private List<ExpeditionRuleBase> rules = new();

    public IReadOnlyList<ExpeditionRuleBase> Rules => rules;

    [Title("Navegación")]
    [Min(0.1f)] public float ArriveDistance = 0.9f;
    [Min(0.05f)] public float RepathInterval = 0.5f;
    [Min(1f)] public float GiveUpSeconds = 12f;
    [Min(0f)] public float ApproachMargin = 0.15f;

    [Title("Beats")]
    [Min(0f)] public float NoticeSeconds = 0.5f;
    [Min(0f)] public float TakeSeconds = 1.2f;
    [Min(0f)] public float LoseSeconds = 1f;

    [Title("Ocupaciones")]
    [Min(0.5f)] public float MiningSecondsPerUnit = 4f;
    [Min(1)] public int CarryCapacity = 3;
    [Min(0f)] public float DepositSeconds = 0.8f;
    public MaterialPickup DropPrefab;
    [Min(0.1f)] public float DropScale = 0.6f;
    [Min(1f)] public float GuardRadius = 4f;
    [Min(0.1f)] public float HuntRepathInterval = 0.4f;
    [Min(1f)] public float DecoyRange = 4.5f;
    [Min(0f)] public float TauntSeconds = 0.8f;
    [Min(1f)] public float DecoyFleeDistance = 8f;
    [Min(0.5f)] public float DecoyFleeSeconds = 5f;
    [Min(0f)] public float DecoyCooldown = 4f;

    [Button] public void PopulateDefaults()
    {
        if (rules == null) rules = new List<ExpeditionRuleBase>();
        if (rules.Count == 0) rules.Add(new SeekMaterialRule());

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
}
