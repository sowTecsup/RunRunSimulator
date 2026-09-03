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
