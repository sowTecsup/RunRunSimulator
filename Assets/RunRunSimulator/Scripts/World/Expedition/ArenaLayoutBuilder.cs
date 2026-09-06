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

    private static readonly Vector3[] EntryAxes =
    {
        new Vector3(1f, 0f, 1f).normalized,
        new Vector3(-1f, 0f, 1f).normalized,
        Vector3.forward,
        Vector3.right,
    };

    private static readonly string[] EntryNames = { "diagonal", "diagonal inversa", "norte-sur", "este-oeste" };

    [Required, SerializeField] private NavMeshSurface surface;
    [SerializeField] private GameObject staticObstacles;
    [SerializeField] private List<GameObject> staticDecor = new();
    [SerializeField] private List<GameObject> treePrefabs = new();
    [SerializeField] private List<GameObject> rockPrefabs = new();
    [SerializeField] private List<GameObject> decorPrefabs = new();

    [Title("Densidad por semilla")]
    [SerializeField] private Vector2Int treeCount = new Vector2Int(4, 9);
    [SerializeField] private Vector2Int rockCount = new Vector2Int(2, 6);
    [SerializeField] private Vector2Int veinCount = new Vector2Int(2, 5);
    [SerializeField] private Vector2Int decorClusters = new Vector2Int(6, 12);
    [SerializeField] private Vector2Int decorPerCluster = new Vector2Int(3, 7);
    [SerializeField, Min(0.5f)] private float decorClusterRadius = 2.2f;
    [SerializeField] private bool mirror = true;

    [Title("Geometría")]
    [SerializeField, Min(1f)] private float arenaHalfSize = 20f;
    [SerializeField, Min(0f)] private float edgeMargin = 2.5f;
    [SerializeField, Min(0f)] private float clearCenterRadius = 6f;
    [SerializeField, Min(0f)] private float clearEntryRadius = 5f;
    [SerializeField, Min(0f)] private float spawnDistance = 8.5f;
    [SerializeField, Min(0f)] private float exitInset = 4f;
    [SerializeField, Min(0.5f)] private float obstacleSpacing = 3.5f;
    [SerializeField] private Vector2 treeScale = new Vector2(0.8f, 1.3f);
    [SerializeField] private Vector2 rockScale = new Vector2(0.6f, 1.2f);
    [SerializeField] private Vector2 decorScale = new Vector2(0.8f, 1.25f);

    [Title("Vetas")]
    [SerializeField, Min(0f)] private float veinMinFromCenter = 7f;
    [SerializeField, Min(0f)] private float veinSpacing = 8f;
    [SerializeField, Min(0f)] private float veinFromObstacle = 2.5f;
    [SerializeField] private Vector2Int veinCapacity = new Vector2Int(4, 8);

    private readonly List<VeinSpot> veins_ = new();
    private readonly List<Vector3> obstaclePositions = new();
    private readonly List<Vector3> decorCenters = new();
    private GameObject generatedRoot;
    private int entryAxis;

    public IReadOnlyList<VeinSpot> Veins => veins_;
    public bool IsBuilt => generatedRoot != null;
    public Vector3 EntryDirection => EntryAxes[entryAxis];
    public string EntryName => EntryNames[entryAxis];
    private float EntryScale => 1f / Mathf.Max(Mathf.Abs(EntryDirection.x), Mathf.Abs(EntryDirection.z));

    public Vector3 EntryPoint(ExpeditionTeam team, float insetFromBorder)
    {
        float sign = team == ExpeditionTeam.Rival ? 1f : -1f;
        return transform.position + EntryDirection * (sign * (arenaHalfSize - insetFromBorder) * EntryScale);
    }

    public Vector3 ExitPoint(ExpeditionTeam team) => EntryPoint(team, exitInset);

    public Vector3 SpawnPoint(ExpeditionTeam team)
    {
        float sign = team == ExpeditionTeam.Rival ? 1f : -1f;
        return transform.position + EntryDirection * (sign * spawnDistance);
    }

    public void Build(int seed, NavMeshQueryFilter filter)
    {
        Clear();

        if (staticObstacles != null) staticObstacles.SetActive(false);
        foreach (var decor in staticDecor)
            if (decor != null) decor.SetActive(false);

        generatedRoot = new GameObject("GeneratedLayout");
        generatedRoot.transform.SetParent(transform, false);

        var rng = new System.Random(seed);
        Vector3 center = transform.position;

        entryAxis = rng.Next(EntryAxes.Length);
        int trees = RangeDraw(rng, treeCount);
        int rocks = RangeDraw(rng, rockCount);
        int veins = RangeDraw(rng, veinCount);
        int clusters = RangeDraw(rng, decorClusters);

        BuildObstacleSet(rng, center, treePrefabs, trees, treeScale);
        BuildObstacleSet(rng, center, rockPrefabs, rocks, rockScale);
        BuildDecor(rng, center, clusters);

        surface.BuildNavMesh();

        BuildVeins(rng, filter, center, veins);

        Debug.Log($"[ArenaLayoutBuilder] seed={seed} entrada={EntryName} obstáculos={obstaclePositions.Count} decorado={decorCenters.Count} vetas={veins_.Count} mirror={mirror}");
    }

    public void Clear()
    {
        if (generatedRoot != null)
        {
            DestroyImmediate(generatedRoot);
            generatedRoot = null;
        }

        obstaclePositions.Clear();
        decorCenters.Clear();
        veins_.Clear();
    }

    private static int RangeDraw(System.Random rng, Vector2Int range)
    {
        int min = Mathf.Max(0, Mathf.Min(range.x, range.y));
        int max = Mathf.Max(range.x, range.y);
        return rng.Next(min, max + 1);
    }

    private void BuildObstacleSet(System.Random rng, Vector3 center, List<GameObject> prefabs, int count, Vector2 scaleRange)
    {
        if (prefabs == null || prefabs.Count == 0 || count <= 0) return;

        int toPlace = mirror ? Mathf.CeilToInt(count / 2f) : count;

        for (int i = 0; i < toPlace; i++)
        {
            if (!TryFindObstaclePoint(rng, center, out Vector3 point)) continue;

            var prefab = prefabs[rng.Next(prefabs.Count)];
            float scale = Lerp(rng, scaleRange);
            float yaw = (float)(rng.NextDouble() * 360.0);

            SpawnObstacle(prefab, point, yaw, scale);

            if (mirror)
                SpawnObstacle(prefab, Mirror(point, center), yaw + 180f, scale);
        }
    }

    private void SpawnObstacle(GameObject prefab, Vector3 position, float yaw, float scale)
    {
        var instance = Instantiate(prefab, position, Quaternion.Euler(0f, yaw, 0f), generatedRoot.transform);
        instance.transform.localScale = Vector3.one * scale;
        obstaclePositions.Add(position);
    }

    private void BuildDecor(System.Random rng, Vector3 center, int clusters)
    {
        if (decorPrefabs == null || decorPrefabs.Count == 0 || clusters <= 0) return;

        int toPlace = mirror ? Mathf.CeilToInt(clusters / 2f) : clusters;

        for (int i = 0; i < toPlace; i++)
        {
            if (!TryFindDecorCenter(rng, center, out Vector3 clusterCenter)) continue;

            int items = RangeDraw(rng, decorPerCluster);
            for (int k = 0; k < items; k++)
            {
                var prefab = decorPrefabs[rng.Next(decorPrefabs.Count)];
                float angle = (float)(rng.NextDouble() * Mathf.PI * 2f);
                float dist = (float)Math.Sqrt(rng.NextDouble()) * decorClusterRadius;
                Vector3 offset = new Vector3(Mathf.Cos(angle) * dist, 0f, Mathf.Sin(angle) * dist);
                float yaw = (float)(rng.NextDouble() * 360.0);
                float scale = Lerp(rng, decorScale);

                SpawnDecor(prefab, clusterCenter + offset, yaw, scale);
                if (mirror) SpawnDecor(prefab, Mirror(clusterCenter + offset, center), yaw + 180f, scale);
            }

            decorCenters.Add(clusterCenter);
            if (mirror) decorCenters.Add(Mirror(clusterCenter, center));
        }
    }

    private void SpawnDecor(GameObject prefab, Vector3 position, float yaw, float scale)
    {
        var instance = Instantiate(prefab, position, Quaternion.Euler(0f, yaw, 0f), generatedRoot.transform);
        instance.transform.localScale = Vector3.one * scale;
        foreach (var collider in instance.GetComponentsInChildren<Collider>(true))
            DestroyImmediate(collider);
    }

    private bool TryFindObstaclePoint(System.Random rng, Vector3 center, out Vector3 point)
    {
        for (int attempt = 0; attempt < 40; attempt++)
        {
            Vector3 candidate = RandomPointInSquare(rng, center);

            if (Vector3.Distance(candidate, center) < clearCenterRadius) continue;
            if (IsNearEntries(candidate, clearEntryRadius)) continue;
            if (IsNearAnyPoint(candidate, obstaclePositions, obstacleSpacing)) continue;

            point = candidate;
            return true;
        }

        point = default;
        return false;
    }

    private bool TryFindDecorCenter(System.Random rng, Vector3 center, out Vector3 point)
    {
        for (int attempt = 0; attempt < 40; attempt++)
        {
            Vector3 candidate = RandomPointInSquare(rng, center);

            if (Vector3.Distance(candidate, center) < clearCenterRadius * 0.6f) continue;
            if (IsNearEntries(candidate, clearEntryRadius * 0.6f)) continue;
            if (IsNearAnyPoint(candidate, obstaclePositions, 1.5f)) continue;
            if (IsNearAnyPoint(candidate, decorCenters, decorClusterRadius * 1.5f)) continue;

            point = candidate;
            return true;
        }

        point = default;
        return false;
    }

    private void BuildVeins(System.Random rng, NavMeshQueryFilter filter, Vector3 center, int count)
    {
        if (count <= 0) return;

        int toPlace = mirror ? Mathf.CeilToInt(count / 2f) : count;
        var placed = new List<Vector3>();

        for (int i = 0; i < toPlace; i++)
        {
            if (!TryFindVeinPoint(rng, center, placed, out Vector3 point)) continue;

            int capacity = rng.Next(veinCapacity.x, veinCapacity.y + 1);
            AddVeinIfOnNavMesh(point, capacity, filter, placed);

            if (mirror)
                AddVeinIfOnNavMesh(Mirror(point, center), capacity, filter, placed);
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
            if (IsNearEntries(candidate, clearEntryRadius)) continue;
            if (IsNearAnyPoint(candidate, placed, veinSpacing)) continue;
            if (IsNearAnyPoint(candidate, obstaclePositions, veinFromObstacle)) continue;

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

    private static float Lerp(System.Random rng, Vector2 range) =>
        (float)(rng.NextDouble() * (range.y - range.x) + range.x);

    private static Vector3 Mirror(Vector3 point, Vector3 center)
    {
        Vector3 local = point - center;
        return center + new Vector3(-local.x, local.y, -local.z);
    }

    private bool IsNearEntries(Vector3 point, float radius)
    {
        return Planar(point, SpawnPoint(ExpeditionTeam.Player)) < radius
            || Planar(point, SpawnPoint(ExpeditionTeam.Rival)) < radius
            || Planar(point, ExitPoint(ExpeditionTeam.Player)) < radius
            || Planar(point, ExitPoint(ExpeditionTeam.Rival)) < radius;
    }

    private static float Planar(Vector3 a, Vector3 b) =>
        Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

    private static bool IsNearAnyPoint(Vector3 point, List<Vector3> points, float radius)
    {
        foreach (var other in points)
            if (Planar(point, other) < radius) return true;

        return false;
    }
}
}
