using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public static class ActionResolver
    {
        public static List<ResolutionEvent> ResolveBeat(CombatSimState state, Beat beat)
        {
            List<ResolutionEvent> events = new List<ResolutionEvent>();
            CombatSimState snapshot = state.Clone();

            for (int i = 0; i < beat.Actions.Count; i++)
            {
                ResolvePlayerAction(state, snapshot, beat.Actions[i], events);
            }

            CombatEffects.CollectDeaths(state, 1, events);

            for (int i = 0; i < state.Units.Count; i++)
            {
                CombatUnit unit = state.Units[i];
                if (unit.Alive && unit.Airborne && !unit.AirborneJustLaunched)
                {
                    CombatEffects.ApplyLanding(state, unit, 2, events);
                }
            }

            CombatEffects.CollectDeaths(state, 2, events);

            for (int i = 0; i < state.Units.Count; i++)
            {
                state.Units[i].AirborneJustLaunched = false;
            }

            return events;
        }

        public static List<ResolutionEvent> ResolveEnemyTurn(CombatSimState state)
        {
            List<ResolutionEvent> events = new List<ResolutionEvent>();

            for (int i = 0; i < state.Units.Count; i++)
            {
                CombatUnit unit = state.Units[i];
                if (unit.Alive && unit.Airborne) CombatEffects.ApplyLanding(state, unit, 0, events);
            }

            CombatEffects.CollectDeaths(state, 0, events);

            List<EnemyUnit> enemies = state.GetEnemies();
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].Alive)
                {
                    ResolveEnemyAttack(state, enemies[i], 1 + i, events);
                    CombatEffects.CollectDeaths(state, 1 + i, events);
                }
            }

            int moveWave = 1 + enemies.Count;
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyUnit enemy = enemies[i];
                if (enemy.Alive && enemy.WasHitThisTurn && !enemy.WasAirborneThisPhase)
                {
                    TryEndOfTurnMove(state, enemy, moveWave, events);
                }
            }

            for (int i = 0; i < state.Units.Count; i++)
            {
                state.Units[i].WasAirborneThisPhase = false;
                if (state.Units[i] is EnemyUnit resetEnemy) resetEnemy.WasHitThisTurn = false;
            }

            return events;
        }

        public static List<ResolutionEvent> ResolveGermination(CombatSimState state)
        {
            List<ResolutionEvent> events = new List<ResolutionEvent>();

            List<EnemyUnit> enemies = state.GetEnemies();
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyUnit enemy = enemies[i];
                if (!enemy.Alive) continue;

                enemy.Ticks = 0;
                enemy.Alive = false;
                events.Add(new ResolutionEvent(ResolutionEventType.Die, enemy.Id) { From = enemy.Cell, To = enemy.Cell, Wave = 0 });
            }

            return events;
        }

        private static void ResolvePlayerAction(CombatSimState state, CombatSimState snapshot, PlannedAction action, List<ResolutionEvent> events)
        {
            CombatUnit unit = state.GetUnit(action.UnitId);
            if (unit == null || !unit.Alive) return;

            PlayerUnit playerUnit = (PlayerUnit)unit;
            CombatAbilitySO ability = playerUnit.Definition.Abilities[action.AbilityIndex];

            if (ability.Type == AbilityType.Movement)
            {
                ResolveMovement(state, unit, action.TargetCell, events);
                return;
            }

            if (ability.SlamTargeted)
            {
                ResolveSlam(state, unit, ability, action, events);
                return;
            }

            ResolveAttack(state, snapshot, unit, ability, action, events);
        }

        private static void ResolveMovement(CombatSimState state, CombatUnit unit, Vector2Int targetCell, List<ResolutionEvent> events)
        {
            if (!state.IsCellFree(targetCell))
            {
                events.Add(new ResolutionEvent(ResolutionEventType.Fizzle, unit.Id) { From = unit.Cell, To = unit.Cell, Wave = 0 });
                return;
            }

            ResolutionEvent moveEvent = new ResolutionEvent(ResolutionEventType.Move, unit.Id);
            moveEvent.From = unit.Cell;
            moveEvent.To = targetCell;
            moveEvent.Wave = 0;
            events.Add(moveEvent);
            unit.Cell = targetCell;
        }

        private static void ResolveSlam(CombatSimState state, CombatUnit unit, CombatAbilitySO ability, PlannedAction action, List<ResolutionEvent> events)
        {
            CombatUnit target = FindAirborneUnitAt(state, action.TargetCell);
            if (target == null)
            {
                events.Add(new ResolutionEvent(ResolutionEventType.Fizzle, unit.Id) { From = unit.Cell, To = unit.Cell, Wave = 0 });
                return;
            }

            Vector2Int landing = AbilityTargeting.GetLandingCell(unit, ability, action);
            if (!AbilityTargeting.IsLandingFree(state, unit, landing))
            {
                events.Add(new ResolutionEvent(ResolutionEventType.Fizzle, unit.Id) { From = unit.Cell, To = unit.Cell, Wave = 0 });
                return;
            }

            if (landing != unit.Cell)
            {
                ResolutionEvent moveEvent = new ResolutionEvent(ResolutionEventType.Move, unit.Id);
                moveEvent.From = unit.Cell;
                moveEvent.To = landing;
                moveEvent.Wave = 0;
                events.Add(moveEvent);
                unit.Cell = landing;
            }

            CombatEffects.ApplySlam(state, target, action.SlamCell, unit.Id, 0, events);
        }

        private static CombatUnit FindAirborneUnitAt(CombatSimState state, Vector2Int cell)
        {
            for (int i = 0; i < state.Units.Count; i++)
            {
                CombatUnit candidate = state.Units[i];
                if (candidate.Alive && candidate.Airborne && candidate.Cell == cell) return candidate;
            }

            return null;
        }

        private static void ResolveAttack(CombatSimState state, CombatSimState snapshot, CombatUnit unit, CombatAbilitySO ability, PlannedAction action, List<ResolutionEvent> events)
        {
            Vector2Int origin = unit.Cell;
            Vector2Int landing = AbilityTargeting.GetLandingCell(unit, ability, action);
            if (!AbilityTargeting.IsLandingFree(state, unit, landing))
            {
                events.Add(new ResolutionEvent(ResolutionEventType.Fizzle, unit.Id) { From = unit.Cell, To = unit.Cell, Wave = 0 });
                return;
            }

            if (landing != unit.Cell)
            {
                ResolutionEvent moveEvent = new ResolutionEvent(ResolutionEventType.Move, unit.Id);
                moveEvent.From = unit.Cell;
                moveEvent.To = landing;
                moveEvent.Wave = 0;
                events.Add(moveEvent);
                unit.Cell = landing;
            }

            List<Vector2Int> cells = AbilityTargeting.GetAffectedCells(snapshot, unit, ability, action);

            ResolutionEvent impactEvent = new ResolutionEvent(ResolutionEventType.Impact, unit.Id);
            impactEvent.Cells = cells;
            impactEvent.To = action.TargetCell;
            impactEvent.Wave = 0;
            impactEvent.Facing = unit.Cell != origin ? AbilityTargeting.DominantCardinal(origin, unit.Cell) : action.Direction;
            impactEvent.Projectile = ability.IgnoresHeight;
            events.Add(impactEvent);

            for (int i = 0; i < cells.Count; i++)
            {
                CombatUnit victim = state.GetUnitAt(cells[i]);
                if (victim == null || victim.Id == unit.Id) continue;

                CombatEffects.ApplyHit(state, victim, unit.Id, false, 0, events);

                if (ability.PushDistance > 0)
                {
                    Vector2Int pushDirection = ability.PushFromCenter ? AbilityTargeting.DominantCardinal(unit.Cell, victim.Cell) : action.Direction;
                    CombatEffects.ApplyPush(state, victim, pushDirection, ability.PushDistance, unit.Id, 0, events);
                }

                if (ability.LaunchesAirborne && !HasAirborneLaunchedBy(state, unit.Id))
                {
                    CombatEffects.ApplyLaunch(state, victim, action.Direction, unit.Id, 0, events);
                }
            }
        }

        private static bool HasAirborneLaunchedBy(CombatSimState state, int launcherId)
        {
            for (int i = 0; i < state.Units.Count; i++)
            {
                CombatUnit candidate = state.Units[i];
                if (candidate.Alive && candidate.Airborne && candidate.AirborneLauncherId == launcherId) return true;
            }

            return false;
        }

        private static void ResolveEnemyAttack(CombatSimState state, EnemyUnit enemy, int wave, List<ResolutionEvent> events)
        {
            if (enemy.Intent == null || !enemy.Intent.HasAttack) return;

            List<Vector2Int> attackCells = enemy.Intent.GetAttackCells(enemy.Cell);

            ResolutionEvent attackEvent = new ResolutionEvent(ResolutionEventType.EnemyAttack, enemy.Id);
            attackEvent.SourceId = enemy.Id;
            attackEvent.Cells = attackCells;
            attackEvent.Wave = wave;
            attackEvent.Projectile = true;
            attackEvent.Facing = enemy.Facing;
            events.Add(attackEvent);

            for (int i = 0; i < attackCells.Count; i++)
            {
                Vector2Int cell = attackCells[i];
                if (!state.Board.InBounds(cell)) continue;

                CombatUnit victim = state.GetUnitAt(cell);
                if (victim != null)
                {
                    CombatEffects.ApplyHit(state, victim, enemy.Id, false, wave, events);
                }
            }
        }

        private static void TryEndOfTurnMove(CombatSimState state, EnemyUnit enemy, int wave, List<ResolutionEvent> events)
        {
            Vector2Int[] offsets = enemy.Definition.MoveOffsets;
            if (offsets == null || offsets.Length == 0) return;

            Vector2Int destination = enemy.Cell;

            for (int i = 0; i < offsets.Length; i++)
            {
                Vector2Int cell = enemy.Cell + AbilityTargeting.RotateOffset(offsets[i], enemy.Facing);

                if (!state.Board.InBounds(cell) || !state.IsCellFree(cell))
                {
                    enemy.Facing = RotateClockwise(enemy.Facing);

                    ResolutionEvent rotateEvent = new ResolutionEvent(ResolutionEventType.Rotate, enemy.Id);
                    rotateEvent.From = enemy.Cell;
                    rotateEvent.To = enemy.Cell;
                    rotateEvent.Facing = enemy.Facing;
                    rotateEvent.Wave = wave;
                    events.Add(rotateEvent);
                    return;
                }

                destination = cell;
            }

            ResolutionEvent moveEvent = new ResolutionEvent(ResolutionEventType.Move, enemy.Id);
            moveEvent.From = enemy.Cell;
            moveEvent.To = destination;
            moveEvent.Wave = wave;
            events.Add(moveEvent);
            enemy.Cell = destination;
        }

        private static Vector2Int RotateClockwise(Vector2Int direction)
        {
            if (direction == new Vector2Int(1, 0)) return new Vector2Int(0, -1);
            if (direction == new Vector2Int(0, -1)) return new Vector2Int(-1, 0);
            if (direction == new Vector2Int(-1, 0)) return new Vector2Int(0, 1);
            return new Vector2Int(1, 0);
        }
    }
}
