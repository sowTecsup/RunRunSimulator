using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class CombatUnit
    {
        public int Id;
        public bool IsPlayer;
        public Vector2Int Cell;
        public int Ticks;
        public int MaxTicks;
        public bool Alive = true;
        public bool Airborne;
        public Vector2Int AirborneLandingCell;
        public Vector2Int AirborneDirection;
        public int AirborneLauncherId = -1;
        public bool AirborneJustLaunched;
        public bool WasAirborneThisPhase;

        public virtual CombatUnit Clone()
        {
            CombatUnit clone = new CombatUnit();
            CopyBaseTo(clone);
            return clone;
        }

        protected void CopyBaseTo(CombatUnit other)
        {
            other.Id = Id;
            other.IsPlayer = IsPlayer;
            other.Cell = Cell;
            other.Ticks = Ticks;
            other.MaxTicks = MaxTicks;
            other.Alive = Alive;
            other.Airborne = Airborne;
            other.AirborneLandingCell = AirborneLandingCell;
            other.AirborneDirection = AirborneDirection;
            other.AirborneLauncherId = AirborneLauncherId;
            other.AirborneJustLaunched = AirborneJustLaunched;
            other.WasAirborneThisPhase = WasAirborneThisPhase;
        }
    }
}
