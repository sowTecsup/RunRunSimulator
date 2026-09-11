using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MoriMonchiSimulator
{

[ExecuteAlways]
[RequireComponent(typeof(ArenaShape))]
public class ArenaShapeDressing : MonoBehaviour
{
    [Title("Semilla")]
    [SerializeField] private int seed = 1;

    [Title("Cobertura")]
    [SerializeField] private List<GameObject> coverPrefabs = new();
    [SerializeField, Min(0.5f)] private float coverSpacing = 1.8f;
    [SerializeField, Range(0f, 1f)] private float coverSkip = 0.2f;
    [SerializeField] private Vector2 coverScale = new Vector2(0.8f, 1.3f);

    [Title("Acentos")]
    [SerializeField] private List<GameObject> accentPrefabs = new();
    [SerializeField, Min(0.5f)] private float accentSpacing = 4.5f;
    [SerializeField, Range(0f, 1f)] private float accentSkip = 0.35f;
    [SerializeField] private Vector2 accentScale = new Vector2(0.8f, 1.25f);

    [Title("Borde")]
    [SerializeField] private List<GameObject> borderTreePrefabs = new();
    [SerializeField] private List<GameObject> borderRockPrefabs = new();
    [SerializeField, Min(0.5f)] private float borderStep = 2.6f;
    [SerializeField, Min(0f)] private float borderInset = 1.3f;
    [SerializeField, Range(0f, 1f)] private float borderJitter = 0.4f;
    [SerializeField, Range(0f, 1f)] private float borderRockChance = 0.3f;
    [SerializeField, Min(0f)] private float borderEntryClear = 5f;
    [SerializeField, Min(0f)] private float borderOuterStep = 2.4f;
    [SerializeField, Min(0f)] private float borderOuterOffset = 1.2f;
    [SerializeField, Min(0f)] private float borderOuterSink = 0.4f;
    [SerializeField] private Vector2 treeScale = new Vector2(0.8f, 1.2f);
    [SerializeField] private Vector2 rockScale = new Vector2(0.6f, 1.1f);

    [SerializeField, Min(0f)] private float margin = 0.8f;

    private ArenaShape shape;
    private GameObject dressing;
    private GameObject border;

    private void OnEnable()
    {
        shape = GetComponent<ArenaShape>();
        shape.Rebuilt += Dress;
        Dress();
    }

    private void OnDisable()
    {
        shape.Rebuilt -= Dress;
        DestroyDressing();
    }

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying)
        {
            UnityEditor.EditorApplication.delayCall -= DressIfAlive;
            UnityEditor.EditorApplication.delayCall += DressIfAlive;
        }
#endif
    }

#if UNITY_EDITOR
    private void DressIfAlive()
    {
        if (this != null && isActiveAndEnabled) Dress();
    }
