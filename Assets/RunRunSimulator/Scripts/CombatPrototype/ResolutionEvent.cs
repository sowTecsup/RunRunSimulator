using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public enum ResolutionEventType { Move, Hit, Push, Launch, Land, Die, Reaction, EnemyAttack }

    public class ResolutionEvent
    {
        public ResolutionEventType Type;
        public int UnitId;
        public int SourceId;
        public Vector2Int From;
        public Vector2Int To;
        public List<Vector2Int> Cells;
        public int TicksAfter;
        public bool Environmental;
        public int Wave;

        public ResolutionEvent(ResolutionEventType type, int unitId)
        {
            Type = type;
            UnitId = unitId;
            SourceId = -1;
            Cells = new List<Vector2Int>();
        }
    }
}
