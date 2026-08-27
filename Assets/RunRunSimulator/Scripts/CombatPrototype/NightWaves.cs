using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public static class NightWaves
    {
        public static int WaveSize(int waveNumber, int baseWaveSize, int extraEveryWaves)
        {
            return baseWaveSize + (waveNumber - 1) / extraEveryWaves;
        }

        public static List<Vector2Int> FindSpawnCells(CombatSimState state, Vector2Int seedCell, int count, int startRadius, List<Vector2Int> exclude)
        {
            List<Vector2Int> found = new List<Vector2Int>();
            int maxRadius = Mathf.Max(state.Board.Width, state.Board.Depth);
            for (int radius = startRadius; radius <= maxRadius && found.Count < count; radius++)
            {
                for (int dx = -radius; dx <= radius && found.Count < count; dx++)
                    for (int dy = -radius; dy <= radius && found.Count < count; dy++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radius) continue;
                        Vector2Int cell = seedCell + new Vector2Int(dx, dy);
                        if (!state.Board.InBounds(cell) || !state.IsCellFree(cell)) continue;
                        if (exclude != null && exclude.Contains(cell)) continue;
                        if (!found.Contains(cell)) found.Add(cell);
                    }
            }
            return found;
        }

        static readonly Vector2Int[] CardinalDirections = new[]
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1)
        };

        public static List<Vector2Int> FindEdgeSpawnCells(CombatSimState state, Vector2Int seedCell, int count, List<Vector2Int> exclude)
        {
            List<Vector2Int> candidates = new List<Vector2Int>();
            for (int x = 0; x < state.Board.Width; x++)
            {
                for (int y = 0; y < state.Board.Depth; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (!state.Board.InBounds(cell) || !state.IsCellFree(cell)) continue;
                    if (exclude != null && exclude.Contains(cell)) continue;

                    bool isEdge = false;
                    for (int i = 0; i < CardinalDirections.Length; i++)
                    {
                        if (!state.Board.InBounds(cell + CardinalDirections[i]))
                        {
                            isEdge = true;
                            break;
                        }
                    }
                    if (!isEdge) continue;

                    candidates.Add(cell);
                }
            }

            candidates.Sort((a, b) =>
            {
                int distA = AbilityTargeting.Chebyshev(a, seedCell);
                int distB = AbilityTargeting.Chebyshev(b, seedCell);
                if (distA != distB) return distA.CompareTo(distB);
                if (a.y != b.y) return a.y.CompareTo(b.y);
                return a.x.CompareTo(b.x);
            });

            if (candidates.Count > count) candidates.RemoveRange(count, candidates.Count - count);
            return candidates;
        }

        public static Vector2Int EdgeOutwardDirection(CombatBoard board, Vector2Int cell)
        {
            for (int i = 0; i < CardinalDirections.Length; i++)
            {
                Vector2Int d = CardinalDirections[i];
                if (!board.InBounds(cell + d)) return d;
            }
            return Vector2Int.zero;
        }
    }
}
