using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator
{

public static class ArenaShapeScatter
{
    public static List<Vector2> InPolygon(IReadOnlyList<Vector2> polygon, System.Random rng, float spacing, float edgeMargin)
    {
        var result = new List<Vector2>();
        var bounds = ArenaShapeMesher.Bounds(polygon);
        int consecutiveFailures = 0;

        for (int attempt = 0; attempt < 400; attempt++)
        {
            float x = (float)(rng.NextDouble() * bounds.size.x + bounds.min.x);
            float z = (float)(rng.NextDouble() * bounds.size.z + bounds.min.z);
            var candidate = new Vector2(x, z);

            bool ok = ArenaShapeMesher.Contains(polygon, candidate)
                && ArenaShapeMesher.DistanceToEdge(polygon, candidate) >= edgeMargin
                && !TooClose(result, candidate, spacing);

            if (!ok)
            {
                consecutiveFailures++;
                if (consecutiveFailures >= 60) break;
                continue;
            }

            consecutiveFailures = 0;
            result.Add(candidate);
        }

        return result;
    }

    public static List<Vector2> AlongEdge(IReadOnlyList<Vector2> loop, System.Random rng, float step, float inset, float jitter)
    {
        var result = new List<Vector2>();
        int count = loop.Count;
        if (count < 2 || step <= 0f) return result;

        bool counterClockwise = ArenaShapeMesher.SignedArea(loop) > 0f;

        float mark = step * (1f + (float)(rng.NextDouble() * 2.0 - 1.0) * jitter);
        float traveled = 0f;

        for (int i = 0; i < count; i++)
        {
            Vector2 a = loop[i];
            Vector2 b = loop[(i + 1) % count];
            Vector2 edge = b - a;
            float edgeLength = edge.magnitude;
            if (edgeLength <= 0f) continue;

            Vector2 normal = counterClockwise
                ? new Vector2(-edge.y, edge.x) / edgeLength
                : new Vector2(edge.y, -edge.x) / edgeLength;

            while (mark <= traveled + edgeLength)
            {
                float t = (mark - traveled) / edgeLength;
                Vector2 point = Vector2.Lerp(a, b, t);
                result.Add(point + normal * inset);

                mark += step * (1f + (float)(rng.NextDouble() * 2.0 - 1.0) * jitter);
            }

            traveled += edgeLength;
        }

        return result;
    }

    private static bool TooClose(List<Vector2> placed, Vector2 candidate, float spacing)
    {
        foreach (var p in placed)
            if (Vector2.Distance(p, candidate) < spacing) return true;
        return false;
    }
}
}
