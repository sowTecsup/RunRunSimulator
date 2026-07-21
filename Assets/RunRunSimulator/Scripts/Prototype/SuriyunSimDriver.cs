using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator
{
public class SuriyunSimDriver : MonoBehaviour
{
    [SerializeField] private int seed = 4242;
    [SerializeField] private bool randomizeEachPlay = false;
    [SerializeField] private int shinyCount = 2;
    [SerializeField] private Vector2 cycleSeconds = new Vector2(2f, 5f);
    [SerializeField] private Vector2 saturationRange = new Vector2(0.28f, 0.56f);
    [SerializeField] private Vector2 valueRange = new Vector2(0.78f, 0.96f);

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int Shade1Id = Shader.PropertyToID("_1st_ShadeColor");
    private static readonly int Shade2Id = Shader.PropertyToID("_2nd_ShadeColor");
    private static readonly int RimId = Shader.PropertyToID("_RimLightColor");

    private static readonly string[] States =
    {
        "IdleA", "Walk 0", "Run 0", "Jump 0", "Eat 0", "Rest",
        "Roar", "Fire", "Sick", "Yes 0", "No 0", "Anim_Dra_Damage"
    };

    private readonly List<Material> furMats = new List<Material>();
    private readonly List<Material> faceMats = new List<Material>();
    private readonly List<Material> gemMats = new List<Material>();
    private readonly List<GameObject> dragons = new List<GameObject>();
    private float[] nextSwitch;
    private System.Random rnd;

    private void Start()
    {
        rnd = randomizeEachPlay ? new System.Random() : new System.Random(seed);
        LoadBanks();
        CollectDragons();
        if (dragons.Count == 0 || furMats.Count == 0) return;

        var shinies = new HashSet<int>();
        while (shinies.Count < Mathf.Min(shinyCount, dragons.Count))
            shinies.Add(rnd.Next(dragons.Count));

        for (int i = 0; i < dragons.Count; i++)
            ApplyLook(dragons[i], shinies.Contains(i));

        nextSwitch = new float[dragons.Count];
        for (int i = 0; i < nextSwitch.Length; i++)
            nextSwitch[i] = Time.time + (float)rnd.NextDouble() * cycleSeconds.x;
    }

    private void Update()
    {
        if (nextSwitch == null) return;
        for (int i = 0; i < dragons.Count; i++)
        {
            if (dragons[i] == null || Time.time < nextSwitch[i]) continue;
            nextSwitch[i] = Time.time + Mathf.Lerp(cycleSeconds.x, cycleSeconds.y, (float)rnd.NextDouble());

            var animator = dragons[i].GetComponent<Animator>();
            if (animator != null)
                animator.CrossFade(States[rnd.Next(States.Length)], 0.15f);

            if (rnd.NextDouble() < 0.5 && faceMats.Count > 0)
                SwapFace(dragons[i]);
        }
    }

    private void LoadBanks()
    {
        for (int i = 0; i < 33; i++)
        {
            var m = Resources.Load<Material>("Materials/MoriMochi/FurPatterns/MonchiFur_" + i.ToString("00"));
            if (m != null) furMats.Add(m);
        }
        for (int i = 1; i <= 25; i++)
        {
            var m = Resources.Load<Material>("Materials/MoriMochi/Faces/MonchiFace_" + i.ToString("00"));
            if (m != null) faceMats.Add(m);
        }
        foreach (var name in new[] { "Gold", "Ruby", "Emerald", "Sapphire", "Amethyst" })
        {
            var m = Resources.Load<Material>("Materials/MoriMochi/Gems/MonchiGem_" + name);
            if (m != null) gemMats.Add(m);
        }
    }

    private void CollectDragons()
    {
        foreach (var root in gameObject.scene.GetRootGameObjects())
            if (root.name.StartsWith("Dragon_"))
                dragons.Add(root);
    }

    private void ApplyLook(GameObject dragon, bool shiny)
    {
        var gem = shiny && gemMats.Count > 0 ? gemMats[rnd.Next(gemMats.Count)] : null;
        var furMat = furMats[rnd.Next(furMats.Count)];

        var baseColor = Color.HSVToRGB(
            (float)rnd.NextDouble(),
            Mathf.Lerp(saturationRange.x, saturationRange.y, (float)rnd.NextDouble()),
            Mathf.Lerp(valueRange.x, valueRange.y, (float)rnd.NextDouble()));
        BuildHarmony(baseColor, out var wingColor, out var accentColor);

        foreach (var r in dragon.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            var partName = r.gameObject.name;
            if (partName == "Face")
            {
                if (faceMats.Count > 0)
                    r.sharedMaterial = faceMats[rnd.Next(faceMats.Count)];
                continue;
            }
            if (gem != null)
            {
                r.sharedMaterial = gem;
                r.SetPropertyBlock(new MaterialPropertyBlock());
                continue;
            }
            r.sharedMaterial = furMat;
            if (partName == "Wing_A") Tint(r, wingColor);
            else if (partName.StartsWith("Horn") || partName.StartsWith("Back")) Tint(r, accentColor);
            else if (partName == "Teech") Tint(r, Color.Lerp(Color.white, baseColor, 0.12f));
            else Tint(r, baseColor);
        }
    }

    private void BuildHarmony(Color baseColor, out Color wing, out Color accent)
    {
        double roll = rnd.NextDouble();
        if (roll < 0.4)
        {
            wing = ShiftHue(baseColor, 28f, 0.9f);
            accent = ShiftHue(baseColor, -28f, 1f);
        }
        else if (roll < 0.7)
        {
            Color.RGBToHSV(baseColor, out float h, out float s, out float v);
            wing = Color.HSVToRGB(h, Mathf.Clamp01(s * 0.5f), Mathf.Clamp01(v * 1.1f));
            accent = Color.HSVToRGB(h, Mathf.Clamp01(s * 1.15f), v * 0.6f);
        }
        else if (roll < 0.9)
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

    private static Color ShiftHue(Color color, float degrees, float satMul)
    {
        Color.RGBToHSV(color, out float h, out float s, out float v);
        return Color.HSVToRGB(Mathf.Repeat(h + degrees / 360f, 1f), Mathf.Clamp01(s * satMul), v);
    }

    private static void Tint(Renderer renderer, Color color)
    {
        var palette = ColorGenetics.BuildFurPalette(color, ColorGenetics.DeriveSecondary(color));
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor(BaseColorId, palette.Base);
        mpb.SetColor(Shade1Id, palette.Shade1);
        mpb.SetColor(Shade2Id, palette.Shade2);
        mpb.SetColor(RimId, Color.Lerp(color, Color.white, 0.65f));
        renderer.SetPropertyBlock(mpb);
    }

    private void SwapFace(GameObject dragon)
    {
        foreach (var r in dragon.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            if (r.gameObject.name == "Face")
            {
                r.sharedMaterial = faceMats[rnd.Next(faceMats.Count)];
                return;
            }
    }
}
}
