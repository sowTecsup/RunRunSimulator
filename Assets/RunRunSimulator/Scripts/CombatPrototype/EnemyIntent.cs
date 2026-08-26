using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class EnemyIntent
    {
        public Vector2Int AttackDirection;
        public Vector2Int[] AttackOffsets;

        public bool HasAttack => AttackDirection != Vector2Int.zero && AttackOffsets != null && AttackOffsets.Length > 0;

        public List<Vector2Int> GetAttackCells(Vector2Int fromCell)
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            if (!HasAttack) return cells;

            for (int i = 0; i < AttackOffsets.Length; i++)
            {
                cells.Add(fromCell + RotateOffset(AttackOffsets[i], AttackDirection));
            }

            return cells;
        }

        public EnemyIntent Clone()
        {
            EnemyIntent clone = new EnemyIntent();
            clone.AttackDirection = AttackDirection;
            clone.AttackOffsets = AttackOffsets;
            return clone;
        }

        private static Vector2Int RotateOffset(Vector2Int offset, Vector2Int direction)
        {
            if (direction == Vector2Int.left) return new Vector2Int(-offset.x, -offset.y);
            if (direction == Vector2Int.up) return new Vector2Int(-offset.y, offset.x);
            if (direction == Vector2Int.down) return new Vector2Int(offset.y, -offset.x);
            return offset;
        }
    }
}
