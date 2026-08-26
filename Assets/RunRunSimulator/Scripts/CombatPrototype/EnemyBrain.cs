using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public static class EnemyBrain
    {
        private enum ScoreMode { Manhattan, Chebyshev, Alignment }

        private static readonly Vector2Int[] CardinalOrder =
        {
            new Vector2Int(1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(-1, 0)
        };

        public static EnemyIntent ComputeIntent(CombatSimState state, EnemyUnit enemy)
        {
            List<PlayerUnit> players = state.GetPlayers();
            if (players.Count == 0) return new EnemyIntent();

            PlayerUnit target = SelectTarget(enemy, players);

            switch (enemy.Definition.Pattern)
            {
                case EnemyPattern.ChaseMelee: return ComputeChaseMelee(state, enemy, target);
                case EnemyPattern.RangedLine: return ComputeRangedLine(state, enemy, target);
                default: return new EnemyIntent();
            }
        }

        private static PlayerUnit SelectTarget(EnemyUnit enemy, List<PlayerUnit> players)
        {
            PlayerUnit best = players[0];
            int bestDistance = AbilityTargeting.Chebyshev(enemy.Cell, best.Cell);

            for (int i = 1; i < players.Count; i++)
            {
                PlayerUnit candidate = players[i];
                int distance = AbilityTargeting.Chebyshev(enemy.Cell, candidate.Cell);
                bool preferCandidate = distance < bestDistance || (distance == bestDistance && IsPreferredTiebreak(candidate.Cell, best.Cell));

                if (preferCandidate)
                {
                    best = candidate;
                    bestDistance = distance;
                }
            }

            return best;
        }

        private static bool IsPreferredTiebreak(Vector2Int candidateCell, Vector2Int bestCell)
        {
            return candidateCell.x != bestCell.x ? candidateCell.x > bestCell.x : candidateCell.y > bestCell.y;
        }

        private static EnemyIntent ComputeChaseMelee(CombatSimState state, EnemyUnit enemy, PlayerUnit target)
        {
            EnemyIntent intent = new EnemyIntent();
            Vector2Int currentCell = StepGreedy(state, intent, enemy.Cell, target.Cell, ScoreMode.Manhattan, enemy.Definition.MoveRange, false);
            if (AbilityTargeting.Manhattan(currentCell, target.Cell) != 1) return intent;

            int elevationGap = state.Board.GetElevation(target.Cell) - state.Board.GetElevation(currentCell);
            if (elevationGap >= 2) return intent;

            intent.AttackDirection = AbilityTargeting.DominantCardinal(currentCell, target.Cell);
            intent.AttackOffsets = new Vector2Int[] { new Vector2Int(1, 0) };
            return intent;
        }

        private static EnemyIntent ComputeRangedLine(CombatSimState state, EnemyUnit enemy, PlayerUnit target)
        {
            EnemyIntent intent = new EnemyIntent();
            Vector2Int currentCell = enemy.Cell;
            int attackRange = enemy.Definition.AttackRange;
            int startDistance = AbilityTargeting.Chebyshev(currentCell, target.Cell);

            if (IsAligned(currentCell, target.Cell) && startDistance <= attackRange)
            {
                if (startDistance < enemy.Definition.PreferredMin)
                {
                    currentCell = StepGreedy(state, intent, currentCell, target.Cell, ScoreMode.Chebyshev, 2, true);
                }

                if (IsAligned(currentCell, target.Cell)) CommitLineAttack(intent, currentCell, target.Cell, attackRange);
                return intent;
            }

            currentCell = StepGreedy(state, intent, currentCell, target.Cell, ScoreMode.Alignment, enemy.Definition.MoveRange, false);
            if (IsAligned(currentCell, target.Cell) && AbilityTargeting.Chebyshev(currentCell, target.Cell) <= attackRange)
            {
                CommitLineAttack(intent, currentCell, target.Cell, attackRange);
            }

            return intent;
        }

        private static void CommitLineAttack(EnemyIntent intent, Vector2Int fromCell, Vector2Int targetCell, int attackRange)
        {
            intent.AttackDirection = AbilityTargeting.DominantCardinal(fromCell, targetCell);
            intent.AttackOffsets = BuildLineOffsets(attackRange);
        }

        private static Vector2Int StepGreedy(CombatSimState state, EnemyIntent intent, Vector2Int currentCell, Vector2Int targetCell, ScoreMode mode, int maxSteps, bool maximize)
        {
            int currentScore = ComputeScore(mode, currentCell, targetCell);

            for (int step = 0; step < maxSteps; step++)
            {
                bool found = false;
                Vector2Int bestCell = currentCell;
                int bestScore = currentScore;

                for (int i = 0; i < CardinalOrder.Length; i++)
                {
                    Vector2Int candidate = currentCell + CardinalOrder[i];
                    if (!IsStepValid(state, currentCell, candidate)) continue;

                    int candidateScore = ComputeScore(mode, candidate, targetCell);
                    bool better = maximize ? candidateScore > bestScore : candidateScore < bestScore;
                    if (!found || better)
                    {
                        found = true;
                        bestCell = candidate;
                        bestScore = candidateScore;
                    }
                }

                bool improved = maximize ? bestScore > currentScore : bestScore < currentScore;
                if (!found || !improved) break;

                currentCell = bestCell;
                currentScore = bestScore;
                intent.MoveSteps.Add(currentCell);
            }

            return currentCell;
        }

        private static int ComputeScore(ScoreMode mode, Vector2Int cell, Vector2Int targetCell)
        {
            if (mode == ScoreMode.Manhattan) return AbilityTargeting.Manhattan(cell, targetCell);
            if (mode == ScoreMode.Chebyshev) return AbilityTargeting.Chebyshev(cell, targetCell);
            return Mathf.Min(Mathf.Abs(cell.x - targetCell.x), Mathf.Abs(cell.y - targetCell.y));
        }

        private static bool IsStepValid(CombatSimState state, Vector2Int fromCell, Vector2Int toCell)
        {
            if (!state.IsCellFree(toCell)) return false;
            return Mathf.Abs(state.Board.GetElevation(toCell) - state.Board.GetElevation(fromCell)) <= 1;
        }

        private static bool IsAligned(Vector2Int cell, Vector2Int targetCell)
        {
            return cell != targetCell && (cell.x == targetCell.x || cell.y == targetCell.y);
        }

        private static Vector2Int[] BuildLineOffsets(int attackRange)
        {
            Vector2Int[] offsets = new Vector2Int[attackRange];
            for (int i = 0; i < attackRange; i++)
            {
                offsets[i] = new Vector2Int(i + 1, 0);
            }

            return offsets;
        }
    }
}
