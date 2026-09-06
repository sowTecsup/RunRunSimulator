using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

public class ArenaLayoutBuilder : MonoBehaviour
{
    [Serializable]
    public struct VeinSpot
    {
        public Vector3 Position;
        public int Capacity;
    }

    [Required, SerializeField] private NavMeshSurface surface;
    [SerializeField] private GameObject staticObstacles;
    [SerializeField] private List<GameObject> treePrefabs = new();
    [SerializeField] private List<GameObject> rockPrefabs = new();
    [SerializeField, Min(0)] private int trees = 6;
    [SerializeField, Min(0)] private int rocks = 4;
    [SerializeField, Min(0)] private int veins = 4;
    [SerializeField] private bool mirror = true;
    [SerializeField, Min(1f)] private float arenaHalfSize = 20f;
    [SerializeField, Min(0f)] private float edgeMargin = 2.5f;
    [SerializeField, Min(0f)] private float clearCenterRadius = 6f;
    [SerializeField, Min(0f)] private float clearCornerRadius = 6f;
    [SerializeField, Min(0.5f)] private float obstacleSpacing = 3.5f;
    [SerializeField] private Vector2 treeScale = new Vector2(0.8f, 1.3f);
    [SerializeField] private Vector2 rockScale = new Vector2(0.6f, 1.2f);
    [SerializeField, Min(0f)] private float veinMinFromCenter = 7f;
    [SerializeField, Min(0f)] private float veinSpacing = 8f;
    [SerializeField, Min(0f)] private float veinFromObstacle = 2.5f;
    [SerializeField] private Vector2Int veinCapacity = new Vector2Int(4, 8);

    private readonly List<VeinSpot> veins_ = new();
    private readonly List<Vector3> obstaclePositions = new();
    private GameObject generatedRoot;

    public IReadOnlyList<VeinSpot> Veins => veins_;
    public int BuiltSeed { get; private set; }

    public void Build(int seed, NavMeshQueryFilter filter)
    {
        Clear();

        if (staticObstacles != null) staticObstacles.SetActive(false);

        generatedRoot = new GameObject("GeneratedLayout");
        generatedRoot.transform.SetParent(transform, false);

        var rng = new System.Random(seed);
        Vector3 center = transform.position;

        BuildObstacles(rng, center);

        surface.BuildNavMesh();

        BuildVeins(rng, filter, center);

        BuiltSeed = seed;
        Debug.Log($"[ArenaLayoutBuilder] seed={seed} obstáculos={obstaclePositions.Count} vetas={veins_.Count} mirror={mirror}");
    }

    public void Clear()
    {
        if (generatedRoot != null)
        {
            if (Application.isPlaying) Destroy(generatedRoot);
            else DestroyImmediate(generatedRoot);
            generatedRoot = null;
        }

        obstaclePositions.Clear();
        veins_.Clear();
    }

    private void BuildObstacles(System.Random rng, Vector3 center)
    {
        BuildObstacleSet(rng, center, treePrefabs, trees, treeScale);
        BuildObstacleSet(rng, center, rockPrefabs, rocks, rockScale);
    }

    private void BuildObstacleSet(System.Random rng, Vector3 center, List<GameObject> prefabs, int count, Vector2 scaleRange)
    {
        if (prefabs == null || prefabs.Count == 0 || count <= 0) return;

        int toPlace = mirror ? Mathf.CeilToInt(count / 2f) : count;

        for (int i = 0; i < toPlace; i++)
        {
            if (!TryFindObstaclePoint(rng, center, out Vector3 point)) continue;

            var prefab = prefabs[rng.Next(prefabs.Count)];
            float scale = (float)(rng.NextDouble() * (scaleRange.y - scaleRange.x) + scaleRange.x);
            float yaw = (float)(rng.NextDouble() * 360.0);

            SpawnObstacle(prefab, point, yaw, scale);

            if (mirror)
            {
                Vector3 mirrorLocal = point - center;
                Vector3 mirrorPoint = center + new Vector3(-mirrorLocal.x, mirrorLocal.y, -mirrorLocal.z);
                SpawnObstacle(prefab, mirrorPoint, yaw + 180f, scale);
            }
        }
    }

