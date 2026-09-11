using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MoriMonchiSimulator
{
public class MonchiVisualizer : MonoBehaviour
{
    [Required, SerializeField] private Transform modelRoot;

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int Shade1ColorId = Shader.PropertyToID("_1st_ShadeColor");
    private static readonly int Shade2ColorId = Shader.PropertyToID("_2nd_ShadeColor");
    private static readonly int RimColorId = Shader.PropertyToID("_RimLightColor");
    private static readonly int RimPowerId = Shader.PropertyToID("_RimLight_Power");
    private static readonly int RimInsideMaskId = Shader.PropertyToID("_RimLight_InsideMask");
    private static readonly int RimLightColorSwitchId = Shader.PropertyToID("_Is_LightColor_RimLight");

    private MonchiVisualBankSO bank;
    private FurTypeDatabaseSO furDatabase;
    private GameObject bodyInstance;
    private Animator animator;
    private SkinnedMeshRenderer faceRenderer;
    private readonly List<SkinnedMeshRenderer> tintRenderers = new();
    private CreatureDNA currentDna;
    private MonchiMood currentMood = MonchiMood.Neutral;
    private bool rimOverride;
    private Color rimOverrideColor;
    private float rimOverridePower;
    private float rimOverrideInsideMask;

    public Animator Animator => animator;
    public Transform ModelRoot => Root;

    private Transform Root => modelRoot != null ? modelRoot : transform;

    public void SetBank(MonchiVisualBankSO visualBank)
    {
        bank = visualBank;
    }

    public void SetFurDatabase(FurTypeDatabaseSO furDb)
    {
        furDatabase = furDb;
    }

    public void Assemble(CreatureDNA dna)
    {
        for (int i = Root.childCount - 1; i >= 0; i--)
        {
            var child = Root.GetChild(i).gameObject;
            child.SetActive(false);
            Object.Destroy(child);
        }

        bodyInstance = null;
        animator = null;
        faceRenderer = null;
        tintRenderers.Clear();

        if (bank == null)
        {
            Debug.LogWarning("[MonchiVisualizer] No MonchiVisualBankSO — model will be empty.");
            return;
        }

        var prefab = bank.GetBody(dna.BodyShapeID);
        if (prefab == null)
        {
            Debug.LogWarning($"[MonchiVisualizer] No body prefab for BodyShapeID '{dna.BodyShapeID}'.");
            return;
        }

        bodyInstance = Object.Instantiate(prefab, Root);
        bodyInstance.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

        foreach (var childTransform in bodyInstance.GetComponentsInChildren<Transform>(true))
            childTransform.gameObject.layer = Root.gameObject.layer;

        animator = bodyInstance.GetComponent<Animator>();
        if (animator == null)
            animator = bodyInstance.AddComponent<Animator>();
        if (bank.AnimatorController != null)
            animator.runtimeAnimatorController = bank.AnimatorController;

        foreach (var renderer in bodyInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            if (renderer.gameObject.name == "Face")
                faceRenderer = renderer;
            else
                tintRenderers.Add(renderer);
        }

        currentDna = dna;
        ApplyLook();
        SetMood(currentMood);
    }

    public void RefreshLook(CreatureDNA dna)
    {
        currentDna = dna;
        if (bodyInstance == null)
        {
            Assemble(dna);
            return;
        }

        ApplyLook();
        SetMood(currentMood);
    }

    public void SetRimOverride(Color color, float power, float insideMask)
    {
        rimOverride = true;
        rimOverrideColor = color;
        rimOverridePower = power;
        rimOverrideInsideMask = insideMask;
        ApplyLook();
    }

    public void ClearRimOverride()
    {
        if (!rimOverride) return;
        rimOverride = false;
        ApplyLook();
    }

    public void SetMood(MonchiMood mood)
    {
        currentMood = mood;
        if (faceRenderer == null || bank?.MoodSet == null) return;

        var face = bank.MoodSet.GetFace(mood);
        if (face != null)
            faceRenderer.sharedMaterial = face;
    }

    private void ApplyLook()
    {
        if (currentDna == null || tintRenderers.Count == 0) return;

        if (currentDna.IsShiny)
        {
            var gem = bank != null ? bank.GetGem(currentDna.UniqueID) : null;
            if (gem != null)
            {
                foreach (var renderer in tintRenderers)
                {
                    renderer.sharedMaterial = gem;
                    renderer.SetPropertyBlock(new MaterialPropertyBlock());
                }
                return;
            }
        }

        var furMat = furDatabase != null ? furDatabase.GetMaterial(currentDna.FurType) : null;
        ColorGenetics.BuildHarmony(currentDna.BaseColor, out var wing, out var accent);

        foreach (var renderer in tintRenderers)
        {
            var partName = renderer.gameObject.name;
            if (furMat != null)
                renderer.sharedMaterial = furMat;

            Color color;
            if (partName.StartsWith("Wing"))
                color = wing;
            else if (partName.StartsWith("Horn") || partName.StartsWith("Back"))
                color = accent;
            else if (partName == "Teech")
                color = Color.Lerp(Color.white, currentDna.BaseColor, 0.12f);
            else
                color = currentDna.BaseColor;

            Tint(renderer, color);
        }
    }

    private void Tint(Renderer renderer, Color color)
    {
        var palette = ColorGenetics.BuildFurPalette(color, ColorGenetics.DeriveSecondary(color));
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor(BaseColorId, palette.Base);
        mpb.SetColor(Shade1ColorId, palette.Shade1);
        mpb.SetColor(Shade2ColorId, palette.Shade2);
        if (rimOverride)
        {
            mpb.SetColor(RimColorId, rimOverrideColor);
            mpb.SetFloat(RimPowerId, rimOverridePower);
            mpb.SetFloat(RimInsideMaskId, rimOverrideInsideMask);
            mpb.SetFloat(RimLightColorSwitchId, 0f);
        }
        else
        {
            mpb.SetColor(RimColorId, Color.Lerp(color, Color.white, 0.65f));
        }
        renderer.SetPropertyBlock(mpb);
    }
}
}
