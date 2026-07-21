using UnityEngine;

namespace MoriMonchiSimulator
{

public class CombatPedestalHighlighter : MonoBehaviour
{
    [SerializeField] private float activeOutlineWidth = 10f;
    [SerializeField] private Color activeOutlineColor = Color.black;
    [SerializeField] private Color activeBaseColor = new Color(1f, 0.84f, 0.35f);
    [SerializeField] private Color activeShadeColor = new Color(0.85f, 0.66f, 0.2f);

    private Renderer highlightedRenderer;
    private Material originalSharedMaterial;
    private Material instancedMaterial;

    private void OnEnable()
    {
        CombatVisualEvents.OnActiveUnit     += HandleActiveUnit;
        CombatVisualEvents.OnVisualCombatEnd += HandleVisualCombatEnd;
    }

    private void OnDisable()
    {
        CombatVisualEvents.OnActiveUnit     -= HandleActiveUnit;
        CombatVisualEvents.OnVisualCombatEnd -= HandleVisualCombatEnd;
        ClearHighlight();
    }

    private void HandleActiveUnit(CombatVisualSide side, int index)
    {
        ClearHighlight();

        var anchor = CombatVisualizerService.Instance?.AnchorOf(side, index);
        if (anchor == null) return;

        var renderer = anchor.GetComponentInChildren<Renderer>();
        if (renderer == null) return;

        originalSharedMaterial = renderer.sharedMaterial;
        instancedMaterial = renderer.material;
        instancedMaterial.SetFloat("_Outline_Width", activeOutlineWidth);
        instancedMaterial.SetColor("_Outline_Color", activeOutlineColor);
        instancedMaterial.SetColor("_BaseColor", activeBaseColor);
        instancedMaterial.SetColor("_Color", activeBaseColor);
        instancedMaterial.SetColor("_1st_ShadeColor", activeShadeColor);
        instancedMaterial.SetColor("_2nd_ShadeColor", activeShadeColor);
        highlightedRenderer = renderer;
    }

    private void HandleVisualCombatEnd(CombatVisualSide winner, bool isDraw)
    {
        ClearHighlight();
    }

    private void ClearHighlight()
    {
        if (highlightedRenderer != null)
        {
            highlightedRenderer.sharedMaterial = originalSharedMaterial;
            Object.Destroy(instancedMaterial);
        }

        highlightedRenderer = null;
        originalSharedMaterial = null;
        instancedMaterial = null;
    }
}
}
