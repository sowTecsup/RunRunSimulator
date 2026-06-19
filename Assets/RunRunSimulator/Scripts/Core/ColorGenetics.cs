using UnityEngine;

public static class ColorGenetics
{
    public static Color RandomBase() => Random.ColorHSV(0f, 1f, 0.6f, 1f, 0.6f, 1f);

    public static Color DeriveShadow(Color baseColor)
    {
        Color.RGBToHSV(baseColor, out float h, out float s, out float v);
        return Color.HSVToRGB(h, Mathf.Clamp01(s + 0.10f), Mathf.Clamp01(v * 0.55f));
    }

    public static Color DeriveOutline(Color baseColor)
    {
        Color.RGBToHSV(baseColor, out float h, out float s, out float v);
        return Color.HSVToRGB(h, Mathf.Clamp01(s + 0.15f), Mathf.Clamp01(v * 0.25f));
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
