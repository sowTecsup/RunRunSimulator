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
    private VisualElement fill;
    private float         targetPct   = 1f;
    private float         currentPct  = 1f;
    private string        desiredName = "?";
    private bool          nameDirty;
    private Transform     cam;

    public void Bind(string displayName)
    {
        desiredName = displayName;
        nameDirty   = true;
        targetPct   = 1f;
        currentPct  = 1f;
        Apply();
    }

    public void SetHp(float pct) => targetPct = Mathf.Clamp01(pct);

    private bool EnsureRefs()
    {
        if (document == null) document = GetComponentInChildren<UIDocument>(true);
        if (document == null) return false;
        var docRoot = document.rootVisualElement;
        if (docRoot == null) return false;
        if (docRoot == root && nameLabel != null && fill != null) return true;

        root      = docRoot;
        nameLabel = root.Q<Label>("name");
        fill      = root.Q<VisualElement>("fill");
        nameDirty = true;
        return nameLabel != null && fill != null;
    }

    private void Update() => Apply();

    private void Apply()
    {
        if (!EnsureRefs()) return;
        if (nameDirty && nameLabel != null) { nameLabel.text = desiredName; nameDirty = false; }
        if (!Mathf.Approximately(currentPct, targetPct))
        {
            float t = fillLerpSeconds > 0f ? Mathf.Min(1f, Time.deltaTime / fillLerpSeconds) : 1f;
            currentPct = Mathf.Lerp(currentPct, targetPct, t);
        }
        if (fill != null) fill.style.width = Length.Percent(currentPct * 100f);
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
