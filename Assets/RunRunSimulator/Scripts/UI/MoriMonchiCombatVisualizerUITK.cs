using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{
public class MoriMonchiCombatVisualizerUITK : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField, Range(0.05f, 2f)] private float fillLerpSeconds = 0.4f;
    [SerializeField] private bool uprightOnly = true;

    private VisualElement root;
    private Label         nameLabel;
    private Label         hpValueLabel;
    private Label         atkLabel;
    private Label         spdLabel;
    private VisualElement fill;
    private float         targetPct   = 1f;
    private float         currentPct  = 1f;
    private float         maxHp       = 1f;
    private string        desiredName = "?";
    private string        desiredAtk  = "";
    private string        desiredSpd  = "";
    private bool          staticDirty;
    private Transform     cam;

    public void Bind(string displayName, float attack, float speed)
    {
        desiredName = displayName;
        desiredAtk  = $"ATK {Mathf.RoundToInt(attack)}";
        desiredSpd  = $"VEL {Mathf.RoundToInt(speed)}";
        staticDirty = true;
        targetPct   = 1f;
        currentPct  = 1f;
        Apply();
    }

    public void SetHp(float current, float max)
    {
        maxHp     = Mathf.Max(0f, max);
        targetPct = maxHp > 0f ? Mathf.Clamp01(current / maxHp) : 0f;
    }

    private bool EnsureRefs()
    {
        if (document == null) document = GetComponentInChildren<UIDocument>(true);
        if (document == null) return false;
        var docRoot = document.rootVisualElement;
        if (docRoot == null) return false;
        if (docRoot == root && nameLabel != null && fill != null) return true;

        root         = docRoot;
        nameLabel    = root.Q<Label>("name");
        fill         = root.Q<VisualElement>("fill");
        hpValueLabel = root.Q<Label>("hp-value");
        atkLabel     = root.Q<Label>("atk");
        spdLabel     = root.Q<Label>("spd");
        staticDirty  = true;
        return nameLabel != null && fill != null;
    }

    private void Update() => Apply();

    private void Apply()
    {
        if (!EnsureRefs()) return;
        if (staticDirty)
        {
            if (nameLabel != null) nameLabel.text = desiredName;
            if (atkLabel  != null) atkLabel.text  = desiredAtk;
            if (spdLabel  != null) spdLabel.text  = desiredSpd;
            staticDirty = false;
        }
        if (!Mathf.Approximately(currentPct, targetPct))
        {
            float t = fillLerpSeconds > 0f ? Mathf.Min(1f, Time.deltaTime / fillLerpSeconds) : 1f;
            currentPct = Mathf.Lerp(currentPct, targetPct, t);
        }
        if (fill != null) fill.style.width = Length.Percent(currentPct * 100f);
        if (hpValueLabel != null)
            hpValueLabel.text = $"{Mathf.RoundToInt(currentPct * maxHp)} / {Mathf.RoundToInt(maxHp)}";
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
