using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public class NightSpawner : MonoBehaviour
    {
        [SerializeField] private BoardHighlighter highlighter;
        [SerializeField] private int baseWaveSize = 2;
        [SerializeField] private int extraEveryWaves = 3;
        [SerializeField] private Color markerColor = new Color(0.78f, 0.49f, 1f);
        [SerializeField] private string markerText = "×";
        [SerializeField] private float markerFontSize = 4f;
        [SerializeField] private float markerHeight = 0.9f;

        private readonly List<EnemySpawn> pendingWave = new List<EnemySpawn>();
        private readonly List<GameObject> spawnMarkers = new List<GameObject>();
        private int waveNumber;

        public int PendingCount => pendingWave.Count;

        public void ResetForEncounter()
        {
            waveNumber = 0;
            pendingWave.Clear();
            if (highlighter != null) highlighter.Clear(HighlightKind.Spawn);
            ClearMarkers();
        }

        public void PrepareNextWave(CombatSimState canonical, Vector2Int seedCell)
        {
            waveNumber++;
            int size = NightWaves.WaveSize(waveNumber, baseWaveSize, extraEveryWaves);
            pendingWave.Clear();
            List<Vector2Int> cells = NightWaves.FindEdgeSpawnCells(canonical, seedCell, size, null);
            for (int i = 0; i < cells.Count; i++)
                pendingWave.Add(new EnemySpawn { Cell = cells[i], Facing = AbilityTargeting.DominantCardinal(cells[i], seedCell) });
            PaintTelegraph(canonical);
        }

        public List<EnemySpawn> ConsumeWave(CombatSimState canonical, Vector2Int seedCell)
        {
            List<EnemySpawn> result = new List<EnemySpawn>();
            List<Vector2Int> taken = new List<Vector2Int>();
            for (int i = 0; i < pendingWave.Count; i++)
            {
                EnemySpawn spawn = pendingWave[i];
                if (!canonical.IsCellFree(spawn.Cell) || taken.Contains(spawn.Cell))
                {
                    List<Vector2Int> alt = NightWaves.FindEdgeSpawnCells(canonical, seedCell, 1, taken);
                    if (alt.Count == 0) continue;
                    spawn = new EnemySpawn { Cell = alt[0], Facing = AbilityTargeting.DominantCardinal(alt[0], seedCell) };
                }
                taken.Add(spawn.Cell);
                result.Add(spawn);
            }
            pendingWave.Clear();
            if (highlighter != null) highlighter.Clear(HighlightKind.Spawn);
            ClearMarkers();
            return result;
        }

        private void PaintTelegraph(CombatSimState state)
        {
            if (highlighter != null)
            {
                List<Vector2Int> cells = new List<Vector2Int>();
                for (int i = 0; i < pendingWave.Count; i++) cells.Add(pendingWave[i].Cell);
                highlighter.Show(HighlightKind.Spawn, cells);
            }

            ClearMarkers();
            if (state == null) return;

            for (int i = 0; i < pendingWave.Count; i++)
            {
                Vector3 worldPosition = state.Board.CellToWorld(pendingWave[i].Cell) + Vector3.up * markerHeight;

                GameObject markerObject = new GameObject("SpawnMarker");
                markerObject.transform.SetParent(transform);
                markerObject.transform.position = worldPosition;
                markerObject.AddComponent<WorldLabelBillboard>();

                TextMeshPro label = markerObject.AddComponent<TextMeshPro>();
                label.text = markerText;
                label.fontSize = markerFontSize;
                label.alignment = TextAlignmentOptions.Center;
                label.color = markerColor;

                spawnMarkers.Add(markerObject);
            }
        }

        private void ClearMarkers()
        {
            for (int i = 0; i < spawnMarkers.Count; i++)
                Object.Destroy(spawnMarkers[i]);
            spawnMarkers.Clear();
        }
    }
}
