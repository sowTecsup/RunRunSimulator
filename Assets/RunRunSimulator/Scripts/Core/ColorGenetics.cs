using UnityEngine;
namespace MoriMonchiSimulator
{

public static class ColorGenetics
{
    public static Color RandomBase() => Random.ColorHSV(0f, 1f, 0.35f, 0.6f, 0.8f, 1f);

    public static Color RandomBase(System.Random rng)
    {
        float h = (float)rng.NextDouble();
        float s = Mathf.Lerp(0.35f, 0.6f, (float)rng.NextDouble());
        float v = Mathf.Lerp(0.8f, 1f, (float)rng.NextDouble());
        return Color.HSVToRGB(h, s, v);
    }

    public static Color DeriveSecondary(Color baseColor)
    {
        Color.RGBToHSV(baseColor, out float h, out float s, out float v);
        return Color.HSVToRGB(Mathf.Repeat(h + 0.08f, 1f), Mathf.Clamp01(s * 0.85f), Mathf.Clamp01(v + 0.15f));
    }

    public static FurPalette BuildFurPalette(Color baseColor, Color secondary)
    {
        return new FurPalette
        {
            Base   = baseColor,
            Shade1 = Shade(baseColor, 0.60f, 0.08f),
            Shade2 = Shade(Color.Lerp(baseColor, secondary, 0.35f), 0.40f, 0.12f),
            Rim    = secondary,
        };
    }

    private static Color Shade(Color color, float valueMul, float satAdd)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);
        return Color.HSVToRGB(h, Mathf.Clamp01(s + satAdd), Mathf.Clamp01(v * valueMul));
    }

    public static Color Inherit(Color a, Color b)
    {
        Color blended = Color.Lerp(a, b, Random.value);
        Color.RGBToHSV(blended, out float h, out float s, out float v);
        h = Mathf.Repeat(h + Random.Range(-0.04f, 0.04f), 1f);
        s = Mathf.Clamp01(s + Random.Range(-0.05f, 0.05f));
        v = Mathf.Clamp01(v + Random.Range(-0.05f, 0.05f));
        return Color.HSVToRGB(h, s, v);
    }

    public static FurType Inherit(FurType mother, FurType father)
        => Random.value < 0.5f ? mother : father;

    public const float ShinyChance = 0.005f;

    public static bool RollShiny() => Random.value < ShinyChance;

    public static void BuildHarmony(Color baseColor, out Color wing, out Color accent)
    {
        float roll = DeterministicRoll(baseColor);
        if (roll < 0.4f)
        {
            wing = ShiftHue(baseColor, 28f, 0.9f);
            accent = ShiftHue(baseColor, -28f, 1f);
        }
        else if (roll < 0.7f)
        {
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);
            wing = Color.HSVToRGB(h, Mathf.Clamp01(s * 0.5f), Mathf.Clamp01(v * 1.1f));
            accent = Color.HSVToRGB(h, Mathf.Clamp01(s * 1.15f), v * 0.6f);
        }
        else if (roll < 0.9f)
        {
            wing = ShiftHue(baseColor, 120f, 0.65f);
            accent = ShiftHue(baseColor, 240f, 0.75f);
        }
        else
        {
            wing = ShiftHue(baseColor, 180f, 0.6f);
            accent = ShiftHue(baseColor, 150f, 0.7f);
        }
    }

    private static float DeterministicRoll(Color c)
    {
        int rgb = (Mathf.RoundToInt(c.r * 255f) << 16) | (Mathf.RoundToInt(c.g * 255f) << 8) | Mathf.RoundToInt(c.b * 255f);
        uint x = (uint)rgb * 2654435761u;
        x ^= x >> 15;
        return (x & 0xFFFFFF) / 16777216f;
    }

    private static Color ShiftHue(Color color, float degrees, float satMul)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);
        return Color.HSVToRGB(Mathf.Repeat(h + degrees / 360f, 1f), Mathf.Clamp01(s * satMul), v);
    }
}

public struct FurPalette
{
    public Color Base;
    public Color Shade1;
    public Color Shade2;
    public Color Rim;
}
}