#endif

    private void Dress()
    {
        DestroyDressing();

        if (shape.OutlinePolygon.Count < 3) return;

        dressing = new GameObject("Dressing") { hideFlags = HideFlags.DontSave };
        dressing.transform.SetParent(transform, false);

        border = new GameObject("Border") { hideFlags = HideFlags.DontSave };
        border.transform.SetParent(dressing.transform, false);

        var rng = new System.Random(seed);

        DressCover(rng);
        DressAccents(rng);
        DressBorder(rng);
    }

    private void DressCover(System.Random rng)
    {
        ScatterGrid(rng, coverPrefabs, coverSpacing, coverSkip, coverScale, false);
    }

    private void DressAccents(System.Random rng)
    {
        ScatterGrid(rng, accentPrefabs, accentSpacing, accentSkip, accentScale, false);
    }

    private void ScatterGrid(System.Random rng, List<GameObject> prefabs, float spacing, float skip, Vector2 scaleRange, bool keepColliders)
    {
        if (prefabs.Count == 0) return;

        var bounds = shape.Bounds;

        for (float x = bounds.min.x; x <= bounds.max.x; x += spacing)
        {
            for (float z = bounds.min.z; z <= bounds.max.z; z += spacing)
            {
                float jitterX = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.45f * spacing;
                float jitterZ = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.45f * spacing;
                var world = new Vector3(x + jitterX, shape.Center.y, z + jitterZ);

                if (rng.NextDouble() < skip) continue;
                if (!shape.IsClear(world, margin)) continue;

                var prefab = prefabs[rng.Next(prefabs.Count)];
                float yaw = (float)(rng.NextDouble() * 360.0);
                float scale = (float)(rng.NextDouble() * (scaleRange.y - scaleRange.x) + scaleRange.x);

                Spawn(prefab, world, yaw, scale, keepColliders);
            }
        }
    }

    private void DressBorder(System.Random rng)
    {
        if (borderTreePrefabs.Count == 0 && borderRockPrefabs.Count == 0) return;

        var innerPoints = ArenaShapeScatter.AlongEdge(shape.OutlinePolygon, rng, borderStep, borderInset, borderJitter);
        ScatterBorder(rng, innerPoints, true, 0f);

        if (borderOuterStep > 0f)
        {
            var outerPoints = ArenaShapeScatter.AlongEdge(shape.OutlinePolygon, rng, borderOuterStep, -borderOuterOffset, borderJitter);
            ScatterBorder(rng, outerPoints, false, borderOuterSink);
        }
    }

    private void ScatterBorder(System.Random rng, List<Vector2> points, bool filterClear, float yOffset)
    {
        foreach (var point in points)
        {
            var world = new Vector3(point.x, shape.Center.y, point.y);

            if (filterClear && !shape.IsClear(world, 0.3f)) continue;
            if (NearEntry(world)) continue;

            bool wantsRock = rng.NextDouble() < borderRockChance;

            List<GameObject> prefabs;
            Vector2 scaleRange;

            if (wantsRock && borderRockPrefabs.Count > 0)
            {
                prefabs = borderRockPrefabs;
                scaleRange = rockScale;
            }
            else if (borderTreePrefabs.Count > 0)
            {
                prefabs = borderTreePrefabs;
                scaleRange = treeScale;
            }
            else
            {
                continue;
            }

            var prefab = prefabs[rng.Next(prefabs.Count)];
            float yaw = (float)(rng.NextDouble() * 360.0);
            float scale = (float)(rng.NextDouble() * (scaleRange.y - scaleRange.x) + scaleRange.x);

            Spawn(prefab, world, yaw, scale, true, border.transform, yOffset);
        }
    }

    private bool NearEntry(Vector3 world)
    {
        for (int pair = 0; pair < shape.EntryPairCount; pair++)
        {
            if (PlanarDistance(world, shape.EntryPoint(pair, ExpeditionTeam.Player)) < borderEntryClear) return true;
            if (PlanarDistance(world, shape.EntryPoint(pair, ExpeditionTeam.Rival)) < borderEntryClear) return true;
        }

        return false;
    }

    private static float PlanarDistance(Vector3 a, Vector3 b)
    {
        return Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));
    }

    public void ClearAround(IReadOnlyList<Vector4> zones)
    {
        if (dressing == null || zones == null || zones.Count == 0) return;

        for (int i = dressing.transform.childCount - 1; i >= 0; i--)
        {
            var child = dressing.transform.GetChild(i);
            if (child.gameObject == border) continue;

            for (int z = 0; z < zones.Count; z++)
            {
                var zone = zones[z];
                var center = new Vector3(zone.x, zone.y, zone.z);

                if (PlanarDistance(child.position, center) < zone.w)
                {
                    if (Application.isPlaying) Destroy(child.gameObject);
                    else DestroyImmediate(child.gameObject);
                    break;
                }
            }
        }
    }

    private void Spawn(GameObject prefab, Vector3 position, float yaw, float scale, bool keepColliders, Transform parent = null, float yOffset = 0f)
    {
        position.y -= yOffset;
        var instance = Instantiate(prefab, position, Quaternion.Euler(0f, yaw, 0f), parent != null ? parent : dressing.transform);
        instance.transform.localScale = Vector3.one * scale;
        instance.hideFlags = HideFlags.DontSave;

        if (!keepColliders)
        {
            var colliders = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
                DestroyImmediate(colliders[i]);
        }
    }

    private void DestroyDressing()
    {
        if (dressing != null)
        {
            DestroyImmediate(dressing);
            dressing = null;
            border = null;
        }

        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name == "Dressing") DestroyImmediate(child.gameObject);
        }
    }
}
}
