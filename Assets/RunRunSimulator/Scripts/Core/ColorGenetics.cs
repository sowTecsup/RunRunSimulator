using UnityEngine;
namespace MoriMonchiSimulator
{

public static class ColorGenetics
{
    public static Color RandomBase() => Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.6f, 1f);

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
}

public struct FurPalette
{
    public Color Base;
    public Color Shade1;
    public Color Shade2;
    public Color Rim;
}
}
