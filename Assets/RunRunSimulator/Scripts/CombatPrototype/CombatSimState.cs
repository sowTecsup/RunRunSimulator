using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class CombatSimState
    {
        public CombatBoard Board;
        public List<CombatUnit> Units = new List<CombatUnit>();

        public CombatSimState Clone()
        {
            CombatSimState clone = new CombatSimState();
            clone.Board = Board;
            for (int i = 0; i < Units.Count; i++)
            {
                clone.Units.Add(Units[i].Clone());
            }
            return clone;
        }

        public CombatUnit GetUnit(int id)
        {
            for (int i = 0; i < Units.Count; i++)
            {
                if (Units[i].Id == id)
                {
                    return Units[i];
                }
            }
            return null;
        }

        public CombatUnit GetUnitAt(Vector2Int cell)
        {
            for (int i = 0; i < Units.Count; i++)
            {
                CombatUnit unit = Units[i];
                if (unit.Alive && !unit.Airborne && unit.Cell == cell)
                {
                    return unit;
                }
            }
            return null;
        }

        public bool IsCellFree(Vector2Int cell)
        {
            return Board.InBounds(cell) && GetUnitAt(cell) == null;
        }

        public List<PlayerUnit> GetPlayers()
        {
            List<PlayerUnit> result = new List<PlayerUnit>();
            for (int i = 0; i < Units.Count; i++)
            {
                if (Units[i] is PlayerUnit player && player.Alive)
                {
                    result.Add(player);
                }
            }
            return result;
        }

        public List<EnemyUnit> GetEnemies()
        {
            List<EnemyUnit> result = new List<EnemyUnit>();
            for (int i = 0; i < Units.Count; i++)
            {
                if (Units[i] is EnemyUnit enemy && enemy.Alive)
                {
                    result.Add(enemy);
                }
            }
            return result;
        }
    }
}
