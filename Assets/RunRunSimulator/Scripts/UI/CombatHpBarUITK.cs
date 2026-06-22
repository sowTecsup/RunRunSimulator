using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{
public class CombatHpBarUITK : MonoBehaviour
{
    [SerializeField] private UIDocument       document;
    [SerializeField] private CombatVisualSide side;
    [SerializeField, Range(0.05f, 2f)] private float fillLerpSeconds = 0.4f;

    private VisualElement root;
    private Label         nameLabel;
    private VisualElement fill;
    private float         targetPct  = 1f;
    private float         currentPct = 1f;

    private void OnEnable()
    {
        CombatVisualEvents.OnVisualCombatStart += HandleStart;
        CombatVisualEvents.OnVisualCombatEnd   += HandleEnd;
        CombatVisualEvents.OnHpChanged         += HandleHpChanged;
    }

    private void OnDisable()
    {
        CombatVisualEvents.OnVisualCombatStart -= HandleStart;
        CombatVisualEvents.OnVisualCombatEnd   -= HandleEnd;
        CombatVisualEvents.OnHpChanged         -= HandleHpChanged;
    }

    private void Start()
    {
        if (document == null) { Debug.LogWarning("[CombatHpBarUITK] No UIDocument."); return; }
        root = document.rootVisualElement;
        if (root == null) return;
        nameLabel = root.Q<Label>("name");
        fill      = root.Q<VisualElement>("fill");
        SetVisible(false);
    }

    private void Update()
    {
        if (fill == null) return;
        if (Mathf.Approximately(currentPct, targetPct)) return;
        float t = fillLerpSeconds > 0f ? Mathf.Min(1f, Time.deltaTime / fillLerpSeconds) : 1f;
        currentPct = Mathf.Lerp(currentPct, targetPct, t);
        fill.style.width = Length.Percent(currentPct * 100f);
    }

    private void HandleStart(CombatVisualContext ctx)
    {
        var dna = side == CombatVisualSide.A ? ctx.DnaA : ctx.DnaB;
        if (nameLabel != null) nameLabel.text = dna != null ? dna.CustomName : "?";
        targetPct  = 1f;
        currentPct = 1f;
        if (fill != null) fill.style.width = Length.Percent(100f);
        SetVisible(true);
    }

    private void HandleEnd(CombatVisualSide _, bool __) { }

    private void HandleHpChanged(CombatVisualSide s, float current, float max)
    {
        if (s != side) return;
        targetPct = max > 0f ? Mathf.Clamp01(current / max) : 0f;
    }

    private void SetVisible(bool v)
    {
        if (root == null) return;
        root.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
}
