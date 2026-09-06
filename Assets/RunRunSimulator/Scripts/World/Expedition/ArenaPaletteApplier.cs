using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;
namespace MoriMonchiSimulator
{

public class ArenaPaletteApplier : MonoBehaviour
{
    private static readonly string[] BaseMapNames = { "_BaseMap", "_Main_Texture", "_Albedo_Map", "_MainTex", "_Texture" };
    private static readonly int BaseMapID = Shader.PropertyToID("_BaseMap");
    private static readonly int RampID = Shader.PropertyToID("_Ramp");
    private static readonly int CutoffID = Shader.PropertyToID("_Cutoff");
    private static readonly int AlphaClipID = Shader.PropertyToID("_AlphaClip");
    private static readonly int WindStrengthID = Shader.PropertyToID("_WindStrength");
    private static readonly int CullID = Shader.PropertyToID("_Cull");

    [Required, SerializeField] private Material paletteMaterial;
    [SerializeField] private List<ArenaPaletteSO> palettes = new();
    [SerializeField] private List<GameObject> roots = new();
    [SerializeField] private Light sun;
    [SerializeField] private Camera skyCamera;
    [SerializeField, Min(0f)] private float foliageWind = 0.05f;
    [SerializeField, Min(0f)] private float grassWind = 0.1f;

    private readonly Dictionary<Material, Material> instanceByOriginal = new();
    private readonly Dictionary<Material, Material> originalByInstance = new();
    private readonly Dictionary<ArenaPaletteSlot, Texture2D> ramps = new();

    public IReadOnlyList<ArenaPaletteSO> Palettes => palettes;
    public ArenaPaletteSO Current { get; private set; }
    public int CurrentIndex { get; private set; } = -1;

    public int IndexForSeed(int seed) => palettes.Count == 0 ? -1 : Mathf.Abs(seed) % palettes.Count;

    public void ApplyIndex(int index)
    {
        if (palettes.Count == 0) return;
        index = ((index % palettes.Count) + palettes.Count) % palettes.Count;
        CurrentIndex = index;
        Apply(palettes[index]);
    }

    public void Apply(ArenaPaletteSO palette)
    {
        if (palette == null || paletteMaterial == null) return;

        Current = palette;
        BuildRamps(palette);

        foreach (var root in roots)
        {
            if (root == null) continue;
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                Remap(renderer);
        }

        ApplyEnvironment(palette);
    }

    private void OnDestroy()
    {
        foreach (var ramp in ramps.Values)
            if (ramp != null) Destroy(ramp);
        foreach (var instance in originalByInstance.Keys)
            if (instance != null) Destroy(instance);
    }

    private void BuildRamps(ArenaPaletteSO palette)
    {
        foreach (ArenaPaletteSlot slot in System.Enum.GetValues(typeof(ArenaPaletteSlot)))
        {
            if (!ramps.TryGetValue(slot, out var texture) || texture == null)
            {
                texture = new Texture2D(256, 1, TextureFormat.RGBA32, false)
                {
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    name = "Ramp_" + slot,
                };
                ramps[slot] = texture;
            }

            var ramp = palette.RampFor(slot);
            for (int x = 0; x < 256; x++)
                texture.SetPixel(x, 0, ramp.Evaluate(x / 255f));
            texture.Apply(false, false);
        }
    }

    private void Remap(Renderer renderer)
    {
        var materials = renderer.sharedMaterials;
        bool changed = false;

        for (int i = 0; i < materials.Length; i++)
        {
            var material = materials[i];
            if (material == null) continue;

            var original = originalByInstance.TryGetValue(material, out var known) ? known : material;
            if (!TryClassify(original, out var slot)) continue;

            var instance = GetInstance(original, slot);
            if (instance != material)
            {
                materials[i] = instance;
                changed = true;
            }
        }

        if (changed) renderer.sharedMaterials = materials;
    }

    private Material GetInstance(Material original, ArenaPaletteSlot slot)
    {
        if (!instanceByOriginal.TryGetValue(original, out var instance) || instance == null)
        {
            instance = new Material(paletteMaterial) { name = original.name + "_Palette" };

            var baseMap = FindBaseMap(original, out string propertyName);
            if (baseMap != null)
            {
                instance.SetTexture(BaseMapID, baseMap);
                instance.SetTextureScale(BaseMapID, original.GetTextureScale(propertyName));
                instance.SetTextureOffset(BaseMapID, original.GetTextureOffset(propertyName));
            }

            bool clip = original.HasProperty("_AlphaClip") && original.GetFloat("_AlphaClip") > 0.5f;
            instance.SetFloat(AlphaClipID, clip ? 1f : 0f);
            if (clip) instance.EnableKeyword("_ALPHACLIP_ON");
            else instance.DisableKeyword("_ALPHACLIP_ON");

            float cutoff = original.HasProperty("_Cutoff") ? original.GetFloat("_Cutoff")
                : original.HasProperty("_Alpha_Clip_Threshold") ? original.GetFloat("_Alpha_Clip_Threshold")
                : 0.5f;
            instance.SetFloat(CutoffID, cutoff);

            float wind = slot == ArenaPaletteSlot.Foliage ? foliageWind : slot == ArenaPaletteSlot.Grass ? grassWind : 0f;
            instance.SetFloat(WindStrengthID, wind);

            float cull = original.HasProperty("_Cull") ? original.GetFloat("_Cull") : (float)CullMode.Back;
            instance.SetFloat(CullID, cull);

            instanceByOriginal[original] = instance;
            originalByInstance[instance] = original;
        }

        instance.SetTexture(RampID, ramps[slot]);
        return instance;
    }

    private static Texture FindBaseMap(Material material, out string propertyName)
    {
        foreach (var name in BaseMapNames)
        {
            if (!material.HasProperty(name)) continue;
            var texture = material.GetTexture(name);
            if (texture == null) continue;
            propertyName = name;
            return texture;
        }

        propertyName = "_BaseMap";
        return null;
    }

    private static bool TryClassify(Material material, out ArenaPaletteSlot slot)
    {
        string n = material.name;

        if (n.Contains("Trunk")) slot = ArenaPaletteSlot.Trunk;
        else if (n.Contains("Leaves") || n.Contains("Tree") || n.Contains("Plants")) slot = ArenaPaletteSlot.Foliage;
        else if (n.Contains("Moss") || n.Contains("Rock") || n.Contains("Pebble") || n.StartsWith("PolygonNature_0")) slot = ArenaPaletteSlot.Rock;
        else if (n.StartsWith("Generic_0") || n.Contains("Grass") || n.Contains("Flower")) slot = ArenaPaletteSlot.Grass;
        else if (n == "ArenaGround" || n == "ArenaOutskirts") slot = ArenaPaletteSlot.Ground;
        else if (n == "ArenaWall") slot = ArenaPaletteSlot.Wall;
        else
        {
            slot = ArenaPaletteSlot.Ground;
            return false;
        }

        return true;
    }

    private void ApplyEnvironment(ArenaPaletteSO palette)
    {
        if (sun != null)
        {
            sun.color = palette.SunColor;
            sun.intensity = palette.SunIntensity;
        }

        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = palette.AmbientColor;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = palette.FogColor;
        RenderSettings.fogDensity = palette.FogDensity;

        if (skyCamera != null)
        {
            skyCamera.clearFlags = CameraClearFlags.SolidColor;
            skyCamera.backgroundColor = palette.SkyColor;
        }
    }
}
}
