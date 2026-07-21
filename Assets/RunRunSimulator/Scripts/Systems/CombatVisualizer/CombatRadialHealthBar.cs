using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
namespace MoriMonchiSimulator
{
public class CombatRadialHealthBar : MonoBehaviour
{
    [SerializeField] private float ringScale = 1.6f;
    [SerializeField] private float facingAngleOffset = 0f;
    [SerializeField] private float hoverRadius = 0.45f;
    [SerializeField] private float hoverHeight = 1.4f;
    [SerializeField] private float flashSeconds = 0.12f;
    [SerializeField] private float drainSeconds = 0.3f;
    [SerializeField] private Color ghostColor = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] private float ghostDelay = 0.35f;
    [SerializeField] private float ghostSeconds = 0.5f;
    [SerializeField] private float shakeAmplitude = 0.08f;
    [SerializeField] private float shakeSeconds = 0.35f;
    [SerializeField] private float punchSeconds = 0.2f;
    [SerializeField] private Color[] shieldLayerColors =
    {
        new Color32(0x5A, 0xA0, 0xFF, 0xFF),
        new Color32(0x9B, 0x59, 0xB6, 0xFF),
        new Color32(0xE8, 0x6F, 0xC7, 0xFF)
    };

    private static readonly Color HpColorLow  = new Color32(0xE7, 0x4C, 0x3C, 0xFF);
    private static readonly Color HpColorHigh = new Color32(0x58, 0xD6, 0x8D, 0xFF);
    private const float ThickRingInner = 0.70f;
    private const float ThinRingInner  = 0.86f;
    private const float RingOuter      = 0.98f;

    private static Sprite thickRingSprite;
    private static Sprite thinRingSprite;
    private static Sprite ticksSprite;
    private static Material overlayMaterial;
    private static Material defaultMaterial;

    private static readonly List<CombatRadialHealthBar> instances = new List<CombatRadialHealthBar>();
    private static CombatRadialHealthBar mathWinner;
    private static int mathWinnerFrame = -1;

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform canvasRoot;
    private Image track;
    private Image hpGhostRight;
    private Image hpGhostLeft;
    private Image hpFillRight;
    private Image hpFillLeft;
    private Image shieldUnderRight;
    private Image shieldUnderLeft;
    private Image shieldFillRight;
    private Image shieldFillLeft;
    private Image ticks;
    private Text hpLabel;

    private float currentHp = 1f;
    private float maxHp = 1f;
    private float currentShield;
    private bool activeTurn;
    private bool mathHover;
    private bool externalHover;
    private bool hoverActive;
    private bool hasIdentity;
    private CombatVisualSide side;
    private int index;
    private Transform facingTarget;
    private Vector3 canvasBaseLocalPos;

    private Coroutine hpRoutine;
    private Coroutine ghostRoutine;
    private Coroutine shakeRoutine;
    private Coroutine punchRoutine;

    private void Awake()
    {
        EnsureOverlayMaterial();
        EnsureDefaultMaterial();
        Build();
    }

    private void OnEnable()
    {
        CombatVisualEvents.OnUnitHover += HandleUnitHover;
        instances.Add(this);
    }

    private void OnDisable()
    {
        CombatVisualEvents.OnUnitHover -= HandleUnitHover;
        instances.Remove(this);
        if (mathWinner == this) mathWinner = null;
        StopJuiceRoutines();
    }

    private void Update()
    {
        UpdateHover();

        bool combinedHover = mathHover || externalHover;
        if (combinedHover != hoverActive)
        {
            hoverActive = combinedHover;
            SetHoverState(hoverActive);
        }
    }

    private void HandleUnitHover(CombatVisualSide s, int i, bool hover)
    {
        if (!hasIdentity) return;
        if (s != side || i != index) return;
        externalHover = hover;
    }

