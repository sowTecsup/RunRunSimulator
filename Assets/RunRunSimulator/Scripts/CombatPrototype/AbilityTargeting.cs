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

        public static Vector2Int GetLandingCell(CombatUnit unit, CombatAbilitySO ability, PlannedAction action)
        {
            switch (ability.Landing)
            {
                case LandingKind.Stay:
                    return unit.Cell;
                case LandingKind.AtAnchor:
                    return action.TargetCell;
                case LandingKind.BehindAnchor:
                    return action.TargetCell - action.Direction;
                default:
                    return unit.Cell;
            }
        }

        public static bool IsLandingFree(CombatSimState state, CombatUnit unit, Vector2Int landing)
        {
            return state.Board.InBounds(landing) && (landing == unit.Cell || state.GetUnitAt(landing) == null);
        }

        public static List<Vector2Int> GetAffectedCells(CombatSimState state, CombatAbilitySO ability, PlannedAction action)
        {
            if (ability.Type != AbilityType.Attack)
                return new List<Vector2Int>();

            switch (ability.Targeting)
            {
                case TargetingMode.AirborneEnemy:
                    return new List<Vector2Int> { action.TargetCell };

                case TargetingMode.DirectionalTemplate:
                    {
                        List<Vector2Int> cells = new List<Vector2Int>();
                        CombatBoard board = state.Board;
                        Vector2Int anchor = action.TargetCell;

                        for (int i = 0; i < ability.TemplateOffsets.Length; i++)
                        {
                            Vector2Int cell = anchor + RotateOffset(ability.TemplateOffsets[i], action.Direction);

                            if (!board.InBounds(cell))
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

                default:
                    return new List<Vector2Int>();
            }
        }

        public static bool IsValidTarget(CombatSimState state, CombatUnit unit, CombatAbilitySO ability, PlannedAction action)
        {
            if (ability.Type == AbilityType.Movement)
                return state.Board.InBounds(action.TargetCell) && state.IsCellFree(action.TargetCell);

            switch (ability.Targeting)
            {
                case TargetingMode.DirectionalTemplate:
                    if (!IsCardinal(action.Direction))
                        return false;
                    if (!state.Board.InBounds(action.TargetCell))
                        return false;
                    if (GetAffectedCells(state, ability, action).Count == 0)
                        return false;
                    return IsLandingFree(state, unit, GetLandingCell(unit, ability, action));

                case TargetingMode.AirborneEnemy:
                    return IsValidAirborneSlam(state, unit, ability, action);

                default:
                    return false;
            }
        }

        private static bool IsValidAirborneSlam(CombatSimState state, CombatUnit unit, CombatAbilitySO ability, PlannedAction action)
        {
            if (FindAirborneUnitAt(state, action.TargetCell) == null)
                return false;

            int slamDistance = Chebyshev(action.TargetCell, action.SlamCell);
            if (slamDistance < 1 || slamDistance > ability.SlamRange)
                return false;

            bool sameRow = action.SlamCell.y == action.TargetCell.y;
            bool sameColumn = action.SlamCell.x == action.TargetCell.x;
            if (!sameRow && !sameColumn)
                return false;

            return IsLandingFree(state, unit, GetLandingCell(unit, ability, action));
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
