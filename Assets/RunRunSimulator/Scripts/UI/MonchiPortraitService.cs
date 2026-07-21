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
    [SerializeField] private int headshotWidth = 512;
    [SerializeField] private int headshotHeight = 192;
    [SerializeField] private float headshotPadding = 1.05f;
    [SerializeField] private float headshotTopFraction = 0.35f;
    [SerializeField] private float headshotCenterHeight = 0.58f;
    [SerializeField] private float headshotPitch = 2f;
    [SerializeField] private float headshotYaw = 140f;
    [SerializeField] private float headshotRoll = 0f;

    private readonly Dictionary<string, Texture2D> cache = new();
    private readonly Dictionary<string, Sprite> spriteCache = new();
    private readonly Dictionary<string, Texture2D> headshotCache = new();
    private readonly Dictionary<string, Sprite> headshotSpriteCache = new();
    private RenderTexture rt;
    private RenderTexture headshotRt;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

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

        if (headshotRt != null)
        {
            headshotRt.Release();
            Destroy(headshotRt);
        }

        foreach (var texture in cache.Values)
        {
            if (texture != null)
                Destroy(texture);
        }

        foreach (var texture in headshotCache.Values)
        {
            if (texture != null)
                Destroy(texture);
        }

        cache.Clear();
        spriteCache.Clear();
        headshotCache.Clear();
        headshotSpriteCache.Clear();
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

    public Texture2D GetHeadshot(CreatureDNA dna)
    {
        if (dna == null)
            return null;

        var key = string.IsNullOrEmpty(dna.UniqueID) ? dna.ToStringID() : dna.UniqueID;

        if (headshotCache.TryGetValue(key, out var cached) && cached != null)
            return cached;

        return CaptureHeadshot(dna, key);
    }

    public Sprite GetHeadshotSprite(CreatureDNA dna)
    {
        if (dna == null)
            return null;

        var key = string.IsNullOrEmpty(dna.UniqueID) ? dna.ToStringID() : dna.UniqueID;

        if (headshotSpriteCache.TryGetValue(key, out var cachedSprite) && cachedSprite != null)
            return cachedSprite;

        var texture = GetHeadshot(dna);
        if (texture == null)
            return null;

        var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        headshotSpriteCache[key] = sprite;
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

    private Texture2D CaptureHeadshot(CreatureDNA dna, string key)
    {
        if (boothRoot == null || boothVisualizer == null || boothCamera == null)
            return null;

        if (headshotRt == null)
            headshotRt = new RenderTexture(headshotWidth, headshotHeight, 16, RenderTextureFormat.ARGB32);

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

        var headBounds = new Bounds(
            new Vector3(bounds.center.x, bounds.min.y + bounds.size.y * headshotCenterHeight, bounds.center.z),
            new Vector3(bounds.size.x, bounds.size.y * headshotTopFraction, bounds.size.z));

        float radius = headBounds.extents.y * headshotPadding;
        float dist = radius / Mathf.Sin(boothCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        Vector3 dir = Quaternion.Euler(headshotPitch, headshotYaw, 0f) * Vector3.forward;
        boothCamera.transform.position = headBounds.center - dir * dist;
        boothCamera.transform.rotation = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(0f, 0f, headshotRoll);

        boothCamera.aspect = (float)headshotWidth / headshotHeight;
        boothCamera.targetTexture = headshotRt;

        var previousActive = RenderTexture.active;
        RenderTexture.active = headshotRt;
        var prevShadows = QualitySettings.shadows;
        QualitySettings.shadows = UnityEngine.ShadowQuality.Disable;
        boothCamera.Render();
        QualitySettings.shadows = prevShadows;
        var tex = new Texture2D(headshotWidth, headshotHeight, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0, 0, headshotWidth, headshotHeight), 0, 0);
        tex.Apply(false, false);
        RenderTexture.active = previousActive;

        boothCamera.targetTexture = rt;
        boothCamera.ResetAspect();

        boothRoot.SetActive(false);
        headshotCache[key] = tex;
        return tex;
    }
}
}
