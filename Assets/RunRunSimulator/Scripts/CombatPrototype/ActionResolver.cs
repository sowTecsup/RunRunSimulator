using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public static class ActionResolver
    {
        private static readonly Vector2Int[] ReactionDirections =
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0)
        };

        public static List<ResolutionEvent> ResolveBeat(CombatSimState state, Beat beat)
        {
            List<ResolutionEvent> events = new List<ResolutionEvent>();
            Dictionary<int, Vector2Int> lastAttackerCell = new Dictionary<int, Vector2Int>();

            for (int i = 0; i < state.Units.Count; i++)
            {
                if (state.Units[i] is EnemyUnit resetEnemy) resetEnemy.WasHitThisBeat = false;
            }

            CombatSimState snapshot = state.Clone();

            for (int i = 0; i < beat.Actions.Count; i++)
            {
                ResolvePlayerAction(state, snapshot, beat.Actions[i], events, lastAttackerCell);
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
            ResolveReactions(state, lastAttackerCell, events);

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
                if (enemies[i].Alive) ResolveEnemyAction(state, enemies[i], 1 + i, events);
            }

            for (int i = 0; i < state.Units.Count; i++)
            {
                state.Units[i].WasAirborneThisPhase = false;
            }

            return events;
        }

        private static void ResolvePlayerAction(CombatSimState state, CombatSimState snapshot, PlannedAction action, List<ResolutionEvent> events, Dictionary<int, Vector2Int> lastAttackerCell)
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
                ResolveSlam(state, unit, action, events);
                return;
            }

            ResolveAttack(state, snapshot, unit, ability, action, events, lastAttackerCell);
        }

        private static void ResolveMovement(CombatSimState state, CombatUnit unit, Vector2Int targetCell, List<ResolutionEvent> events)
        {
            if (!state.IsCellFree(targetCell)) return;

            ResolutionEvent moveEvent = new ResolutionEvent(ResolutionEventType.Move, unit.Id);
            moveEvent.From = unit.Cell;
            moveEvent.To = targetCell;
            moveEvent.Wave = 0;
            events.Add(moveEvent);
            unit.Cell = targetCell;
        }

        private static void ResolveSlam(CombatSimState state, CombatUnit unit, PlannedAction action, List<ResolutionEvent> events)
        {
            CombatUnit target = FindAirborneUnitAt(state, action.TargetCell);
            if (target == null) return;

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

        private static void ResolveAttack(CombatSimState state, CombatSimState snapshot, CombatUnit unit, CombatAbilitySO ability, PlannedAction action, List<ResolutionEvent> events, Dictionary<int, Vector2Int> lastAttackerCell)
        {
            CombatUnit snapUnit = snapshot.GetUnit(unit.Id);
            List<Vector2Int> cells = AbilityTargeting.GetAffectedCells(snapshot, snapUnit, ability, action);

            for (int i = 0; i < cells.Count; i++)
            {
                CombatUnit victim = state.GetUnitAt(cells[i]);
                if (victim == null || victim.Id == unit.Id) continue;

                CombatEffects.ApplyHit(state, victim, unit.Id, false, 0, events);
                if (victim is EnemyUnit enemyVictim) lastAttackerCell[enemyVictim.Id] = unit.Cell;

                if (ability.PushDistance > 0)
                {
                    CombatEffects.ApplyPush(state, victim, action.Direction, ability.PushDistance, unit.Id, 0, events);
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

        private static void ResolveReactions(CombatSimState state, Dictionary<int, Vector2Int> lastAttackerCell, List<ResolutionEvent> events)
        {
            List<EnemyUnit> enemies = state.GetEnemies();
            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyUnit enemy = enemies[i];
                if (!enemy.WasHitThisBeat || enemy.Airborne) continue;
                if (!lastAttackerCell.TryGetValue(enemy.Id, out Vector2Int attackerCell)) continue;

                ResolveSingleReaction(state, enemy, attackerCell, events);
            }
        }

        private static void ResolveSingleReaction(CombatSimState state, EnemyUnit enemy, Vector2Int attackerCell, List<ResolutionEvent> events)
        {
            Vector2Int start = enemy.Cell;
            Vector2Int current = start;

            for (int step = 0; step < enemy.Definition.ReactionDistance; step++)
            {
                int currentDistance = AbilityTargeting.Chebyshev(current, attackerCell);
                int currentElevation = state.Board.GetElevation(current);
                Vector2Int? best = null;
                int bestDistance = currentDistance;

                for (int d = 0; d < ReactionDirections.Length; d++)
                {
                    Vector2Int candidate = current + ReactionDirections[d];
                    if (!state.IsCellFree(candidate)) continue;
                    if (Mathf.Abs(state.Board.GetElevation(candidate) - currentElevation) > 1) continue;

                    int distance = AbilityTargeting.Chebyshev(candidate, attackerCell);
                    if (distance > bestDistance)
                    {
                        bestDistance = distance;
                        best = candidate;
                    }
                }

                if (best == null) break;
                current = best.Value;
            }

            if (current == start) return;

            enemy.Cell = current;
            enemy.HasReacted = true;

            ResolutionEvent reactionEvent = new ResolutionEvent(ResolutionEventType.Reaction, enemy.Id);
            reactionEvent.From = start;
            reactionEvent.To = current;
            reactionEvent.Wave = 3;
            events.Add(reactionEvent);
        }

        private static void ResolveEnemyAction(CombatSimState state, EnemyUnit enemy, int wave, List<ResolutionEvent> events)
        {
            if (enemy.Intent == null) return;

            if (!enemy.HasReacted && enemy.Intent.MoveSteps.Count > 0) ResolveEnemyMovement(state, enemy, wave, events);
            if (enemy.Intent.HasAttack) ResolveEnemyAttack(state, enemy, wave, events);

            enemy.HasReacted = false;
            CombatEffects.CollectDeaths(state, wave, events);
        }

        private static void ResolveEnemyMovement(CombatSimState state, EnemyUnit enemy, int wave, List<ResolutionEvent> events)
        {
            Vector2Int start = enemy.Cell;
            Vector2Int current = start;

            for (int i = 0; i < enemy.Intent.MoveSteps.Count; i++)
            {
                Vector2Int step = enemy.Intent.MoveSteps[i];
                if (!state.IsCellFree(step)) break;
                if (Mathf.Abs(state.Board.GetElevation(step) - state.Board.GetElevation(current)) > 1) break;

                current = step;
            }

            if (current == start) return;

            enemy.Cell = current;

            ResolutionEvent moveEvent = new ResolutionEvent(ResolutionEventType.Move, enemy.Id);
            moveEvent.From = start;
            moveEvent.To = current;
            moveEvent.Wave = wave;
            events.Add(moveEvent);
        }

        private static void ResolveEnemyAttack(CombatSimState state, EnemyUnit enemy, int wave, List<ResolutionEvent> events)
        {
            List<Vector2Int> attackCells = enemy.Intent.GetAttackCells(enemy.Cell);

            ResolutionEvent attackEvent = new ResolutionEvent(ResolutionEventType.EnemyAttack, enemy.Id);
            attackEvent.SourceId = enemy.Id;
            attackEvent.Cells = attackCells;
            attackEvent.Wave = wave;
            events.Add(attackEvent);

            for (int i = 0; i < attackCells.Count; i++)
            {
                Vector2Int cell = attackCells[i];
                if (!state.Board.InBounds(cell) || AbilityTargeting.IsWall(state.Board, enemy.Cell, cell)) break;

                CombatUnit victim = state.GetUnitAt(cell);
                if (victim != null)
                {
                    CombatEffects.ApplyHit(state, victim, enemy.Id, false, wave, events);
                    break;
                }
            }
        }
    }
}
