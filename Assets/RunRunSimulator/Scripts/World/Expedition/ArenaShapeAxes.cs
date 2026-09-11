using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

public static class ArenaShapeAxes
{
    public struct Axis
    {
        public Vector2 A;
        public Vector2 B;
        public float Length;
    }

    public static List<Axis> Longest(IReadOnlyList<Vector2> polygon, Vector2 center, int max, float minAngleDegrees, float minLengthRatio, float maxMidOffsetRatio)
    {
        var result = new List<Axis>();
        if (polygon == null || polygon.Count < 3) return result;

        var points = polygon;
        if (points.Count > 256)
        {
            var sampled = new List<Vector2>();
            int step = Mathf.CeilToInt(points.Count / 256f);
            for (int i = 0; i < points.Count; i += step)
                sampled.Add(points[i]);
            points = sampled;
        }

        var centered = new List<Axis>();
        var loose = new List<Axis>();

        for (int i = 0; i < points.Count; i++)
        {
            for (int j = i + 1; j < points.Count; j++)
            {
                var a = points[i];
                var b = points[j];
                var axis = new Axis { A = a, B = b, Length = Vector2.Distance(a, b) };

                loose.Add(axis);
                if (Vector2.Distance((a + b) * 0.5f, center) <= axis.Length * maxMidOffsetRatio)
                    centered.Add(axis);
            }
        }

        var candidates = centered.Count > 0 ? centered : loose;
        if (candidates.Count == 0) return result;

        candidates.Sort((x, y) => y.Length.CompareTo(x.Length));

        var mainAxis = candidates[0];
        result.Add(mainAxis);

        var directions = new List<Vector2> { (mainAxis.B - mainAxis.A).normalized };

        for (int i = 1; i < candidates.Count && result.Count < max; i++)
        {
            var candidate = candidates[i];
            if (candidate.Length < minLengthRatio * mainAxis.Length) break;

            var direction = (candidate.B - candidate.A).normalized;
            bool farEnough = true;

            foreach (var existing in directions)
            {
                if (LineAngle(direction, existing) < minAngleDegrees)
                {
                    farEnough = false;
                    break;
                }
            }

            if (!farEnough) continue;

            result.Add(candidate);
            directions.Add(direction);
        }

        return result;
    }

    public static string Name(Vector2 direction)
    {
        var references = new (string name, Vector2 dir)[]
        {
            ("norte-sur", new Vector2(0f, 1f)),
            ("este-oeste", new Vector2(1f, 0f)),
            ("diagonal", new Vector2(1f, 1f).normalized),
            ("diagonal inversa", new Vector2(1f, -1f).normalized),
        };

        string best = references[0].name;
        float bestAngle = float.PositiveInfinity;

        foreach (var reference in references)
        {
            float angle = LineAngle(direction, reference.dir);
            if (angle < bestAngle)
            {
                bestAngle = angle;
                best = reference.name;
            }
        }

        return best;
    }

    private static float LineAngle(Vector2 a, Vector2 b)
    {
        float angle = Vector2.Angle(a, b);
        return angle > 90f ? 180f - angle : angle;
    }
}
}
