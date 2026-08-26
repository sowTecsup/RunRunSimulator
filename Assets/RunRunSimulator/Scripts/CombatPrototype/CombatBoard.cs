using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class CombatBoard
    {
        public const float CellSize = 1f;
        public readonly float LevelHeight;

        public int Width { get; }
        public int Depth { get; }

        private readonly int[,] _elevations;
        private readonly bool[,] _holes;

        public CombatBoard(BoardLayoutSO layout, float levelHeight)
        {
            LevelHeight = levelHeight;
            Width = layout.Width;
            Depth = layout.Depth;
            _elevations = new int[Width, Depth];
            _holes = new bool[Width, Depth];

            for (int x = 0; x < Width; x++)
            {
                for (int z = 0; z < Depth; z++)
                {
                    _elevations[x, z] = layout.GetElevation(x, z);
                    _holes[x, z] = layout.IsHole(x, z);
                }
            }
        }

        public bool InBounds(Vector2Int cell)
        {
            if (cell.x < 0 || cell.x >= Width || cell.y < 0 || cell.y >= Depth)
                return false;

            return !_holes[cell.x, cell.y];
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
