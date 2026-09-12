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

    [Title("Margenes")]
    [SerializeField] private float edgeMargin = 0.6f;
    [SerializeField] private float obstacleMargin = 0.5f;

    private ArenaShape shape;
    private GameObject grassField;

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

                for (int i = 0; i < candidateCount; i++)
                {
                    float x = (float)(rng.NextDouble() * chunkSize + chunkX);
                    float z = (float)(rng.NextDouble() * chunkSize + chunkZ);
                    var world = new Vector3(x, shape.Center.y, z);

                    if (!bordeSeguro && !shape.Contains(world)) continue;
                    if (!obstaculoLejos && !shape.IsClear(world, obstacleMargin)) continue;
                    if (!bordeSeguro && ArenaShapeMesher.DistanceToEdge(shape.OutlinePolygon, new Vector2(x, z)) <= edgeMargin) continue;

                    roots.Add(world);
                }

                if (roots.Count == 0) continue;

                if (totalBlades + roots.Count > maxBlades)
                {
                    int remaining = maxBlades - totalBlades;
                    if (remaining <= 0) return;
                    roots.RemoveRange(remaining, roots.Count - remaining);
                }

                totalBlades += roots.Count;

                var mesh = ArenaGrassBlades.Build(roots, rng, heightRange, widthRange, tilt, chunkCenter);
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
