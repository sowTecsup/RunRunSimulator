using UnityEngine;
namespace MoriMonchiSimulator
{

public static class SpawnBallistics
{
    public static Vector3 SolveLaunchVelocity(Vector3 origin, Vector3 target, float angleRad)
    {
        Vector3 flat = target - origin; flat.y = 0f;
        float   d    = flat.magnitude;
        float   y    = target.y - origin.y;
        float   g    = Mathf.Abs(Physics.gravity.y); if (g < 0.01f) g = 9.81f;

        float cos = Mathf.Cos(angleRad), sin = Mathf.Sin(angleRad);
        Vector3 dir;
        if (d > 0.001f) dir = flat / d;
        else { Vector2 r = Random.insideUnitCircle.normalized; dir = new Vector3(r.x, 0f, r.y); }

        float denom = 2f * cos * cos * (d * (sin / cos) - y);
        if (denom <= 0.001f)
        {
            float vFallback = Mathf.Sqrt(g * Mathf.Max(d, 2f));
            return dir * (vFallback * cos) + Vector3.up * (vFallback * sin);
        }

        float v = Mathf.Sqrt(g * d * d / denom);
        return dir * (v * cos) + Vector3.up * (v * sin);
    }

    public static Transform ResolvePlayer()
    {
        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null) return tagged.transform;
        return Camera.main != null ? Camera.main.transform : null;
    }

    public static void DrawSimulatedArc(Vector3 origin, Vector3 vel, float groundY, float g)
    {
        const int   steps = 48;
        const float dt    = 0.04f;
        Vector3 p = origin, v = vel, prev = origin;
        for (int i = 0; i < steps; i++)
        {
            v += Vector3.down * g * dt;
            p += v * dt;
            Gizmos.DrawLine(prev, p);
            prev = p;
            if (p.y <= groundY && i > 1) break;
        }
    }

    public static void DrawRing(Vector3 center, float radius, int segments)
    {
        if (radius < 0.05f) return;
        float   step = 2f * Mathf.PI / segments;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float   a    = i * step;
            Vector3 curr = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            Gizmos.DrawLine(prev, curr);
            prev = curr;
        }
    }
}
}