    public void Bind(CombatVisualSide side, int index)
    {
        this.side = side;
        this.index = index;
        hasIdentity = true;
        currentHp = 1f;
        maxHp = 1f;
        currentShield = 0f;
        activeTurn = false;
        StopJuiceRoutines();
        ApplyHpImmediate();
        ApplyShield();
    }

    public void SetHp(float current, float max)
    {
        float newMax = Mathf.Max(0f, max);
        float newCurrent = Mathf.Clamp(current, 0f, newMax);
        float oldPct = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
        float oldHp = currentHp;

        maxHp = newMax;
        currentHp = newCurrent;
        float newPct = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;

        StopJuiceRoutines();

        if (!isActiveAndEnabled)
        {
            ApplyHpImmediate();
            return;
        }

        if (newPct < oldPct - 0.0001f)
        {
            float damage = oldHp - currentHp;
            ghostRoutine = StartCoroutine(GhostRoutine(oldPct, newPct));
            hpRoutine = StartCoroutine(DamageRoutine(oldPct, newPct, oldHp, currentHp));
            shakeRoutine = StartCoroutine(ShakeRoutine(damage));
            punchRoutine = StartCoroutine(PunchRoutine());
        }
        else
        {
            SetGhostPct(newPct);
            hpRoutine = StartCoroutine(HealRoutine(oldPct, newPct, oldHp, currentHp));
        }
    }

    public void SnapHp(float current, float max)
    {
        StopJuiceRoutines();
        maxHp = Mathf.Max(0f, max);
        currentHp = Mathf.Clamp(current, 0f, maxHp);
        ApplyHpImmediate();
    }

    public void SetShield(float shield)
    {
        currentShield = Mathf.Max(0f, shield);
        ApplyShield();
    }

    public void SetActiveTurn(bool value)
    {
        activeTurn = value;
    }

    public void SetTargeted(bool value)
    {
    }

    public void SetFacingTarget(Transform target)
    {
        facingTarget = target;
        ApplyFixedFacing();
    }

