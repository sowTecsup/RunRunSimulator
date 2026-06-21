using UnityEngine;
namespace MoriMonchiSimulator
{

public static class SpawnBallistics
{
    // Solves the launch velocity to hit 'target' from 'origin' at elevation 'angleRad', accounting
    // for the height difference between muzzle and floor. Classic projectile range equation rearranged
    // for v:  y = d·tanθ − g·d² / (2·v²·cos²θ)  ⇒  v² = g·d² / (2·cos²θ·(d·tanθ − y)).
    // Falls back to a gentle arc if the target is unreachable at this angle (too close/high).
    public static Vector3 SolveLaunchVelocity(Vector3 origin, Vector3 target, float angleRad)
    {
        Vector3 flat = target - origin; flat.y = 0f;
        float   d    = flat.magnitude;
        float   y    = target.y - origin.y;          // negative when the floor is below the muzzle
        float   g    = Mathf.Abs(Physics.gravity.y); if (g < 0.01f) g = 9.81f;

        float cos = Mathf.Cos(angleRad), sin = Mathf.Sin(angleRad);
        Vector3 dir;
        if (d > 0.001f) dir = flat / d;
        else { Vector2 r = Random.insideUnitCircle.normalized; dir = new Vector3(r.x, 0f, r.y); }

        float denom = 2f * cos * cos * (d * (sin / cos) - y);
        if (denom <= 0.001f)
        {
            float vFallback = Mathf.Sqrt(g * Mathf.Max(d, 2f));   // safe lob toward the target
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
}
}
