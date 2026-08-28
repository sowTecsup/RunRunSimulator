using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public enum ResolutionEventType { Move, Hit, Push, Launch, Land, Die, EnemyAttack, Rotate, Fizzle, Impact }

    public class ResolutionEvent
    {
        public ResolutionEventType Type;
        public int UnitId;
        public int SourceId;
        public Vector2Int From;
        public Vector2Int To;
        public Vector2Int Facing;
        public List<Vector2Int> Cells;
        public int TicksAfter;
        public bool Environmental;
        public int Wave;
        public bool Projectile;
        public List<Vector2Int> Path;

        public ResolutionEvent(ResolutionEventType type, int unitId)
        {
            Type = type;
            UnitId = unitId;
            SourceId = -1;
            Cells = new List<Vector2Int>();
        }
    }
}
