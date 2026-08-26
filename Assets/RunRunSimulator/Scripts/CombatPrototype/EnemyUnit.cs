using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class EnemyUnit : CombatUnit
    {
        public EnemyDefinitionSO Definition;
        public Vector2Int Facing = new Vector2Int(1, 0);
        public bool WasHitThisTurn;
        public EnemyIntent Intent;

        public override CombatUnit Clone()
        {
            EnemyUnit clone = new EnemyUnit();
            CopyBaseTo(clone);
            clone.Definition = Definition;
            clone.Facing = Facing;
            clone.WasHitThisTurn = WasHitThisTurn;
            clone.Intent = Intent != null ? Intent.Clone() : null;
            return clone;
        }
    }
}