    private void SpawnObstacle(GameObject prefab, Vector3 position, float yaw, float scale)
    {
        var instance = Instantiate(prefab, position, Quaternion.Euler(0f, yaw, 0f), generatedRoot.transform);
        instance.transform.localScale = Vector3.one * scale;
        obstaclePositions.Add(position);
    }

    private bool TryFindObstaclePoint(System.Random rng, Vector3 center, out Vector3 point)
    {
        for (int attempt = 0; attempt < 40; attempt++)
        {
            Vector3 candidate = RandomPointInSquare(rng, center);

            if (Vector3.Distance(candidate, center) < clearCenterRadius) continue;
            if (IsNearAnyCorner(candidate, center, clearCornerRadius)) continue;
            if (IsNearAnyObstacle(candidate, obstacleSpacing)) continue;

            point = candidate;
            return true;
        }

        point = default;
        return false;
    }

    private void BuildVeins(System.Random rng, NavMeshQueryFilter filter, Vector3 center)
    {
        if (veins <= 0) return;

        int toPlace = mirror ? Mathf.CeilToInt(veins / 2f) : veins;
        var placed = new List<Vector3>();

        for (int i = 0; i < toPlace; i++)
        {
            if (!TryFindVeinPoint(rng, center, placed, out Vector3 point)) continue;

            int capacity = rng.Next(veinCapacity.x, veinCapacity.y + 1);
            AddVeinIfOnNavMesh(point, capacity, filter, placed);

            if (mirror)
            {
                Vector3 mirrorLocal = point - center;
                Vector3 mirrorPoint = center + new Vector3(-mirrorLocal.x, mirrorLocal.y, -mirrorLocal.z);
                AddVeinIfOnNavMesh(mirrorPoint, capacity, filter, placed);
            }
        }
    }

    private void AddVeinIfOnNavMesh(Vector3 point, int capacity, NavMeshQueryFilter filter, List<Vector3> placed)
    {
        if (!NavMesh.SamplePosition(point, out var hit, 3f, filter)) return;

        veins_.Add(new VeinSpot { Position = hit.position, Capacity = capacity });
        placed.Add(point);
    }

    private bool TryFindVeinPoint(System.Random rng, Vector3 center, List<Vector3> placed, out Vector3 point)
    {
        for (int attempt = 0; attempt < 40; attempt++)
        {
            Vector3 candidate = RandomPointInSquare(rng, center);

            if (Vector3.Distance(candidate, center) < veinMinFromCenter) continue;
            if (IsNearAnyCorner(candidate, center, clearCornerRadius)) continue;
            if (IsNearAnyPoint(candidate, placed, veinSpacing)) continue;
            if (IsNearAnyObstacle(candidate, veinFromObstacle)) continue;

            point = candidate;
            return true;
        }

        point = default;
        return false;
    }

    private Vector3 RandomPointInSquare(System.Random rng, Vector3 center)
    {
        float min = -arenaHalfSize + edgeMargin;
        float max = arenaHalfSize - edgeMargin;
        float x = (float)(rng.NextDouble() * (max - min) + min);
        float z = (float)(rng.NextDouble() * (max - min) + min);
        return center + new Vector3(x, 0f, z);
    }

    private bool IsNearAnyCorner(Vector3 point, Vector3 center, float radius)
    {
        float offset = arenaHalfSize - 6f;
        Vector3[] corners =
        {
            center + new Vector3(-offset, 0f, -offset),
            center + new Vector3(-offset, 0f, offset),
            center + new Vector3(offset, 0f, -offset),
            center + new Vector3(offset, 0f, offset),
        };

        foreach (var corner in corners)
            if (Vector3.Distance(point, corner) < radius) return true;

        return false;
    }

    private bool IsNearAnyObstacle(Vector3 point, float radius)
    {
        return IsNearAnyPoint(point, obstaclePositions, radius);
    }

    private bool IsNearAnyPoint(Vector3 point, List<Vector3> points, float radius)
    {
        foreach (var other in points)
        {
            Vector2 a = new Vector2(point.x, point.z);
            Vector2 b = new Vector2(other.x, other.z);
            if (Vector2.Distance(a, b) < radius) return true;
        }

        return false;
    }
}
}
