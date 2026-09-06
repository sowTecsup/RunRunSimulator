using UnityEngine;
namespace MoriMonchiSimulator
{

public static class VisionProfile
{
    public static void Resolve(CreatureDNA dna, ExpeditionRulesSO rules, out float radius, out float degrees, out float nearRadius)
    {
        float boldness = dna != null ? Mathf.Clamp01(dna.Boldness) : 0.5f;
        float skew = rules.BoldnessVisionSkew * (boldness - 0.5f) * 2f;

        radius = rules.VisionRadius * (1f + skew);
        degrees = Mathf.Clamp(rules.VisionDegrees * (1f - skew), 30f, 360f);
        nearRadius = rules.NearSenseRadius;
    }

    public static bool CanSense(Vector3 forward, Vector3 from, Vector3 target, float radius, float degrees, float nearRadius)
    {
        Vector3 dir = target - from;
        dir.y = 0f;
        float sqrDist = dir.sqrMagnitude;

        if (sqrDist <= nearRadius * nearRadius) return true;
        if (sqrDist > radius * radius) return false;
        if (degrees >= 360f) return true;

        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f || sqrDist < 0.0001f) return true;

        return Vector3.Angle(forward, dir) <= degrees * 0.5f;
    }

    public static float FacingAngle(Vector3 forward)
    {
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) return 0f;
        return Mathf.Atan2(forward.z, forward.x);
    }
}
}
