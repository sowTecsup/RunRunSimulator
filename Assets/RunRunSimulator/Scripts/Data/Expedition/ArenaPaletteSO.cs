using System;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "ArenaPalette", menuName = "RunRunSimulator/Expedition/Arena Palette")]
public class ArenaPaletteSO : SerializedScriptableObject
{
    [Serializable]
    public struct Ramp
    {
        public Color Dark;
        public Color Mid;
        public Color Light;

        public Ramp(Color dark, Color mid, Color light)
        {
            Dark = dark;
            Mid = mid;
            Light = light;
        }

        public Color Evaluate(float t) =>
            t < 0.5f ? Color.Lerp(Dark, Mid, t * 2f) : Color.Lerp(Mid, Light, (t - 0.5f) * 2f);
    }

    public string DisplayName = "Pradera";

    [Title("Rampas por material")]
    public Ramp Ground = new Ramp(new Color(0.22f, 0.42f, 0.16f), new Color(0.45f, 0.66f, 0.28f), new Color(0.72f, 0.84f, 0.45f));
    public Ramp Grass = new Ramp(new Color(0.2f, 0.45f, 0.18f), new Color(0.42f, 0.7f, 0.3f), new Color(0.75f, 0.9f, 0.5f));
    public Ramp Foliage = new Ramp(new Color(0.12f, 0.35f, 0.14f), new Color(0.3f, 0.6f, 0.22f), new Color(0.62f, 0.82f, 0.36f));
    public Ramp Trunk = new Ramp(new Color(0.25f, 0.16f, 0.1f), new Color(0.45f, 0.3f, 0.18f), new Color(0.65f, 0.5f, 0.35f));
    public Ramp Rock = new Ramp(new Color(0.3f, 0.32f, 0.34f), new Color(0.55f, 0.56f, 0.55f), new Color(0.8f, 0.8f, 0.76f));
    public Ramp Wall = new Ramp(new Color(0.2f, 0.17f, 0.14f), new Color(0.3f, 0.26f, 0.22f), new Color(0.45f, 0.4f, 0.34f));

    [Title("Luz y aire")]
    public Color SunColor = new Color(1f, 0.96f, 0.88f);
    [Min(0f)] public float SunIntensity = 1.3f;
    public Color AmbientColor = new Color(0.45f, 0.5f, 0.55f);
    public Color FogColor = new Color(0.7f, 0.8f, 0.85f);
    [Range(0f, 0.05f)] public float FogDensity = 0.006f;
    public Color SkyColor = new Color(0.55f, 0.75f, 0.9f);

    public Ramp RampFor(ArenaPaletteSlot slot)
    {
        switch (slot)
        {
            case ArenaPaletteSlot.Grass: return Grass;
            case ArenaPaletteSlot.Foliage: return Foliage;
            case ArenaPaletteSlot.Trunk: return Trunk;
            case ArenaPaletteSlot.Rock: return Rock;
            case ArenaPaletteSlot.Wall: return Wall;
            default: return Ground;
        }
    }
}
}
