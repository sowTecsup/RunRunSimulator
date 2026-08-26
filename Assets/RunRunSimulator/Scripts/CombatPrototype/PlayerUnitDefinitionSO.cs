using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    [CreateAssetMenu(fileName = "PlayerUnit", menuName = "MoriMonchi/Combat Prototype/Player Unit")]
    public class PlayerUnitDefinitionSO : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public int MaxTicks;
        public CombatAbilitySO[] Abilities;
        public GameObject VisualPrefab;
        public Color Tint;
    }
}
