using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public static class EnemyBrain
    {
        public static EnemyIntent ComputeIntent(CombatSimState state, EnemyUnit enemy)
        {
            EnemyIntent intent = new EnemyIntent();
            intent.AttackDirection = enemy.Facing;

            switch (enemy.Definition.Pattern)
            {
                case EnemyPattern.ChaseMelee:
                    intent.AttackOffsets = new Vector2Int[] { new Vector2Int(1, 0) };
                    break;
                case EnemyPattern.RangedLine:
                    intent.AttackOffsets = BuildLineOffsets(enemy.Definition.AttackRange);
                    break;
            }

            return intent;
        }

        private static Vector2Int[] BuildLineOffsets(int attackRange)
        {
            Vector2Int[] offsets = new Vector2Int[attackRange];
            for (int i = 0; i < attackRange; i++)
            {
                offsets[i] = new Vector2Int(i + 1, 0);
            }

            return offsets;
        }
    }
}
