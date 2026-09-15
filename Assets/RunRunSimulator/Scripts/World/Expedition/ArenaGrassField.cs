using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MoriMonchiSimulator
{

[ExecuteAlways]
[RequireComponent(typeof(ArenaShape))]
public class ArenaGrassField : MonoBehaviour
{
    [Title("Semilla")]
    [SerializeField] private int seed = 11;

    [Title("Material")]
    [SerializeField] private Material bladeMaterial;

    [Title("Parcelas")]
    [SerializeField, Min(2f)] private float chunkSize = 5f;
    [SerializeField, Min(0.1f)] private float density = 50f;
    [SerializeField] private int maxBlades = 120000;

    [Title("Brizna")]
    [SerializeField] private Vector2 heightRange = new Vector2(0.12f, 0.26f);
    [SerializeField] private Vector2 widthRange = new Vector2(0.045f, 0.075f);
    [SerializeField] private float tilt = 0.25f;

    [Title("Cobertura")]
    [SerializeField] private ArenaGrassCover.Settings cover = ArenaGrassCover.Settings.Default;

    [Title("Margenes")]
    [SerializeField] private float edgeMargin = 0.6f;
    [SerializeField] private float obstacleMargin = 0.5f;

    private ArenaShape shape;
    private GameObject grassField;
    private readonly List<GrassChunk> chunks = new List<GrassChunk>();

    private class GrassChunk
    {
        public GameObject Go;
        public Vector3 Center;
        public List<Vector3> Roots;
        public List<float> Heights;
    }

    private void OnEnable()
    {
        shape = GetComponent<ArenaShape>();
        shape.Rebuilt += Sow;
        Sow();
    }

    private void OnDisable()
    {
        shape.Rebuilt -= Sow;
        DestroyGrassField();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall -= SowIfAlive;
            UnityEditor.EditorApplication.delayCall += SowIfAlive;
        }
#endif
    }

#if UNITY_EDITOR
    private void SowIfAlive()
    {
        if (this != null && isActiveAndEnabled) Sow();
    }
#endif

    private void Sow()
    {
        DestroyGrassField();

        if (shape.OutlinePolygon.Count < 3) return;
        if (bladeMaterial == null) return;

        grassField = new GameObject("GrassField") { hideFlags = HideFlags.DontSave };
        grassField.transform.SetParent(transform, false);

        var rng = new System.Random(seed);
        var bounds = shape.Bounds;
        int totalBlades = 0;
        float circumRadius = chunkSize * 0.7071f;

        var obstacleBounds = new List<Rect>();
        CollectObstacleBounds(shape.RockPolygons, obstacleMargin, obstacleBounds);
        CollectObstacleBounds(shape.LakePolygons, obstacleMargin, obstacleBounds);
        CollectObstacleBounds(shape.PitPolygons, obstacleMargin, obstacleBounds);
        CollectObstacleBounds(shape.GrovePolygons, obstacleMargin, obstacleBounds);

        for (float chunkX = bounds.min.x; chunkX < bounds.max.x; chunkX += chunkSize)
        {
            for (float chunkZ = bounds.min.z; chunkZ < bounds.max.z; chunkZ += chunkSize)
            {
                var chunkCenter = new Vector3(chunkX + chunkSize * 0.5f, shape.Center.y, chunkZ + chunkSize * 0.5f);
                var chunkCenter2D = new Vector2(chunkCenter.x, chunkCenter.z);
                bool centroAdentro = shape.Contains(chunkCenter);
                float distanciaCentro = ArenaShapeMesher.DistanceToEdge(shape.OutlinePolygon, chunkCenter2D);

                if (!centroAdentro && distanciaCentro > circumRadius) continue;

                bool bordeSeguro = centroAdentro && distanciaCentro > edgeMargin + circumRadius;

                var chunkRect = Rect.MinMaxRect(chunkX, chunkZ, chunkX + chunkSize, chunkZ + chunkSize);
                bool obstaculoLejos = true;
                for (int i = 0; i < obstacleBounds.Count; i++)
                {
                    if (chunkRect.Overlaps(obstacleBounds[i]))
                    {
                        obstaculoLejos = false;
                        break;
                    }
                }

                int candidateCount = Mathf.RoundToInt(density * chunkSize * chunkSize);
                var roots = new List<Vector3>(candidateCount);
                var heights = new List<float>(candidateCount);

                for (int i = 0; i < candidateCount; i++)
                {
                    float x = (float)(rng.NextDouble() * chunkSize + chunkX);
                    float z = (float)(rng.NextDouble() * chunkSize + chunkZ);
                    var world = new Vector3(x, shape.Center.y, z);

                    if (!bordeSeguro && !shape.Contains(world)) continue;
                    if (!obstaculoLejos && !shape.IsClear(world, obstacleMargin)) continue;
                    if (!bordeSeguro && ArenaShapeMesher.DistanceToEdge(shape.OutlinePolygon, new Vector2(x, z)) <= edgeMargin) continue;

                    float density = ArenaGrassCover.Sample(new Vector2(x, z), seed, in cover, out float heightScale);
                    if (rng.NextDouble() >= density) continue;
                    roots.Add(world);
                    heights.Add(heightScale);
                }

                if (roots.Count == 0) continue;

                if (totalBlades + roots.Count > maxBlades)
                {
                    int remaining = maxBlades - totalBlades;
                    if (remaining <= 0) return;
                    roots.RemoveRange(remaining, roots.Count - remaining);
                    heights.RemoveRange(remaining, heights.Count - remaining);
                }

                totalBlades += roots.Count;

                var mesh = ArenaGrassBlades.Build(roots, rng, heightRange, widthRange, tilt, chunkCenter, heights);
                if (mesh == null) continue;

                var chunk = new GameObject("GrassChunk") { hideFlags = HideFlags.DontSave };
                chunk.transform.SetParent(grassField.transform, false);
                chunk.transform.position = chunkCenter;

                var filter = chunk.AddComponent<MeshFilter>();
                filter.sharedMesh = mesh;

                var renderer = chunk.AddComponent<MeshRenderer>();
                renderer.sharedMaterial = bladeMaterial;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = true;

                chunks.Add(new GrassChunk { Go = chunk, Center = chunkCenter, Roots = roots, Heights = heights });

                if (totalBlades >= maxBlades) return;
            }
        }
    }

    private void DestroyGrassField()
    {
        if (grassField != null)
        {
            DestroyChunks(grassField.transform);
            DestroyImmediate(grassField);
            grassField = null;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name == "GrassField")
            {
                DestroyChunks(child);
                DestroyImmediate(child.gameObject);
            }
        }

        chunks.Clear();
    }

    public void ClearAround(IReadOnlyList<Vector4> zones)
    {
        if (grassField == null || zones == null || zones.Count == 0) return;

        for (int c = chunks.Count - 1; c >= 0; c--)
        {
            var chunk = chunks[c];

            bool zonaCerca = false;
            for (int z = 0; z < zones.Count; z++)
            {
                var zone = zones[z];
                float alcance = zone.w + chunkSize * 0.7071f;
                if (PlanarDistance(chunk.Center, zone) <= alcance)
                {
                    zonaCerca = true;
                    break;
                }
            }

            if (!zonaCerca) continue;

            var rootsFiltradas = new List<Vector3>(chunk.Roots.Count);
            var heightsFiltradas = new List<float>(chunk.Roots.Count);
            for (int r = 0; r < chunk.Roots.Count; r++)
            {
                var root = chunk.Roots[r];
                bool dentroDeAlguna = false;
                for (int z = 0; z < zones.Count; z++)
                {
                    var zone = zones[z];
                    if (PlanarDistance(root, zone) < zone.w)
                    {
                        dentroDeAlguna = true;
                        break;
                    }
                }

                if (!dentroDeAlguna)
                {
                    rootsFiltradas.Add(root);
                    heightsFiltradas.Add(chunk.Heights[r]);
                }
            }

            if (rootsFiltradas.Count == chunk.Roots.Count) continue;

            var filter = chunk.Go.GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null) DestroyImmediate(filter.sharedMesh);

            if (rootsFiltradas.Count == 0)
            {
                DestroyImmediate(chunk.Go);
                chunks.RemoveAt(c);
                continue;
            }

            var mesh = ArenaGrassBlades.Build(rootsFiltradas, new System.Random(seed + c), heightRange, widthRange, tilt, chunk.Center, heightsFiltradas);
            if (filter != null) filter.sharedMesh = mesh;

            chunk.Roots = rootsFiltradas;
            chunk.Heights = heightsFiltradas;
        }
    }

    private static float PlanarDistance(Vector3 a, Vector4 b)
    {
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }

    private static void CollectObstacleBounds(IReadOnlyList<IReadOnlyList<Vector2>> polygons, float margin, List<Rect> bounds)
    {
        for (int i = 0; i < polygons.Count; i++)
            bounds.Add(PolygonBounds(polygons[i], margin));
    }

    private static Rect PolygonBounds(IReadOnlyList<Vector2> polygon, float margin)
    {
        float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
        for (int i = 0; i < polygon.Count; i++)
        {
            var p = polygon[i];
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minY) minY = p.y;
            if (p.y > maxY) maxY = p.y;
        }

        return Rect.MinMaxRect(minX - margin, minY - margin, maxX + margin, maxY + margin);
    }

    private static void DestroyChunks(Transform field)
    {
        for (int i = 0; i < field.childCount; i++)
        {
            var filter = field.GetChild(i).GetComponent<MeshFilter>();
            if (filter != null && filter.sharedMesh != null) DestroyImmediate(filter.sharedMesh);
        }
    }
}
}
