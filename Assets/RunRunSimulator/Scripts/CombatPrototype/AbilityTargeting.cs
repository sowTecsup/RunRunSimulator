using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public static class AbilityTargeting
    {
        public static int Chebyshev(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        public static int Manhattan(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
        }

        public static Vector2Int DominantCardinal(Vector2Int from, Vector2Int to)
        {
            int dx = to.x - from.x;
            int dy = to.y - from.y;

            if (dx == 0 && dy == 0)
                return new Vector2Int(1, 0);

            if (Mathf.Abs(dx) >= Mathf.Abs(dy))
                return new Vector2Int(dx > 0 ? 1 : -1, 0);

            return new Vector2Int(0, dy > 0 ? 1 : -1);
        }

        public static Vector2Int RotateOffset(Vector2Int offset, Vector2Int direction)
        {
            if (direction == Vector2Int.left) return new Vector2Int(-offset.x, -offset.y);
            if (direction == Vector2Int.up) return new Vector2Int(-offset.y, offset.x);
            if (direction == Vector2Int.down) return new Vector2Int(offset.y, -offset.x);
            return offset;
        }

        public static bool IsWall(CombatBoard board, Vector2Int fromCell, Vector2Int cell)
        {
            if (!board.InBounds(cell))
                return true;

            return board.GetElevation(cell) >= board.GetElevation(fromCell) + 2;
        }

        public static List<Vector2Int> GetValidTargets(CombatSimState state, CombatUnit unit, CombatAbilitySO ability)
        {
            List<Vector2Int> result = new List<Vector2Int>();
            CombatBoard board = state.Board;
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            switch (ability.Targeting)
            {
                case TargetingMode.FreeCell:
                    for (int x = 0; x < board.Width; x++)
                        for (int z = 0; z < board.Depth; z++)
                        {
                            Vector2Int cell = new Vector2Int(x, z);
                            int distance = Chebyshev(unit.Cell, cell);
                            if (distance >= 1 && distance <= ability.Range && state.IsCellFree(cell))
                                result.Add(cell);
                        }
                    break;

                case TargetingMode.StraightLine:
                    for (int d = 0; d < directions.Length; d++)
                        for (int dist = 1; dist <= ability.Range; dist++)
                        {
                            Vector2Int cell = unit.Cell + directions[d] * dist;
                            if (state.IsCellFree(cell))
                                result.Add(cell);
                        }
                    break;

                case TargetingMode.DirectionalTemplate:
                    for (int d = 0; d < directions.Length; d++)
                    {
                        List<Vector2Int> cells = GetAffectedCellsForDirection(state, unit.Cell, ability, directions[d]);
                        for (int i = 0; i < cells.Count; i++)
                            if (!result.Contains(cells[i]))
                                result.Add(cells[i]);
                    }
                    break;

                case TargetingMode.RangeBand:
                    for (int x = 0; x < board.Width; x++)
                        for (int z = 0; z < board.Depth; z++)
                        {
                            Vector2Int cell = new Vector2Int(x, z);
                            int distance = Chebyshev(unit.Cell, cell);
                            if (distance >= ability.RangeMin && distance <= ability.Range)
                                result.Add(cell);
                        }
                    break;

                case TargetingMode.AirborneEnemy:
                    for (int i = 0; i < state.Units.Count; i++)
                    {
                        CombatUnit candidate = state.Units[i];
                        if (candidate.Airborne && candidate.Alive && Chebyshev(unit.Cell, candidate.Cell) <= ability.Range)
                            result.Add(candidate.Cell);
                    }
                    break;
            }

            return result;
        }

        public static List<Vector2Int> GetAffectedCellsForDirection(CombatSimState state, Vector2Int origin, CombatAbilitySO ability, Vector2Int direction)
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            CombatBoard board = state.Board;

            for (int i = 0; i < ability.TemplateOffsets.Length; i++)
            {
                Vector2Int cell = origin + RotateOffset(ability.TemplateOffsets[i], direction);

                if (!board.InBounds(cell))
                    break;
                if (!ability.IgnoresHeight && IsWall(board, origin, cell))
                    break;

                if (state.GetUnitAt(cell) != null)
                {
                    cells.Add(cell);
                    if (!ability.IgnoresObstacles)
                        break;
                    continue;
                }

                cells.Add(cell);
            }

            return cells;
        }

        public static List<Vector2Int> GetAffectedCells(CombatSimState state, CombatUnit unit, CombatAbilitySO ability, PlannedAction action)
        {
            if (ability.Type == AbilityType.Movement)
                return new List<Vector2Int>();

            switch (ability.Targeting)
            {
                case TargetingMode.DirectionalTemplate:
                    return GetAffectedCellsForDirection(state, unit.Cell, ability, action.Direction);
                case TargetingMode.RangeBand:
                    return new List<Vector2Int> { action.TargetCell };
                case TargetingMode.AirborneEnemy:
                    return new List<Vector2Int> { action.TargetCell };
                default:
                    return new List<Vector2Int>();
            }
        }

        public static bool IsValidTarget(CombatSimState state, CombatUnit unit, CombatAbilitySO ability, PlannedAction action)
        {
            switch (ability.Targeting)
            {
                case TargetingMode.FreeCell:
                case TargetingMode.StraightLine:
                    return GetValidTargets(state, unit, ability).Contains(action.TargetCell);

                case TargetingMode.DirectionalTemplate:
                    if (!IsCardinal(action.Direction))
                        return false;
                    return GetAffectedCells(state, unit, ability, action).Count > 0;

                case TargetingMode.RangeBand:
                    return state.Board.InBounds(action.TargetCell)
                        && Chebyshev(unit.Cell, action.TargetCell) >= ability.RangeMin
                        && Chebyshev(unit.Cell, action.TargetCell) <= ability.Range;

                case TargetingMode.AirborneEnemy:
                    return IsValidAirborneSlam(state, unit, ability, action);

                default:
                    return false;
            }
        }

        public static List<Vector2Int> GetLineCells(CombatSimState state, Vector2Int from, Vector2Int direction, int length, bool stopAtUnits)
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            CombatBoard board = state.Board;

            for (int i = 1; i <= length; i++)
            {
                Vector2Int cell = from + direction * i;

                if (!board.InBounds(cell) || IsWall(board, from, cell))
                    break;

                if (state.GetUnitAt(cell) != null)
                {
                    cells.Add(cell);
                    if (stopAtUnits)
                        break;
                    continue;
                }

                cells.Add(cell);
            }

            return cells;
        }

        private static bool IsValidAirborneSlam(CombatSimState state, CombatUnit unit, CombatAbilitySO ability, PlannedAction action)
        {
            if (FindAirborneUnitAt(state, action.TargetCell) == null)
                return false;
            if (Chebyshev(unit.Cell, action.TargetCell) > ability.Range)
                return false;

            int slamDistance = Chebyshev(action.TargetCell, action.SlamCell);
            if (slamDistance < 1 || slamDistance > ability.SlamRange)
                return false;

            bool sameRow = action.SlamCell.y == action.TargetCell.y;
            bool sameColumn = action.SlamCell.x == action.TargetCell.x;
            return sameRow || sameColumn;
        }

        private static CombatUnit FindAirborneUnitAt(CombatSimState state, Vector2Int cell)
        {
            for (int i = 0; i < state.Units.Count; i++)
            {
                CombatUnit candidate = state.Units[i];
                if (candidate.Airborne && candidate.Alive && candidate.Cell == cell)
                    return candidate;
            }
            return null;
        }

        private static bool IsCardinal(Vector2Int direction)
        {
            return direction == Vector2Int.up || direction == Vector2Int.down || direction == Vector2Int.left || direction == Vector2Int.right;
        }
    }
}
