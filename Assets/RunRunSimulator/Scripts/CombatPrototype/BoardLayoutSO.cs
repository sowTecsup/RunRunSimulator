using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
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

        public List<Vector2Int> GetEnemySpawns()
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
                    if (row[x] == 'E')
                        spawns.Add(new Vector2Int(x, z));
                }
            }

            return spawns;
        }
    }
}
