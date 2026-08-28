using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public enum HighlightKind { Template, Intent, Path, Landing, Selection, Spawn }

    public class BoardHighlighter : MonoBehaviour
    {
        [SerializeField] private CombatBoardBuilder builder;
        [SerializeField] private Color templateColor = new Color(0.2f, 0.9f, 1f, 0.45f);
        [SerializeField] private Color intentColor = new Color(1f, 0.25f, 0.2f, 0.45f);
        [SerializeField] private Color pathColor = new Color(1f, 0.9f, 0.2f, 0.45f);
        [SerializeField] private Color landingColor = new Color(0.3f, 1f, 0.35f, 0.45f);
        [SerializeField] private Color selectionColor = new Color(1f, 1f, 1f, 0.65f);
        [SerializeField] private Color spawnColor = new Color(0.75f, 0.35f, 1f, 0.5f);
        [SerializeField] private float stackStep = 0.012f;

        private readonly List<GameObject> pool = new List<GameObject>();
        private readonly Dictionary<HighlightKind, List<GameObject>> active = new Dictionary<HighlightKind, List<GameObject>>();
        private readonly Dictionary<HighlightKind, List<Vector2Int>> activeCells = new Dictionary<HighlightKind, List<Vector2Int>>();
        private readonly Dictionary<HighlightKind, Material> materials = new Dictionary<HighlightKind, Material>();

        public void Show(HighlightKind kind, IEnumerable<Vector2Int> cells)
        {
            Clear(kind);

            if (cells == null)
                return;

            CombatBoard board = builder.Board;
            if (board == null)
                return;

            List<GameObject> quads = GetActiveList(kind);
            List<Vector2Int> cellList = GetActiveCellList(kind);
            Material material = GetMaterial(kind);
            float height = 0.02f + Priority(kind) * stackStep;

            foreach (Vector2Int cell in cells)
            {
                if (!board.InBounds(cell))
                    continue;

                GameObject quad = GetPooledQuad();
                quad.GetComponent<MeshRenderer>().sharedMaterial = material;
                quad.transform.position = board.CellToWorld(cell) + Vector3.up * height;
                quad.SetActive(true);
                quads.Add(quad);
                cellList.Add(cell);
            }

            RefreshVisibility();
        }

        public void Clear(HighlightKind kind)
        {
            if (active.TryGetValue(kind, out List<GameObject> quads))
            {
                for (int i = 0; i < quads.Count; i++)
                {
                    quads[i].SetActive(false);
                    pool.Add(quads[i]);
                }

                quads.Clear();
            }

            if (activeCells.TryGetValue(kind, out List<Vector2Int> cellList))
                cellList.Clear();

            RefreshVisibility();
        }

        public void ClearAll()
        {
            foreach (HighlightKind kind in active.Keys)
                Clear(kind);
        }

        private static int Priority(HighlightKind kind)
        {
            switch (kind)
            {
                case HighlightKind.Selection: return 5;
                case HighlightKind.Spawn: return 4;
                case HighlightKind.Intent: return 3;
                case HighlightKind.Landing: return 2;
                case HighlightKind.Path: return 1;
                default: return 0;
            }
        }

        private void RefreshVisibility()
        {
            var covered = new HashSet<Vector2Int>();
            var kinds = new List<HighlightKind>(active.Keys);
            kinds.Sort((a, b) => Priority(b).CompareTo(Priority(a)));

            foreach (HighlightKind kind in kinds)
            {
                List<GameObject> quads = active[kind];
                List<Vector2Int> cells = activeCells.TryGetValue(kind, out List<Vector2Int> list) ? list : null;

                if (cells != null)
                {
                    for (int i = 0; i < quads.Count && i < cells.Count; i++)
                        quads[i].SetActive(!covered.Contains(cells[i]));

                    for (int i = 0; i < cells.Count; i++)
                        covered.Add(cells[i]);
                }
            }
        }

        private List<GameObject> GetActiveList(HighlightKind kind)
        {
            if (!active.TryGetValue(kind, out List<GameObject> quads))
            {
                quads = new List<GameObject>();
                active[kind] = quads;
            }

            return quads;
        }

        private List<Vector2Int> GetActiveCellList(HighlightKind kind)
        {
            if (!activeCells.TryGetValue(kind, out List<Vector2Int> cells))
            {
                cells = new List<Vector2Int>();
                activeCells[kind] = cells;
            }

            return cells;
        }

        private Material GetMaterial(HighlightKind kind)
        {
            if (materials.TryGetValue(kind, out Material material))
                return material;

            material = new Material(Shader.Find("Sprites/Default"));
            material.color = GetColor(kind);
            materials[kind] = material;
            return material;
        }

        private Color GetColor(HighlightKind kind)
        {
            switch (kind)
            {
                case HighlightKind.Template: return templateColor;
                case HighlightKind.Intent: return intentColor;
                case HighlightKind.Path: return pathColor;
                case HighlightKind.Selection: return selectionColor;
                case HighlightKind.Spawn: return spawnColor;
                default: return landingColor;
            }
        }

        private GameObject GetPooledQuad()
        {
            if (pool.Count > 0)
            {
                GameObject pooled = pool[pool.Count - 1];
                pool.RemoveAt(pool.Count - 1);
                return pooled;
            }

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.transform.SetParent(transform);
            Destroy(quad.GetComponent<MeshCollider>());
            quad.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            quad.transform.localScale = new Vector3(0.95f, 0.95f, 1f);
            return quad;
        }
    }
}