    private IEnumerator DamageRoutine(float oldPct, float newPct, float oldHpVal, float newHpVal)
    {
        Color baseColor = Color.Lerp(HpColorLow, HpColorHigh, oldPct);
        yield return FlashRoutine(baseColor);

        float t = 0f;
        while (t < drainSeconds)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / drainSeconds);
            float eu = 1f - (1f - u) * (1f - u);
            float pct = Mathf.Lerp(oldPct, newPct, eu);
            SetFillPct(pct);
            SetHpLabelValue(Mathf.Lerp(oldHpVal, newHpVal, eu));
            yield return null;
        }
        SetFillPct(newPct);
        SetHpLabelValue(newHpVal);
        hpRoutine = null;
    }

    private IEnumerator HealRoutine(float oldPct, float newPct, float oldHpVal, float newHpVal)
    {
        float t = 0f;
        while (t < drainSeconds)
        {
            t += Time.deltaTime;
            float u = Mathf.Clamp01(t / drainSeconds);
            float eu = 1f - (1f - u) * (1f - u);
            float pct = Mathf.Lerp(oldPct, newPct, eu);
            SetFillPct(pct);
            SetFillColor(Color.Lerp(HpColorLow, HpColorHigh, pct));
            SetHpLabelValue(Mathf.Lerp(oldHpVal, newHpVal, eu));
            yield return null;
        }
        SetFillPct(newPct);
        SetFillColor(Color.Lerp(HpColorLow, HpColorHigh, newPct));
        SetHpLabelValue(newHpVal);
        hpRoutine = null;
    }

    private IEnumerator FlashRoutine(Color baseColor)
    {
        float half = flashSeconds * 0.5f;
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            SetFillColor(Color.Lerp(baseColor, Color.white, Mathf.Clamp01(t / half)));
            yield return null;
        }
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            SetFillColor(Color.Lerp(Color.white, baseColor, Mathf.Clamp01(t / half)));
            yield return null;
        }
        SetFillColor(baseColor);
    }

    private IEnumerator GhostRoutine(float oldPct, float newPct)
    {
        SetGhostPct(oldPct);

        float t = 0f;
        while (t < ghostDelay)
        {
            t += Time.deltaTime;
            yield return null;
        }

        t = 0f;
        while (t < ghostSeconds)
        {
            t += Time.deltaTime;
            float eu = EaseInOut(Mathf.Clamp01(t / ghostSeconds));
            SetGhostPct(Mathf.Lerp(oldPct, newPct, eu));
            yield return null;
        }

        SetGhostPct(newPct);
        ghostRoutine = null;
    }

    private IEnumerator ShakeRoutine(float damage)
    {
        float amp = shakeAmplitude * Mathf.Clamp01(damage / Mathf.Max(1f, maxHp) * 4f);
        float t = 0f;
        while (t < shakeSeconds)
        {
            t += Time.deltaTime;
            float damp = 1f - Mathf.Clamp01(t / shakeSeconds);
            Vector2 offset = Random.insideUnitCircle * amp * damp;
            canvasRoot.localPosition = canvasBaseLocalPos + new Vector3(offset.x, 0f, offset.y);
            yield return null;
        }
        canvasRoot.localPosition = canvasBaseLocalPos;
        shakeRoutine = null;
    }

    private IEnumerator PunchRoutine()
    {
        Vector3 baseScale = Vector3.one * (ringScale / 100f);
        float half = punchSeconds * 0.5f;
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            canvasRoot.localScale = baseScale * Mathf.Lerp(1f, 1.12f, Mathf.Clamp01(t / half));
            yield return null;
        }
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            canvasRoot.localScale = baseScale * Mathf.Lerp(1.12f, 1f, Mathf.Clamp01(t / half));
            yield return null;
        }
        canvasRoot.localScale = baseScale;
        punchRoutine = null;
    }

    private void StopJuiceRoutines()
    {
        if (hpRoutine != null) { StopCoroutine(hpRoutine); hpRoutine = null; }
        if (ghostRoutine != null) { StopCoroutine(ghostRoutine); ghostRoutine = null; }
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
            if (canvasRoot != null) canvasRoot.localPosition = canvasBaseLocalPos;
        }
        if (punchRoutine != null)
        {
            StopCoroutine(punchRoutine);
            punchRoutine = null;
            if (canvasRoot != null) canvasRoot.localScale = Vector3.one * (ringScale / 100f);
        }
    }

    private static float EaseInOut(float t)
    {
        return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
    }

    private void ApplyHpImmediate()
    {
        float pct = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
        SetFillPct(pct);
        SetGhostPct(pct);
        SetFillColor(Color.Lerp(HpColorLow, HpColorHigh, pct));
        SetHpLabelValue(currentHp);
    }

    private void SetFillPct(float pct)
    {
        float half = pct * 0.5f;
        if (hpFillRight != null) hpFillRight.fillAmount = half;
        if (hpFillLeft != null) hpFillLeft.fillAmount = half;
    }

    private void SetFillColor(Color c)
    {
        if (hpFillRight != null) hpFillRight.color = c;
        if (hpFillLeft != null) hpFillLeft.color = c;
    }

    private void SetGhostPct(float pct)
    {
        float half = pct * 0.5f;
        if (hpGhostRight != null) hpGhostRight.fillAmount = half;
        if (hpGhostLeft != null) hpGhostLeft.fillAmount = half;
    }

    private void SetHpLabelValue(float hpValue)
    {
        if (hpLabel != null) hpLabel.text = $"{Mathf.RoundToInt(hpValue)} / {Mathf.RoundToInt(maxHp)}";
    }

    private void ApplyShield()
    {
        if (shieldFillRight == null || shieldFillLeft == null || shieldUnderRight == null || shieldUnderLeft == null) return;

        int s = Mathf.RoundToInt(currentShield);
        if (s <= 0)
        {
            shieldFillRight.gameObject.SetActive(false);
            shieldFillLeft.gameObject.SetActive(false);
            shieldUnderRight.gameObject.SetActive(false);
            shieldUnderLeft.gameObject.SetActive(false);
            return;
        }

        int layer = (s - 1) / 10;
        int rem = s - layer * 10;

        shieldFillRight.gameObject.SetActive(true);
        shieldFillLeft.gameObject.SetActive(true);
        float fillHalf = (rem / 10f) * 0.5f;
        shieldFillRight.fillAmount = fillHalf;
        shieldFillLeft.fillAmount = fillHalf;
        Color fillColor = ShieldColor(layer);
        shieldFillRight.color = fillColor;
        shieldFillLeft.color = fillColor;

        if (layer > 0)
        {
            shieldUnderRight.gameObject.SetActive(true);
            shieldUnderLeft.gameObject.SetActive(true);
            shieldUnderRight.fillAmount = 0.5f;
            shieldUnderLeft.fillAmount = 0.5f;
            Color underColor = ShieldColor(layer - 1);
            shieldUnderRight.color = underColor;
            shieldUnderLeft.color = underColor;
        }
        else
        {
            shieldUnderRight.gameObject.SetActive(false);
            shieldUnderLeft.gameObject.SetActive(false);
        }
    }

    private Color ShieldColor(int layer)
    {
        return shieldLayerColors[layer % shieldLayerColors.Length];
    }

    private void ApplyFixedFacing()
    {
        if (facingTarget == null || canvasRoot == null) return;
        canvasRoot.rotation = Quaternion.Euler(90f, facingTarget.eulerAngles.y + facingAngleOffset, 0f);
    }

    private void UpdateHover()
    {
        if (Time.frameCount != mathWinnerFrame)
        {
            RecomputeMathWinner();
            mathWinnerFrame = Time.frameCount;
        }
        mathHover = mathWinner == this;
    }

    private static void RecomputeMathWinner()
    {
        mathWinner = null;

        var mouse = UnityEngine.InputSystem.Mouse.current;
        if (Camera.main == null || mouse == null) return;

        var ray = Camera.main.ScreenPointToRay(mouse.position.ReadValue());
        float bestDist = float.MaxValue;

        for (int i = 0; i < instances.Count; i++)
        {
            var instance = instances[i];
            if (instance == null || !instance.isActiveAndEnabled || !instance.gameObject.activeInHierarchy) continue;

            Vector3 segA = instance.transform.position;
            Vector3 segB = instance.transform.position + Vector3.up * instance.hoverHeight;
            float dist = ClosestDistanceRayToSegment(ray, segA, segB);

            if (dist <= instance.hoverRadius && dist < bestDist)
            {
                bestDist = dist;
                mathWinner = instance;
            }
        }
    }

    private void SetHoverState(bool hover)
    {
        Sprite ringSpr = hover ? GetThickRingSprite() : GetThinRingSprite();
        Material mat = hover ? overlayMaterial : defaultMaterial;

        ApplyRingVisual(track, ringSpr, mat);
        ApplyRingVisual(hpGhostRight, ringSpr, mat);
        ApplyRingVisual(hpGhostLeft, ringSpr, mat);
        ApplyRingVisual(hpFillRight, ringSpr, mat);
        ApplyRingVisual(hpFillLeft, ringSpr, mat);
        ApplyRingVisual(shieldUnderRight, ringSpr, mat);
        ApplyRingVisual(shieldUnderLeft, ringSpr, mat);
        ApplyRingVisual(shieldFillRight, ringSpr, mat);
        ApplyRingVisual(shieldFillLeft, ringSpr, mat);

        if (ticks != null) ticks.material = mat;
        if (hpLabel != null)
        {
            hpLabel.material = mat;
            hpLabel.gameObject.SetActive(hover);
        }

        if (hover)
        {
            if (punchRoutine != null) StopCoroutine(punchRoutine);
            punchRoutine = StartCoroutine(PunchRoutine());
        }
    }

    private static void ApplyRingVisual(Image image, Sprite sprite, Material material)
    {
        if (image == null) return;
        image.sprite = sprite;
        image.material = material;
    }

    private static float ClosestDistanceRayToSegment(Ray ray, Vector3 segA, Vector3 segB)
    {
        Vector3 d1 = ray.direction;
        Vector3 d2 = segB - segA;
        Vector3 r = ray.origin - segA;

        float a = Vector3.Dot(d1, d1);
        float e = Vector3.Dot(d2, d2);
        float f = Vector3.Dot(d2, r);

        const float epsilon = 1e-8f;
        float s, t;

        if (a <= epsilon && e <= epsilon)
        {
            s = 0f;
            t = 0f;
        }
        else if (a <= epsilon)
        {
            t = 0f;
            s = Mathf.Clamp01(f / e);
        }
        else
        {
            float c = Vector3.Dot(d1, r);
            if (e <= epsilon)
            {
                s = 0f;
                t = Mathf.Max(0f, -c / a);
            }
            else
            {
                float b = Vector3.Dot(d1, d2);
                float denom = a * e - b * b;
                t = denom != 0f ? Mathf.Max(0f, (b * f - c * e) / denom) : 0f;
                s = (b * t + f) / e;

                if (s < 0f)
                {
                    s = 0f;
                    t = Mathf.Max(0f, -c / a);
                }
                else if (s > 1f)
                {
                    s = 1f;
                    t = Mathf.Max(0f, (b - c) / a);
                }
            }
        }

        Vector3 closestOnRay = ray.origin + d1 * t;
        Vector3 closestOnSeg = segA + d2 * s;
        return Vector3.Distance(closestOnRay, closestOnSeg);
    }

    private static void EnsureOverlayMaterial()
    {
        if (overlayMaterial != null) return;
        var shader = Shader.Find("MoriMonchi/UIRingOverlay");
        overlayMaterial = new Material(shader);
    }

    private static void EnsureDefaultMaterial()
    {
        if (defaultMaterial != null) return;
        defaultMaterial = new Material(Shader.Find("UI/Default"));
    }

    private void Build()
    {
        var canvasGO = new GameObject("RadialHealthBarCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 500;

        canvasGroup = canvasGO.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        canvasRoot = canvasGO.GetComponent<RectTransform>();
        canvasRoot.sizeDelta = new Vector2(100f, 100f);
        canvasRoot.localScale = Vector3.one * (ringScale / 100f);
        canvasBaseLocalPos = canvasRoot.localPosition;

        Sprite thinSprite = GetThinRingSprite();
        Sprite tick = GetTicksSprite();

        track            = CreateLayer(canvasRoot, "Track", thinSprite, Image.Type.Simple);
        hpGhostRight     = CreateLayer(canvasRoot, "HpGhostRight", thinSprite, Image.Type.Filled);
        hpGhostLeft      = CreateLayer(canvasRoot, "HpGhostLeft", thinSprite, Image.Type.Filled);
        hpFillRight      = CreateLayer(canvasRoot, "HpFillRight", thinSprite, Image.Type.Filled);
        hpFillLeft       = CreateLayer(canvasRoot, "HpFillLeft", thinSprite, Image.Type.Filled);
        shieldUnderRight = CreateLayer(canvasRoot, "ShieldUnderRight", thinSprite, Image.Type.Filled);
        shieldUnderLeft  = CreateLayer(canvasRoot, "ShieldUnderLeft", thinSprite, Image.Type.Filled);
        shieldFillRight  = CreateLayer(canvasRoot, "ShieldFillRight", thinSprite, Image.Type.Filled);
        shieldFillLeft   = CreateLayer(canvasRoot, "ShieldFillLeft", thinSprite, Image.Type.Filled);
        ticks            = CreateLayer(canvasRoot, "Ticks", tick, Image.Type.Simple);

        ConfigureFilled(hpGhostRight, true);
        ConfigureFilled(hpGhostLeft, false);
        ConfigureFilled(hpFillRight, true);
        ConfigureFilled(hpFillLeft, false);
        ConfigureFilled(shieldUnderRight, true);
        ConfigureFilled(shieldUnderLeft, false);
        ConfigureFilled(shieldFillRight, true);
        ConfigureFilled(shieldFillLeft, false);

        track.rectTransform.localScale = Vector3.one * 1.05f;
        track.color = new Color(0f, 0f, 0f, 0.45f);

        hpGhostRight.color = ghostColor;
        hpGhostLeft.color = ghostColor;

        ticks.color = new Color(0f, 0f, 0f, 0.5f);

        shieldUnderRight.gameObject.SetActive(false);
        shieldUnderLeft.gameObject.SetActive(false);
        shieldFillRight.gameObject.SetActive(false);
        shieldFillLeft.gameObject.SetActive(false);

        BuildHpLabel(canvasRoot);

        ApplyHpImmediate();
        ApplyShield();
    }

    private void BuildHpLabel(RectTransform parent)
    {
        var labelGo = new GameObject("HpLabel", typeof(RectTransform), typeof(Text));
        labelGo.transform.SetParent(parent, false);

        var labelRect = labelGo.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        hpLabel = labelGo.GetComponent<Text>();
        hpLabel.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hpLabel.alignment = TextAnchor.MiddleCenter;
        hpLabel.fontSize = 200;
        hpLabel.color = Color.white;
        hpLabel.raycastTarget = false;
        hpLabel.material = defaultMaterial;
        labelGo.transform.localScale = Vector3.one * 0.05f;
        labelGo.SetActive(false);
    }

    private static Image CreateLayer(RectTransform parent, string name, Sprite sprite, Image.Type type)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.type = type;
        image.raycastTarget = false;
        image.material = defaultMaterial;
        return image;
    }

    private static void ConfigureFilled(Image image, bool clockwise)
    {
        image.fillMethod = Image.FillMethod.Radial360;
        image.fillOrigin = (int)Image.Origin360.Top;
        image.fillClockwise = clockwise;
        image.fillAmount = 0f;
    }

    private static Sprite GetThickRingSprite()
    {
        if (thickRingSprite != null) return thickRingSprite;
        thickRingSprite = BuildRingSprite(ThickRingInner, RingOuter);
        return thickRingSprite;
    }

    private static Sprite GetThinRingSprite()
    {
        if (thinRingSprite != null) return thinRingSprite;
        thinRingSprite = BuildRingSprite(ThinRingInner, RingOuter);
        return thinRingSprite;
    }

    private static Sprite BuildRingSprite(float inner, float outer)
    {
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float center = size / 2f;
        float radius = size / 2f;
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - center;
                float dy = y + 0.5f - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                bool inRing = dist >= inner && dist <= outer;
                pixels[y * size + x] = inRing ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private static Sprite GetTicksSprite()
    {
        if (ticksSprite != null) return ticksSprite;

        const int size = 256;
        const float tickHalfWidthDeg = 1f;
        const float tickSpacingDeg = 36f;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float center = size / 2f;
        float radius = size / 2f;
        var pixels = new Color32[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x + 0.5f - center;
                float dy = y + 0.5f - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy) / radius;
                bool onTick = false;

                if (dist >= ThickRingInner && dist <= RingOuter)
                {
                    float angle = Mathf.Atan2(dx, dy) * Mathf.Rad2Deg;
                    if (angle < 0f) angle += 360f;
                    float mod = angle % tickSpacingDeg;
                    float delta = mod <= tickSpacingDeg * 0.5f ? mod : tickSpacingDeg - mod;
                    onTick = delta <= tickHalfWidthDeg;
                }

                pixels[y * size + x] = onTick ? new Color32(0, 0, 0, 255) : new Color32(0, 0, 0, 0);
            }
        }

        tex.SetPixels32(pixels);
        tex.Apply();
        ticksSprite = Sprite.Create(tex, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
        return ticksSprite;
    }
}
}
