using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public enum EnemyPattern { ChaseMelee, RangedLine }

    [CreateAssetMenu(fileName = "EnemyUnit", menuName = "MoriMonchi/Combat Prototype/Enemy Unit")]
    public class EnemyDefinitionSO : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public int GuardTicks;
        public int FinisherTicks;
        public EnemyPattern Pattern;
        public int AttackRange;
        public Vector2Int[] MoveOffsets;
        public string[] BriefLines;
        public GameObject VisualPrefab;
        public Color Tint;
    }
}
