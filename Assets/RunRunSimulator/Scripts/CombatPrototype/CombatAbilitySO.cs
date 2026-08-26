using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public enum AbilityType { Movement, Attack }

    public enum TargetingMode { FreeCell, StraightLine, DirectionalTemplate, RangeBand, AirborneEnemy }

    public enum LandingKind { Stay, AtAnchor, BehindAnchor }

    [CreateAssetMenu(fileName = "CombatAbility", menuName = "MoriMonchi/Combat Prototype/Ability")]
    public class CombatAbilitySO : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public AbilityType Type;
        public TargetingMode Targeting;
        public int Range;
        public int RangeMin;
        public Vector2Int[] TemplateOffsets;
        public int PushDistance;
        public bool LaunchesAirborne;
        public bool SlamTargeted;
        public int SlamRange;
        public bool IgnoresHeight;
        public bool IgnoresObstacles;
        public LandingKind Landing;
    }
}
