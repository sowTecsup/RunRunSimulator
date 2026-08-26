using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public struct EnemySpawn
    {
        public Vector2Int Cell;
        public Vector2Int Facing;
    }

    [CreateAssetMenu(fileName = "BoardLayout", menuName = "MoriMonchi/Combat Prototype/Board Layout")]
    public class BoardLayoutSO : ScriptableObject
    {
        public string[] HeightRows;
        public string[] SpawnRows;

        public int Width => (HeightRows != null && HeightRows.Length > 0) ? HeightRows[0].Length : 0;

        public int Depth => HeightRows != null ? HeightRows.Length : 0;

        public int GetElevation(int x, int z)
        {
            if (HeightRows == null || z < 0 || z >= HeightRows.Length)
                return 0;

            string row = HeightRows[z];
            if (row == null || x < 0 || x >= row.Length)
                return 0;

            char c = row[x];
            return char.IsDigit(c) ? c - '0' : 0;
        }

        public bool IsHole(int x, int z)
        {
            if (HeightRows == null || z < 0 || z >= HeightRows.Length)
                return false;

            string row = HeightRows[z];
            if (row == null || x < 0 || x >= row.Length)
                return false;

            return row[x] == '.';
        }

        public List<Vector2Int> GetPlayerSpawns()
        {
            List<Vector2Int> spawns = new List<Vector2Int>();

            if (SpawnRows == null)
                return spawns;

            for (int z = 0; z < SpawnRows.Length; z++)
            {
                string row = SpawnRows[z];
                if (row == null)
                    continue;

                for (int x = 0; x < row.Length; x++)
                {
                    if (row[x] == 'P')
                        spawns.Add(new Vector2Int(x, z));
                }
            }

            return spawns;
        }

        public List<EnemySpawn> GetEnemySpawnsWithFacing()
        {
            List<EnemySpawn> spawns = new List<EnemySpawn>();

            if (SpawnRows == null)
                return spawns;

            for (int z = 0; z < SpawnRows.Length; z++)
            {
                string row = SpawnRows[z];
                if (row == null)
                    continue;

                for (int x = 0; x < row.Length; x++)
                {
                    char c = row[x];
                    Vector2Int facing;

                    if (c == 'E' || c == '>')
                        facing = new Vector2Int(1, 0);
                    else if (c == '<')
                        facing = new Vector2Int(-1, 0);
                    else if (c == '^')
                        facing = new Vector2Int(0, 1);
                    else if (c == 'v')
                        facing = new Vector2Int(0, -1);
                    else
                        continue;

                    spawns.Add(new EnemySpawn { Cell = new Vector2Int(x, z), Facing = facing });
                }
            }

            return spawns;
        }
    }
}
