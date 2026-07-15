using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{
public class MoriMonchiCombatVisualizerUITK : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private bool hideForDebug = false;
    [SerializeField, Range(0.05f, 2f)] private float fillLerpSeconds = 0.4f;
    [SerializeField] private bool uprightOnly = true;

    private VisualElement root;
    private Label         nameLabel;
    private Label         atkLabel;
    private Label         spdLabel;
    private Label         hpValueLabel;
    private VisualElement fill;
    private VisualElement track;
    private VisualElement shieldFill;
    private float         targetPct  = 1f;
    private float         currentPct = 1f;
    private float         maxHp      = 1f;
    private float         desiredShield;
    private bool          activeTurn;
    private bool          targeted;
    private Transform     cam;

    public void Bind()
    {
        targetPct     = 1f;
        currentPct    = 1f;
        desiredShield = 0f;
        activeTurn    = false;
        targeted      = false;
        Apply();
    }

    public void SetHp(float current, float max)
    {
        maxHp     = Mathf.Max(0f, max);
        targetPct = maxHp > 0f ? Mathf.Clamp01(current / maxHp) : 0f;
    }

    public void SetShield(float shield)
    {
        desiredShield = shield;
    }

    public void SetActiveTurn(bool value)
    {
        activeTurn = value;
    }

    public void SetTargeted(bool value)
    {
        targeted = value;
    }

    private bool EnsureRefs()
    {
        if (document == null) document = GetComponentInChildren<UIDocument>(true);
        if (document == null) return false;
        var docRoot = document.rootVisualElement;
        if (docRoot == null) return false;
        if (docRoot == root && fill != null) return true;

        root         = docRoot;
        nameLabel    = root.Q<Label>("name");
        fill         = root.Q<VisualElement>("fill");
        hpValueLabel = root.Q<Label>("hp-value");
        atkLabel     = root.Q<Label>("atk");
        spdLabel     = root.Q<Label>("spd");

        if (nameLabel != null) nameLabel.style.display = DisplayStyle.None;
        if (atkLabel  != null) atkLabel.style.display  = DisplayStyle.None;
        if (spdLabel  != null) spdLabel.style.display  = DisplayStyle.None;

        track = fill != null ? fill.parent : null;
        if (track != null)
        {
            track.style.borderTopWidth    = 2;
            track.style.borderBottomWidth = 2;
            track.style.borderLeftWidth   = 2;
            track.style.borderRightWidth  = 2;

            shieldFill = track.Q<VisualElement>("shield-fill");
            if (shieldFill == null)
            {
                shieldFill = new VisualElement { name = "shield-fill" };
                shieldFill.style.position        = Position.Absolute;
                shieldFill.style.top             = 0;
                shieldFill.style.bottom          = 0;
                shieldFill.style.backgroundColor = new Color(90f / 255f, 160f / 255f, 255f / 255f);
                track.Add(shieldFill);
            }
        }

        return fill != null;
    }

    private void Update() => Apply();

    private void Apply()
    {
        if (!EnsureRefs()) return;
        if (root != null) root.style.display = hideForDebug ? DisplayStyle.None : DisplayStyle.Flex;
        if (hideForDebug) return;

        if (!Mathf.Approximately(currentPct, targetPct))
        {
            float t = fillLerpSeconds > 0f ? Mathf.Min(1f, Time.deltaTime / fillLerpSeconds) : 1f;
            currentPct = Mathf.Lerp(currentPct, targetPct, t);
        }
        if (fill != null) fill.style.width = Length.Percent(currentPct * 100f);
        if (hpValueLabel != null)
        {
            hpValueLabel.text = desiredShield >= 0.5f
                ? $"{Mathf.RoundToInt(currentPct * maxHp)} / {Mathf.RoundToInt(maxHp)}  +{Mathf.RoundToInt(desiredShield)}"
                : $"{Mathf.RoundToInt(currentPct * maxHp)} / {Mathf.RoundToInt(maxHp)}";
        }

        float hpPct     = Mathf.Clamp01(currentPct);
        float shieldPct = maxHp > 0f ? Mathf.Clamp01(desiredShield / maxHp) : 0f;
        shieldPct       = Mathf.Min(shieldPct, 1f - hpPct);
        if (shieldFill != null)
        {
            shieldFill.style.left    = Length.Percent(hpPct * 100f);
            shieldFill.style.width   = Length.Percent(shieldPct * 100f);
            shieldFill.style.display = desiredShield >= 0.5f && shieldPct > 0f ? DisplayStyle.Flex : DisplayStyle.None;
        }

        if (track != null)
        {
            Color borderColor = BorderColor();
            track.style.borderTopColor    = borderColor;
            track.style.borderBottomColor = borderColor;
            track.style.borderLeftColor   = borderColor;
            track.style.borderRightColor  = borderColor;
        }
    }

    private Color BorderColor()
    {
        if (targeted)   return new Color(1f, 72f / 255f, 72f / 255f);
        if (activeTurn) return new Color(1f, 200f / 255f, 60f / 255f);
        return Color.clear;
    }

    private void LateUpdate()
    {
        if (cam == null)
        {
            if (Camera.main == null) return;
            cam = Camera.main.transform;
        }
        Vector3 toCam = transform.position - cam.position;
        if (uprightOnly) toCam.y = 0f;
        if (toCam.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(toCam);
    }
}
}
