using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MoriMonchiSimulator
{
public class MonchiPortraitService : MonoBehaviour
{
    public static MonchiPortraitService Instance { get; private set; }

    [Required, SerializeField] private MonchiVisualBankSO visualBank;
    [Required, SerializeField] private FurTypeDatabaseSO furTypeDatabase;
    [Required, SerializeField] private MonchiVisualizer boothVisualizer;
    [Required, SerializeField] private Camera boothCamera;
    [Required, SerializeField] private GameObject boothRoot;
    [SerializeField] private int textureSize = 384;
    [SerializeField] private float framePadding = 1.15f;
    [SerializeField] private float cameraPitch = 12f;
    [SerializeField] private float cameraYaw = 180f;
    [SerializeField] private MonchiMood portraitMood = MonchiMood.Neutral;

    private readonly Dictionary<string, Texture2D> cache = new();
    private readonly Dictionary<string, Sprite> spriteCache = new();
    private RenderTexture rt;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        rt = new RenderTexture(textureSize, textureSize, 16, RenderTextureFormat.ARGB32);
        boothCamera.targetTexture = rt;
        boothCamera.enabled = false;
        boothCamera.clearFlags = CameraClearFlags.SolidColor;
        boothCamera.backgroundColor = Color.clear;
        boothRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (rt != null)
        {
            rt.Release();
            Destroy(rt);
        }

        foreach (var texture in cache.Values)
        {
            if (texture != null)
                Destroy(texture);
        }

        cache.Clear();
        spriteCache.Clear();
    }

    public Texture2D GetPortrait(CreatureDNA dna)
    {
        if (dna == null)
            return null;

        var key = string.IsNullOrEmpty(dna.UniqueID) ? dna.ToStringID() : dna.UniqueID;

        if (cache.TryGetValue(key, out var cached) && cached != null)
            return cached;

        return Capture(dna, key);
    }

    public Sprite GetPortraitSprite(CreatureDNA dna)
    {
        if (dna == null)
            return null;

        var key = string.IsNullOrEmpty(dna.UniqueID) ? dna.ToStringID() : dna.UniqueID;

        if (spriteCache.TryGetValue(key, out var cachedSprite) && cachedSprite != null)
            return cachedSprite;

        var texture = GetPortrait(dna);
        if (texture == null)
            return null;

        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        spriteCache[key] = sprite;
        return sprite;
    }

    private Texture2D Capture(CreatureDNA dna, string key)
    {
        if (boothRoot == null || boothVisualizer == null || boothCamera == null)
            return null;

        boothRoot.SetActive(true);

        boothVisualizer.SetBank(visualBank);
        boothVisualizer.SetFurDatabase(furTypeDatabase);
        boothVisualizer.Assemble(dna);
        boothVisualizer.SetMood(portraitMood);

        var anim = boothVisualizer.Animator;
        if (anim != null)
        {
            anim.Play("Idle", 0, 0f);
            anim.Update(0f);
        }

        var renderers = boothVisualizer.ModelRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        if (renderers.Length == 0)
        {
            boothRoot.SetActive(false);
            return null;
        }

        var bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        float radius = bounds.extents.magnitude * framePadding;
        float dist = radius / Mathf.Sin(boothCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        Vector3 dir = Quaternion.Euler(cameraPitch, cameraYaw, 0f) * Vector3.forward;
        boothCamera.transform.position = bounds.center - dir * dist;
        boothCamera.transform.rotation = Quaternion.LookRotation(dir, Vector3.up);

        var previousActive = RenderTexture.active;
        RenderTexture.active = rt;
        var prevShadows = QualitySettings.shadows;
        QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
        boothCamera.Render();
        QualitySettings.shadows = prevShadows;
        var tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, textureSize, textureSize), 0, 0);
        tex.Apply(false, false);
        RenderTexture.active = previousActive;

        boothRoot.SetActive(false);
        cache[key] = tex;
        return tex;
    }
}
}
