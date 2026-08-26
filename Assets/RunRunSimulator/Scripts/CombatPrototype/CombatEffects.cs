using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public static class CombatEffects
    {
        public static void ApplyHit(CombatSimState s, CombatUnit target, int sourceId, bool environmental, int wave, List<ResolutionEvent> events)
        {
            if (target == null || !target.Alive)
            {
                return;
            }

            target.Ticks = Mathf.Max(0, target.Ticks - 1);

            if (target is EnemyUnit enemy)
            {
                enemy.WasHitThisBeat = true;
            }

            events.Add(new ResolutionEvent(ResolutionEventType.Hit, target.Id) { SourceId = sourceId, TicksAfter = target.Ticks, Environmental = environmental, Wave = wave });
        }

        public static void ApplyPush(CombatSimState s, CombatUnit target, Vector2Int direction, int distance, int sourceId, int wave, List<ResolutionEvent> events)
        {
            if (target == null || !target.Alive || target.Airborne || direction == Vector2Int.zero)
            {
                return;
            }

            Vector2Int start = target.Cell;
            Vector2Int current = start;
            bool hitWall = false;
            CombatUnit blocker = null;

            for (int step = 0; step < distance; step++)
            {
                Vector2Int next = current + direction;

                if (!s.Board.InBounds(next) || s.Board.GetElevation(next) > s.Board.GetElevation(current))
                {
                    hitWall = true;
                    break;
                }

                CombatUnit occupant = s.GetUnitAt(next);
                if (occupant != null)
                {
                    blocker = occupant;
                    break;
                }

                current = next;
            }

            bool fell = !hitWall && blocker == null && s.Board.GetElevation(start) - s.Board.GetElevation(current) >= 2;

            events.Add(new ResolutionEvent(ResolutionEventType.Push, target.Id) { SourceId = sourceId, From = start, To = current, Wave = wave });

            target.Cell = current;

            if (hitWall)
            {
                ApplyHit(s, target, sourceId, true, wave, events);
            }
            else if (blocker != null)
            {
                ApplyHit(s, target, sourceId, true, wave, events);
                ApplyHit(s, blocker, sourceId, true, wave, events);
            }
            else if (fell)
            {
                ApplyHit(s, target, sourceId, true, wave, events);
            }
        }

        public static void ApplyLaunch(CombatSimState s, CombatUnit target, Vector2Int direction, int launcherId, int wave, List<ResolutionEvent> events)
        {
            if (target == null || !target.Alive || target.Airborne)
            {
                return;
            }

            Vector2Int landingCell = target.Cell + direction;

            target.Airborne = true;
            target.AirborneJustLaunched = true;
            target.WasAirborneThisPhase = true;
            target.AirborneDirection = direction;
            target.AirborneLandingCell = landingCell;
            target.AirborneLauncherId = launcherId;

            events.Add(new ResolutionEvent(ResolutionEventType.Launch, target.Id) { From = target.Cell, To = landingCell, SourceId = launcherId, Wave = wave });
        }

        public static void ApplyLanding(CombatSimState s, CombatUnit unit, int wave, List<ResolutionEvent> events)
        {
            if (!unit.Airborne)
            {
                return;
            }

            Vector2Int oldCell = unit.Cell;
            Vector2Int destination = unit.AirborneLandingCell;

            if (!s.Board.InBounds(destination))
            {
                destination = unit.Cell;
            }

            CombatUnit occupant = s.GetUnitAt(destination);
            if (occupant != null)
            {
                ApplyHit(s, unit, -1, true, wave, events);
                ApplyHit(s, occupant, -1, true, wave, events);
                destination = FindFreeCell(s, destination, unit.AirborneDirection, unit.Cell);
            }

            if (s.Board.GetElevation(unit.Cell) - s.Board.GetElevation(destination) >= 2)
            {
                ApplyHit(s, unit, -1, true, wave, events);
            }

            events.Add(new ResolutionEvent(ResolutionEventType.Land, unit.Id) { From = oldCell, To = destination, Wave = wave });

            unit.Cell = destination;
            unit.Airborne = false;
            unit.AirborneJustLaunched = false;
            unit.AirborneLauncherId = -1;
        }

        public static void ApplySlam(CombatSimState s, CombatUnit target, Vector2Int impactCell, int sourceId, int wave, List<ResolutionEvent> events)
        {
            if (target == null || !target.Alive || !target.Airborne)
            {
                return;
            }

            ApplyHit(s, target, sourceId, false, wave, events);

            Vector2Int dir = AbilityTargeting.DominantCardinal(target.Cell, impactCell);
            Vector2Int destination = impactCell;

            if (AbilityTargeting.IsWall(s.Board, target.Cell, impactCell))
            {
                ApplyHit(s, target, sourceId, true, wave, events);
                Vector2Int previous = impactCell - dir;
                destination = s.Board.InBounds(previous) ? previous : target.Cell;
            }

            CombatUnit occupant = s.GetUnitAt(destination);
            if (occupant != null && occupant != target)
            {
                ApplyHit(s, target, sourceId, true, wave, events);
                ApplyHit(s, occupant, sourceId, true, wave, events);
                destination = FindFreeCell(s, destination, dir, target.Cell);
            }

            if (s.Board.GetElevation(target.Cell) - s.Board.GetElevation(destination) >= 2)
            {
                ApplyHit(s, target, sourceId, true, wave, events);
            }

            events.Add(new ResolutionEvent(ResolutionEventType.Land, target.Id) { From = target.Cell, To = destination, SourceId = sourceId, Wave = wave });

            target.Cell = destination;
            target.Airborne = false;
            target.AirborneJustLaunched = false;
            target.AirborneLauncherId = -1;
        }

        public static void CollectDeaths(CombatSimState s, int wave, List<ResolutionEvent> events)
        {
            for (int i = 0; i < s.Units.Count; i++)
            {
                CombatUnit unit = s.Units[i];
                if (unit.Alive && unit.Ticks <= 0)
                {
                    unit.Alive = false;
                    events.Add(new ResolutionEvent(ResolutionEventType.Die, unit.Id) { From = unit.Cell, To = unit.Cell, Wave = wave });
                }
            }
        }

        private static Vector2Int FindFreeCell(CombatSimState s, Vector2Int from, Vector2Int direction, Vector2Int fallback)
        {
            if (direction != Vector2Int.zero)
            {
                Vector2Int probe = from + direction;
                while (s.Board.InBounds(probe))
                {
                    if (s.IsCellFree(probe))
                    {
                        return probe;
                    }
                    probe += direction;
                }
            }

            Vector2Int[] cardinals = new Vector2Int[]
            {
                new Vector2Int(0, 1),
                new Vector2Int(1, 0),
                new Vector2Int(0, -1),
                new Vector2Int(-1, 0)
            };

            for (int i = 0; i < cardinals.Length; i++)
            {
                Vector2Int candidate = from + cardinals[i];
                if (s.IsCellFree(candidate))
                {
                    return candidate;
                }
            }

            return fallback;
        }
    }
}
