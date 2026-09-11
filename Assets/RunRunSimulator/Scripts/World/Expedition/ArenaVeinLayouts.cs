using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

public static class ArenaVeinLayouts
{
    public enum Pattern { Racimos, Anillo, Franja, Lobulos }

    public static Pattern Pick(System.Random rng) => (Pattern)rng.Next(4);

    public static string Name(Pattern pattern) => pattern switch
    {
        Pattern.Racimos => "racimos",
        Pattern.Anillo => "anillo",
        Pattern.Franja => "franja",
        Pattern.Lobulos => "lobulos",
        _ => "",
    };

    public static List<Vector2> Candidates(Pattern pattern, System.Random rng, Vector2 center, Vector2 acrossDirection, float innerRadius, float outerRadius, int count)
    {
        if (count <= 0) return new List<Vector2>();

        var raw = pattern switch
        {
            Pattern.Racimos => Racimos(rng, center, innerRadius, outerRadius, count),
            Pattern.Anillo => Anillo(rng, center, innerRadius, outerRadius, count),
            Pattern.Franja => Franja(rng, center, acrossDirection, innerRadius, outerRadius, count),
            _ => Lobulos(rng, center, innerRadius, outerRadius, count),
        };

        var result = new List<Vector2>(raw.Count);
        foreach (var point in raw)
            result.Add(ClampToMinRadius(point, center, innerRadius));

        return result;
    }

    private static List<Vector2> Racimos(System.Random rng, Vector2 center, float innerRadius, float outerRadius, int count)
    {
        float midRadius = Mathf.Lerp(innerRadius, outerRadius, 0.5f);
        float angleA = RandomAngle(rng);
        float angleB = angleA + Mathf.PI + RandomJitter(rng, 40f) * Mathf.Deg2Rad;
        Vector2 clusterA = center + FromPolar(angleA, midRadius);
        Vector2 clusterB = center + FromPolar(angleB, midRadius);

        float spread = Mathf.Max(0.5f, (outerRadius - innerRadius) * 0.25f);
        var result = new List<Vector2>(count);

        for (int i = 0; i < count; i++)
        {
            Vector2 origin = i % 2 == 0 ? clusterA : clusterB;
            float angle = RandomAngle(rng);
            float dist = (float)rng.NextDouble() * spread;
            result.Add(origin + FromPolar(angle, dist));
        }

        return result;
    }

    private static List<Vector2> Anillo(System.Random rng, Vector2 center, float innerRadius, float outerRadius, int count)
    {
        float midRadius = Mathf.Lerp(innerRadius, outerRadius, 0.5f);
        float step = Mathf.PI * 2f / count;
        float radiusJitter = Mathf.Max(0.3f, (outerRadius - innerRadius) * 0.1f);
        var result = new List<Vector2>(count);

        for (int i = 0; i < count; i++)
        {
            float angle = step * i + RandomJitter(rng, 10f) * Mathf.Deg2Rad;
            float radius = midRadius + ((float)rng.NextDouble() * 2f - 1f) * radiusJitter;
            result.Add(center + FromPolar(angle, radius));
        }

        return result;
    }

    private static List<Vector2> Franja(System.Random rng, Vector2 center, Vector2 acrossDirection, float innerRadius, float outerRadius, int count)
    {
        Vector2 axis = acrossDirection.sqrMagnitude > 0.0001f ? acrossDirection.normalized : Vector2.right;
        Vector2 perpendicular = new Vector2(-axis.y, axis.x);
        float lateralJitter = Mathf.Max(0.3f, (outerRadius - innerRadius) * 0.15f);
        var result = new List<Vector2>(count);

        for (int i = 0; i < count; i++)
        {
            float t = count > 1 ? (i / (float)(count - 1)) * 2f - 1f : 0f;
            float distance = innerRadius + Mathf.Abs(t) * (outerRadius - innerRadius);
            float sign = t < 0f ? -1f : 1f;
            Vector2 along = axis * (sign * distance);
            Vector2 lateral = perpendicular * (((float)rng.NextDouble() * 2f - 1f) * lateralJitter);
            result.Add(center + along + lateral);
        }

        return result;
    }

    private static List<Vector2> Lobulos(System.Random rng, Vector2 center, float innerRadius, float outerRadius, int count)
    {
        float baseAngle = RandomAngle(rng);
        float step = Mathf.PI * 2f / count;
        float radiusJitter = Mathf.Max(0.2f, (outerRadius - innerRadius) * 0.08f);
        var result = new List<Vector2>(count);

        for (int i = 0; i < count; i++)
        {
            float angle = baseAngle + step * i + RandomJitter(rng, 8f) * Mathf.Deg2Rad;
            float radius = outerRadius - (float)rng.NextDouble() * radiusJitter;
            result.Add(center + FromPolar(angle, radius));
        }

        return result;
    }

    private static Vector2 ClampToMinRadius(Vector2 point, Vector2 center, float innerRadius)
    {
        Vector2 offset = point - center;
        float distance = offset.magnitude;
        if (distance >= innerRadius) return point;

        Vector2 direction = distance > 0.0001f ? offset / distance : Vector2.up;
        return center + direction * innerRadius;
    }

    private static float RandomAngle(System.Random rng) => (float)(rng.NextDouble() * Mathf.PI * 2f);

    private static float RandomJitter(System.Random rng, float maxDegrees) =>
        ((float)rng.NextDouble() * 2f - 1f) * maxDegrees;

    private static Vector2 FromPolar(float angle, float radius) =>
        new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
}
}
