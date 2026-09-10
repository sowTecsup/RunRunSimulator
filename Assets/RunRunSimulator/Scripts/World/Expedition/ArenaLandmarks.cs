using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MoriMonchiSimulator
{

public class ArenaLandmarks : MonoBehaviour
{
    [Title("Prefabs")]
    [SerializeField] private List<GameObject> boulderPrefabs = new();
    [SerializeField] private List<GameObject> grovePrefabs = new();
    [SerializeField] private List<GameObject> poolPrefabs = new();

    [Title("Cantidad")]
    [SerializeField] private Vector2Int count = new Vector2Int(3, 3);

    [Title("Reparto")]
    [SerializeField, Min(0f)] private float bandInner = 10f;
    [SerializeField, Min(0f)] private float bandOuter = 24f;
    [SerializeField] private bool mirror = true;
    [SerializeField, Min(0f)] private float minSeparation = 11f;
    [SerializeField, Min(0f)] private float minFromSmall = 4f;
    [SerializeField] private Vector2 scale = new Vector2(0.9f, 1.25f);
    [SerializeField, Min(0f)] private float clearMargin = 2.5f;
    [SerializeField, Min(1)] private int attempts = 14;

    private readonly List<Vector4> placedList = new();

    public IReadOnlyList<Vector4> Placed => placedList;

    public int Place(System.Random rng, Transform parent, ArenaShape shape, Vector3 center, float edgeMargin, float centerRadius, IReadOnlyList<Vector3> safePoints, float safeRadius, List<Vector3> placedOut)
    {
        placedList.Clear();

        if (shape == null || parent == null) return 0;
        if (boulderPrefabs.Count == 0 && grovePrefabs.Count == 0 && poolPrefabs.Count == 0) return 0;

        int min = Mathf.Max(0, Mathf.Min(count.x, count.y));
        int max = Mathf.Max(0, Mathf.Max(count.x, count.y));
        int toPlace = rng.Next(min, max + 1);
        int placed = 0;
        var localPlaced = new List<Vector3>();

        var allFamilies = new List<List<GameObject>>();
        if (boulderPrefabs.Count > 0) allFamilies.Add(boulderPrefabs);
        if (grovePrefabs.Count > 0) allFamilies.Add(grovePrefabs);
        if (poolPrefabs.Count > 0) allFamilies.Add(poolPrefabs);
        var familyPool = new List<List<GameObject>>(allFamilies);

        for (int i = 0; i < toPlace; i++)
        {
            if (!TryFindPoint(rng, shape, center, edgeMargin, safePoints, safeRadius, placedOut, localPlaced, out Vector3 point, out Vector3 mirrorPoint)) continue;

            var prefabs = PickFamily(rng, allFamilies, familyPool);
            var prefab = prefabs[rng.Next(prefabs.Count)];
            float yaw = (float)(rng.NextDouble() * 360.0);
            float s = (float)(rng.NextDouble() * (scale.y - scale.x) + scale.x);

            var instance = Instantiate(prefab, point, Quaternion.Euler(0f, yaw, 0f), parent);
            instance.transform.localScale = Vector3.one * s;
            placedList.Add(ToPlacedEntry(instance));

            placedOut.Add(point);
            localPlaced.Add(point);
            placed++;

            if (mirror)
            {
                var mirrorInstance = Instantiate(prefab, mirrorPoint, Quaternion.Euler(0f, yaw + 180f, 0f), parent);
                mirrorInstance.transform.localScale = Vector3.one * s;
                placedList.Add(ToPlacedEntry(mirrorInstance));

                placedOut.Add(mirrorPoint);
                localPlaced.Add(mirrorPoint);
                placed++;
            }
        }

        return placed;
    }

    private bool TryFindPoint(System.Random rng, ArenaShape shape, Vector3 center, float edgeMargin, IReadOnlyList<Vector3> safePoints, float safeRadius, List<Vector3> placedOut, List<Vector3> localPlaced, out Vector3 point, out Vector3 mirrorPoint)
    {
        for (int attempt = 0; attempt < attempts; attempt++)
        {
            if (!shape.TryRandomPoint(rng, edgeMargin, out Vector3 candidate)) continue;
            if (!IsValidCandidate(candidate, shape, center, safePoints, safeRadius, placedOut, localPlaced)) continue;

            if (mirror)
            {
                Vector3 mirrored = MirrorPoint(candidate, center);
                if (!IsValidCandidate(mirrored, shape, center, safePoints, safeRadius, placedOut, localPlaced)) continue;

                point = candidate;
                mirrorPoint = mirrored;
                return true;
            }

            point = candidate;
            mirrorPoint = default;
            return true;
        }

        point = default;
        mirrorPoint = default;
        return false;
    }

    private bool IsValidCandidate(Vector3 candidate, ArenaShape shape, Vector3 center, IReadOnlyList<Vector3> safePoints, float safeRadius, List<Vector3> placedOut, List<Vector3> localPlaced)
    {
        float distance = Planar(candidate, center);
        if (distance < bandInner || distance > bandOuter) return false;
        if (IsNearAnyPoint(candidate, safePoints, safeRadius)) return false;
        if (IsNearAnyPoint(candidate, localPlaced, minSeparation)) return false;
        if (IsNearAnyPoint(candidate, placedOut, minFromSmall)) return false;
        if (!shape.IsClear(candidate, clearMargin)) return false;

        return true;
    }

    private static Vector3 MirrorPoint(Vector3 p, Vector3 center) =>
        center + new Vector3(-(p.x - center.x), p.y - center.y, -(p.z - center.z));

    private static Vector4 ToPlacedEntry(GameObject instance)
    {
        var renderers = instance.GetComponentsInChildren<Renderer>();
        float radius = 2f;

        if (renderers.Length > 0)
        {
            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            radius = Mathf.Max(bounds.size.x, bounds.size.z) * 0.5f;
        }

        Vector3 position = instance.transform.position;
        return new Vector4(position.x, position.y, position.z, radius);
    }

    private static List<GameObject> PickFamily(System.Random rng, List<List<GameObject>> allFamilies, List<List<GameObject>> pool)
    {
        if (pool.Count == 0) pool.AddRange(allFamilies);

        int index = rng.Next(pool.Count);
        var chosen = pool[index];
        pool.RemoveAt(index);
        return chosen;
    }

    private static float Planar(Vector3 a, Vector3 b) =>
        Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z));

    private static bool IsNearAnyPoint(Vector3 point, IReadOnlyList<Vector3> points, float radius)
    {
        for (int i = 0; i < points.Count; i++)
            if (Planar(point, points[i]) < radius) return true;

        return false;
    }
}
}
