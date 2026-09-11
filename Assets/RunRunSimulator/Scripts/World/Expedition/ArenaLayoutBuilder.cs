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
    [SerializeField] private ArenaLandmarks landmarks;
    [SerializeField] private GameObject staticObstacles;
    [SerializeField] private List<GameObject> staticDecor = new();
    [SerializeField] private List<GameObject> treePrefabs = new();
    [SerializeField] private List<GameObject> rockPrefabs = new();
    [SerializeField] private List<GameObject> decorPrefabs = new();

    [Title("Densidad por semilla")]
    [SerializeField] private Vector2Int treeCount = new Vector2Int(4, 9);
    [SerializeField] private Vector2Int rockCount = new Vector2Int(2, 6);
    [SerializeField] private Vector2Int veinPairs = new Vector2Int(1, 3);
    [SerializeField] private Vector2Int decorClusters = new Vector2Int(6, 12);
    [SerializeField] private Vector2Int decorPerCluster = new Vector2Int(3, 7);
    [SerializeField, Min(0.5f)] private float decorClusterRadius = 2.2f;
    [SerializeField] private bool mirror = true;

    [Title("Forma de la sala")]
    [SerializeField] private List<ArenaShape> shapes = new();
    [SerializeField] private int shapeIndex = -1;
    [SerializeField] private GameObject legacySquare;

    [Title("Geometría")]
    [SerializeField, Min(1f)] private float arenaHalfSize = 20f;
    [SerializeField, Min(0f)] private float edgeMargin = 2.5f;
    [SerializeField, Min(0f)] private float clearCenterRadius = 6f;
    [SerializeField, Min(0f)] private float clearEntryRadius = 5f;
    [SerializeField, Min(0f)] private float spawnDistance = 8.5f;
    [SerializeField, Range(0.1f, 1f)] private float spawnReach = 0.72f;
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
    private ArenaShape activeShape;
    private int entryPair;
    private bool mirrorActive;

    public IReadOnlyList<VeinSpot> Veins => veins_;
    public int ObstacleCount => obstaclePositions.Count;
    public IReadOnlyList<Vector4> PlacedObstacles => landmarks != null ? landmarks.Placed : null;
    public bool IsBuilt => generatedRoot != null;
    public ArenaShape ActiveShape => activeShape;
    public Vector3 Center => activeShape != null ? activeShape.Center : transform.position;
    public string ShapeName => activeShape != null ? activeShape.DisplayName : "cuadrado";

    public Vector3 EntryDirection => activeShape != null
        ? DirectionToAnchor(activeShape.EntryPoint(entryPair, ExpeditionTeam.Rival))
        : EntryAxes[entryAxis];

    public string EntryName => activeShape != null ? activeShape.EntryName(entryPair) : EntryNames[entryAxis];
    private float EntryScale => 1f / Mathf.Max(Mathf.Abs(EntryDirection.x), Mathf.Abs(EntryDirection.z));

    private Vector3 DirectionToAnchor(Vector3 anchor)
    {
        Vector3 dir = anchor - Center;
        dir.y = 0f;
        return dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.forward;
    }

    public Vector3 EntryPoint(ExpeditionTeam team, float insetFromBorder)
    {
        if (activeShape != null)
        {
            Vector3 anchor = activeShape.EntryPoint(entryPair, team);
            return anchor - DirectionToAnchor(anchor) * insetFromBorder;
        }

        float sign = team == ExpeditionTeam.Rival ? 1f : -1f;
        return transform.position + EntryDirection * (sign * (arenaHalfSize - insetFromBorder) * EntryScale);
    }

    public Vector3 ExitPoint(ExpeditionTeam team) => SpawnPoint(team);

    public Vector3 SpawnPoint(ExpeditionTeam team)
    {
        if (activeShape != null)
        {
            Vector3 anchor = activeShape.EntryPoint(entryPair, team);
            return Center + (anchor - Center) * spawnReach;
        }

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

        activeShape = shapes.Count > 0 ? shapes[(shapeIndex >= 0 ? shapeIndex : Math.Abs(seed)) % shapes.Count] : null;
        foreach (var s in shapes)
            if (s != null) s.gameObject.SetActive(s == activeShape);
        if (legacySquare != null) legacySquare.SetActive(activeShape == null);

        if (activeShape != null)
        {
            var brush = activeShape.GetComponent<ArenaShapeBrush>();
            if (brush != null && brush.ProceduralBySeed) brush.Regenerate(seed);
        }

        mirrorActive = mirror && (activeShape == null || activeShape.Symmetric);

        Vector3 center = Center;

        if (activeShape != null && activeShape.EntryPairCount > 0)
            entryPair = rng.Next(activeShape.EntryPairCount);
        else
            entryAxis = rng.Next(EntryAxes.Length);

        int trees = RangeDraw(rng, treeCount);
        int rocks = RangeDraw(rng, rockCount);
        int pairs = RangeDraw(rng, veinPairs);
        int clusters = RangeDraw(rng, decorClusters);

        BuildObstacleSet(rng, center, treePrefabs, trees, treeScale);
        BuildObstacleSet(rng, center, rockPrefabs, rocks, rockScale);
        BuildDecor(rng, center, clusters);

        int landmarksPlaced = 0;
        if (landmarks != null)
        {
            var safePoints = new List<Vector3>
            {
                SpawnPoint(ExpeditionTeam.Player), SpawnPoint(ExpeditionTeam.Rival),
                ExitPoint(ExpeditionTeam.Player), ExitPoint(ExpeditionTeam.Rival),
            };
            landmarksPlaced = landmarks.Place(rng, generatedRoot.transform, activeShape, center, edgeMargin, clearCenterRadius, safePoints, clearEntryRadius, obstaclePositions);
        }

        surface.BuildNavMesh();

        string veinPattern = BuildVeins(rng, filter, center, pairs);

        Debug.Log($"[ArenaLayoutBuilder] seed={seed} entrada={EntryName} obstáculos={obstaclePositions.Count} decorado={decorCenters.Count} vetas={veins_.Count} grandes={landmarksPlaced} mirror={mirrorActive} forma={ShapeName} cristales={veinPattern}");
    }

    public void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name == "GeneratedLayout") DestroyImmediate(child.gameObject);
        }
        generatedRoot = null;

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

        int toPlace = mirrorActive ? Mathf.CeilToInt(count / 2f) : count;

        for (int i = 0; i < toPlace; i++)
        {
            if (!TryFindObstaclePoint(rng, center, out Vector3 point)) continue;

            var prefab = prefabs[rng.Next(prefabs.Count)];
            float scale = Lerp(rng, scaleRange);
            float yaw = (float)(rng.NextDouble() * 360.0);

            SpawnObstacle(prefab, point, yaw, scale);

            if (mirrorActive)
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

        int toPlace = mirrorActive ? Mathf.CeilToInt(clusters / 2f) : clusters;

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
                if (mirrorActive) SpawnDecor(prefab, Mirror(clusterCenter + offset, center), yaw + 180f, scale);
            }

            decorCenters.Add(clusterCenter);
            if (mirrorActive) decorCenters.Add(Mirror(clusterCenter, center));
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
            if (!TryRandomPoint(rng, center, out Vector3 candidate)) continue;

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
            if (!TryRandomPoint(rng, center, out Vector3 candidate)) continue;

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

    private string BuildVeins(System.Random rng, NavMeshQueryFilter filter, Vector3 center, int pairs)
    {
        var pattern = ArenaVeinLayouts.Pick(rng);
        if (pairs <= 0) return ArenaVeinLayouts.Name(pattern);

        float innerRadius = veinMinFromCenter;
        float outerRadius = ShapeOuterRadius();
        Vector2 acrossDirection = Perpendicular(EntryDirection);
        Vector2 centerXZ = new Vector2(center.x, center.z);
        var placed = new List<Vector3>();

        if (mirrorActive)
        {
            var candidates = ArenaVeinLayouts.Candidates(pattern, rng, centerXZ, acrossDirection, innerRadius, outerRadius, pairs);
            int placedPairs = 0;

            for (int i = 0; i < pairs; i++)
            {
                int capacity = rng.Next(veinCapacity.x, veinCapacity.y + 1);
                bool hasCandidate = i < candidates.Count;

                if (TryBuildVeinPair(rng, hasCandidate ? candidates[i] : default, hasCandidate, center, placed, filter, capacity))
                    placedPairs++;
            }

            if (placedPairs < pairs)
                Debug.Log($"[ArenaLayoutBuilder] pidió {pairs} pares de vetas, entraron {placedPairs}");
        }
        else
        {
            int toPlace = pairs * 2;
            var candidates = ArenaVeinLayouts.Candidates(pattern, rng, centerXZ, acrossDirection, innerRadius, outerRadius, toPlace);
            int placedCount = 0;

            for (int i = 0; i < toPlace; i++)
            {
                int capacity = rng.Next(veinCapacity.x, veinCapacity.y + 1);
                bool hasCandidate = i < candidates.Count;

                if (TryBuildSingleVein(rng, hasCandidate ? candidates[i] : default, hasCandidate, center, placed, filter, capacity))
                    placedCount++;
            }

            if (placedCount % 2 != 0)
            {
                veins_.RemoveAt(veins_.Count - 1);
                placed.RemoveAt(placed.Count - 1);
                placedCount--;
            }

            if (placedCount < toPlace)
                Debug.Log($"[ArenaLayoutBuilder] pidió {pairs} pares de vetas, entraron {placedCount / 2}");
        }

        return ArenaVeinLayouts.Name(pattern);
    }

    private float ShapeOuterRadius()
    {
        if (activeShape == null) return arenaHalfSize - edgeMargin;

        var bounds = activeShape.Bounds;
        return Mathf.Min(bounds.extents.x, bounds.extents.z) - edgeMargin;
    }

    private static Vector2 Perpendicular(Vector3 direction) => new Vector2(-direction.z, direction.x);

    private bool TryPlaceVeinCandidate(Vector2 candidate, Vector3 center, List<Vector3> placed, out Vector3 point)
    {
        Vector2 offset = candidate - new Vector2(center.x, center.z);

        for (int attempt = 0; attempt < 4; attempt++)
        {
            Vector3 candidatePos = center + new Vector3(offset.x, 0f, offset.y);

            if (IsValidVeinPoint(candidatePos, center, placed))
            {
                point = candidatePos;
                return true;
            }

            offset *= attempt % 2 == 0 ? 0.85f : 1.2f;
        }

        point = default;
        return false;
    }

    private bool IsValidVeinPoint(Vector3 candidate, Vector3 center, List<Vector3> placed)
    {
        if (activeShape != null && !activeShape.IsClear(candidate, edgeMargin)) return false;
        if (Vector3.Distance(candidate, center) < veinMinFromCenter) return false;
        if (IsNearEntries(candidate, clearEntryRadius)) return false;
        if (IsNearAnyPoint(candidate, placed, veinSpacing)) return false;
        if (IsNearAnyPoint(candidate, obstaclePositions, veinFromObstacle)) return false;
        if (IsNearAnyLandmark(candidate)) return false;

        return true;
    }

    private bool IsNearAnyLandmark(Vector3 point)
    {
        if (landmarks == null || landmarks.Placed == null) return false;

        foreach (var mark in landmarks.Placed)
            if (Planar(point, new Vector3(mark.x, mark.y, mark.z)) < mark.w + veinFromObstacle) return true;

        return false;
    }

    private bool TryBuildVeinPair(System.Random rng, Vector2 candidate, bool hasCandidate, Vector3 center, List<Vector3> placed, NavMeshQueryFilter filter, int capacity)
    {
        for (int attempt = 0; attempt < 6; attempt++)
        {
            Vector3 point;
            bool found = hasCandidate && attempt == 0
                ? TryPlaceVeinCandidate(candidate, center, placed, out point)
                : TryFindVeinPoint(rng, center, placed, out point);

            if (!found) continue;

            Vector3 mirrorPoint = Mirror(point, center);
            if (!IsValidVeinPoint(mirrorPoint, center, placed)) continue;

            if (!NavMesh.SamplePosition(point, out var hit, 1.5f, filter)) continue;
            if (!NavMesh.SamplePosition(mirrorPoint, out var mirrorHit, 1.5f, filter)) continue;

            if (!IsValidVeinPoint(hit.position, center, placed)) continue;
            if (!IsValidVeinPoint(mirrorHit.position, center, placed)) continue;

            veins_.Add(new VeinSpot { Position = hit.position, Capacity = capacity });
            veins_.Add(new VeinSpot { Position = mirrorHit.position, Capacity = capacity });
            placed.Add(point);
            placed.Add(mirrorPoint);
            return true;
        }

        return false;
    }

    private bool TryBuildSingleVein(System.Random rng, Vector2 candidate, bool hasCandidate, Vector3 center, List<Vector3> placed, NavMeshQueryFilter filter, int capacity)
    {
        for (int attempt = 0; attempt < 6; attempt++)
        {
            Vector3 point;
            bool found = hasCandidate && attempt == 0
                ? TryPlaceVeinCandidate(candidate, center, placed, out point)
                : TryFindVeinPoint(rng, center, placed, out point);

            if (!found) continue;
            if (!NavMesh.SamplePosition(point, out var hit, 1.5f, filter)) continue;
            if (!IsValidVeinPoint(hit.position, center, placed)) continue;

            veins_.Add(new VeinSpot { Position = hit.position, Capacity = capacity });
            placed.Add(point);
            return true;
        }

        return false;
    }

    private bool TryFindVeinPoint(System.Random rng, Vector3 center, List<Vector3> placed, out Vector3 point)
    {
        for (int attempt = 0; attempt < 40; attempt++)
        {
            if (!TryRandomPoint(rng, center, out Vector3 candidate)) continue;

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

    private bool TryRandomPoint(System.Random rng, Vector3 center, out Vector3 point)
    {
        if (activeShape != null)
            return activeShape.TryRandomPoint(rng, edgeMargin, out point);

        float min = -arenaHalfSize + edgeMargin;
        float max = arenaHalfSize - edgeMargin;
        float x = (float)(rng.NextDouble() * (max - min) + min);
        float z = (float)(rng.NextDouble() * (max - min) + min);
        point = center + new Vector3(x, 0f, z);
        return true;
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
            || Planar(point, SpawnPoint(ExpeditionTeam.Rival)) < radius;
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
