using UnityEngine;

namespace MoriMonchiSimulator
{

public static class ArenaGrassCover
{
    [System.Serializable]
    public struct Settings
    {
        [Min(0.5f)] public float clumpSpacing;
        [Min(0.1f)] public float clumpRadius;
        [Range(0f, 1f)] public float haloDensity;
        [Min(0.5f)] public float patchScale;
        [Range(0f, 1f)] public float patchThreshold;
        [Range(0f, 1f)] public float patchDensity;
        [Range(0f, 1f)] public float looseDensity;
        [Range(0f, 1f)] public float looseHeight;

        public static Settings Default => new Settings { clumpSpacing = 3f, clumpRadius = 1f, haloDensity = 0.15f, patchScale = 6f, patchThreshold = 0.55f, patchDensity = 0.55f, looseDensity = 0.06f, looseHeight = 0.65f };
    }

    public static float Sample(Vector2 point, int seed, in Settings s, out float heightScale)
    {
        int cellX = Mathf.FloorToInt(point.x / s.clumpSpacing);
        int cellZ = Mathf.FloorToInt(point.y / s.clumpSpacing);

        float bestDistance = float.MaxValue;
        float bestRadius = s.clumpRadius;

        for (int ox = -1; ox <= 1; ox++)
        {
            for (int oz = -1; oz <= 1; oz++)
            {
                int cx = cellX + ox;
                int cz = cellZ + oz;

                float offsetX = Mathf.Lerp(0.15f, 0.85f, Hash01(cx, cz, seed * 3 + 0));
                float offsetZ = Mathf.Lerp(0.15f, 0.85f, Hash01(cx, cz, seed * 3 + 1));
                float radiusT = Hash01(cx, cz, seed * 3 + 2);
                float radius = s.clumpRadius * Mathf.Lerp(0.7f, 1.3f, radiusT);

                float centerX = (cx + offsetX) * s.clumpSpacing;
                float centerZ = (cz + offsetZ) * s.clumpSpacing;
                float dx = point.x - centerX;
                float dz = point.y - centerZ;
                float distance = Mathf.Sqrt(dx * dx + dz * dz);

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestRadius = radius;
                }
            }
        }

        float clump = 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(bestRadius * 0.6f, bestRadius, bestDistance));

        float halo = 0f;
        if (bestDistance >= bestRadius && bestDistance <= bestRadius * 2f)
        {
            halo = s.haloDensity * (1f - Mathf.InverseLerp(bestRadius, bestRadius * 2f, bestDistance));
        }

        float noise = Mathf.PerlinNoise((point.x + seed * 0.731f) / s.patchScale, (point.y + seed * 1.173f) / s.patchScale);
        float patch = s.patchDensity * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(s.patchThreshold, s.patchThreshold + 0.15f, noise));

        float loose = s.looseDensity;

        float density = Mathf.Max(Mathf.Max(clump, halo), Mathf.Max(patch, loose));
        density = Mathf.Clamp01(density);

        heightScale = Mathf.Lerp(s.looseHeight, 1.1f, density);

        return density;
    }

    private static float Hash01(int a, int b, int c)
    {
        unchecked
        {
            int h = a * 374761393 + b * 668265263 + c * 1911520717;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            uint u = (uint)h;
            return (u & 0xFFFFFFu) / (float)0xFFFFFF;
        }
    }
}
}
