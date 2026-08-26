using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator.CombatPrototype
{
    public enum HighlightKind { Template, Intent, Path, Landing }

    public class BoardHighlighter : MonoBehaviour
    {
        [SerializeField] private CombatBoardBuilder builder;
        [SerializeField] private Color templateColor = new Color(0.2f, 0.9f, 1f, 0.45f);
        [SerializeField] private Color intentColor = new Color(1f, 0.25f, 0.2f, 0.45f);
        [SerializeField] private Color pathColor = new Color(1f, 0.9f, 0.2f, 0.45f);
        [SerializeField] private Color landingColor = new Color(0.3f, 1f, 0.35f, 0.45f);

        private readonly List<GameObject> pool = new List<GameObject>();
        private readonly Dictionary<HighlightKind, List<GameObject>> active = new Dictionary<HighlightKind, List<GameObject>>();
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
            Material material = GetMaterial(kind);

            foreach (Vector2Int cell in cells)
            {
                if (!board.InBounds(cell))
                    continue;

                GameObject quad = GetPooledQuad();
                quad.GetComponent<MeshRenderer>().sharedMaterial = material;
                quad.transform.position = board.CellToWorld(cell) + Vector3.up * 0.02f;
                quad.SetActive(true);
                quads.Add(quad);
            }
        }

        public void Clear(HighlightKind kind)
        {
            if (!active.TryGetValue(kind, out List<GameObject> quads))
                return;

            for (int i = 0; i < quads.Count; i++)
            {
                quads[i].SetActive(false);
                pool.Add(quads[i]);
            }

            quads.Clear();
        }

        public void ClearAll()
        {
            foreach (HighlightKind kind in active.Keys)
                Clear(kind);
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
