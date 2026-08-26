using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class CombatBoard
    {
        public const float CellSize = 1f;
        public const float LevelHeight = 0.5f;

        public int Width { get; }
        public int Depth { get; }

        private readonly int[,] _elevations;

        public CombatBoard(BoardLayoutSO layout)
        {
            Width = layout.Width;
            Depth = layout.Depth;
            _elevations = new int[Width, Depth];

            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    _elevations[x, z] = layout.GetElevation(x, z);
                }
            }
        }

        public bool InBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < Width && cell.y >= 0 && cell.y < Depth;
        }

        public int GetElevation(Vector2Int cell)
        {
            if (!InBounds(cell))
            {
                return 0;
            }

            return _elevations[cell.x, cell.y];
        }

        public Vector3 CellToWorld(Vector2Int cell)
        {
            float x = cell.x * CellSize + CellSize * 0.5f;
            float y = GetElevation(cell) * LevelHeight;
            float z = cell.y * CellSize + CellSize * 0.5f;
            return new Vector3(x, y, z);
        }

        public Vector2Int WorldToCell(Vector3 world)
        {
            int x = Mathf.FloorToInt(world.x / CellSize);
            int z = Mathf.FloorToInt(world.z / CellSize);
            return new Vector2Int(x, z);
        }
    }
}
