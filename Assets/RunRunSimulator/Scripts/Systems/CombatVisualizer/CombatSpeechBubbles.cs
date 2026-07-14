using UnityEngine;
using UnityEngine.UIElements;
namespace MoriMonchiSimulator
{

public class CombatSpeechBubbles : MonoBehaviour
{
    [SerializeField] private UIDocument document;
    [SerializeField] private Vector3    speakerOffset = new Vector3(0f, 1.6f, 0f);
    [SerializeField] private Vector3    targetOffset  = new Vector3(0f, 1.9f, 0f);

    private static readonly Color DefaultBubbleBorder = new Color32(30, 30, 30, 255);
    private static readonly Color DefaultArrowColor    = new Color32(255, 210, 74, 255);

    private VisualElement root;
    private VisualElement bubble;
    private Label         bubbleLabel;
    private Label         bubbleTail;
    private Label         targetArrow;

    private CombatSpeechData activeData;
    private bool             hasActive;
    private float            activeUntil;

    private void OnEnable()
    {
        CombatVisualEvents.OnSpeech            += HandleSpeech;
        CombatVisualEvents.OnVisualCombatStart += HandleStart;
        CombatVisualEvents.OnVisualCombatEnd   += HandleEnd;
    }

    private void OnDisable()
    {
        CombatVisualEvents.OnSpeech            -= HandleSpeech;
        CombatVisualEvents.OnVisualCombatStart -= HandleStart;
        CombatVisualEvents.OnVisualCombatEnd   -= HandleEnd;
    }

    private bool EnsureRefs()
    {
        if (document == null) return false;
        var currentRoot = document.rootVisualElement;
        if (currentRoot == null) return false;

        if (currentRoot != root)
        {
            root        = currentRoot;
            bubble      = null;
            bubbleLabel = null;
            bubbleTail  = null;
            targetArrow = null;
        }

        if (bubble == null) BuildBubble();
        if (bubbleTail == null) BuildTail();
        if (targetArrow == null) BuildArrow();

        return true;
    }

    private void BuildBubble()
    {
        bubble = new VisualElement { pickingMode = PickingMode.Ignore };
        bubble.style.position        = Position.Absolute;
        bubble.style.backgroundColor = (Color)new Color32(250, 250, 250, 255);
        bubble.style.paddingTop      = 4;
        bubble.style.paddingBottom   = 4;
        bubble.style.paddingLeft     = 8;
        bubble.style.paddingRight    = 8;
        bubble.style.maxWidth        = 220;
        bubble.style.borderTopWidth    = 2;
        bubble.style.borderRightWidth  = 2;
        bubble.style.borderBottomWidth = 2;
        bubble.style.borderLeftWidth   = 2;
        bubble.style.borderTopLeftRadius     = 8;
        bubble.style.borderTopRightRadius    = 8;
        bubble.style.borderBottomLeftRadius  = 8;
        bubble.style.borderBottomRightRadius = 8;
        SetBorderColor(bubble, DefaultBubbleBorder);
        bubble.style.display = DisplayStyle.None;

        bubbleLabel = new Label();
        bubbleLabel.style.fontSize          = 13;
        bubbleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        bubbleLabel.style.color             = (Color)new Color32(20, 20, 20, 255);
        bubbleLabel.style.whiteSpace        = WhiteSpace.Normal;
        bubbleLabel.style.unityTextAlign    = TextAnchor.MiddleCenter;
        bubble.Add(bubbleLabel);

        root.Add(bubble);
    }

    private void BuildTail()
    {
        bubbleTail = new Label("▼") { pickingMode = PickingMode.Ignore };
        bubbleTail.style.position = Position.Absolute;
        bubbleTail.style.fontSize = 14;
        bubbleTail.style.color    = (Color)new Color32(250, 250, 250, 255);
        bubbleTail.style.display  = DisplayStyle.None;
        root.Add(bubbleTail);
    }

    private void BuildArrow()
    {
        targetArrow = new Label("▼") { pickingMode = PickingMode.Ignore };
        targetArrow.style.position = Position.Absolute;
        targetArrow.style.fontSize = 22;
        targetArrow.style.unityFontStyleAndWeight = FontStyle.Bold;
        targetArrow.style.color    = DefaultArrowColor;
        targetArrow.style.display  = DisplayStyle.None;
        root.Add(targetArrow);
    }

    private static void SetBorderColor(VisualElement el, Color c)
    {
        el.style.borderTopColor    = c;
        el.style.borderRightColor  = c;
        el.style.borderBottomColor = c;
        el.style.borderLeftColor   = c;
    }

    private void HandleStart(CombatVisualContext ctx)
    {
        EnsureRefs();
        HideAll();
    }

    private void HandleEnd(CombatVisualSide winner, bool isDraw)
    {
        HideAll();
    }

    private void HandleSpeech(CombatSpeechData d)
    {
        if (!EnsureRefs()) return;

        activeData  = d;
        activeUntil = Time.time + Mathf.Max(0.5f, d.Duration);
        hasActive   = true;

        bubbleLabel.text = d.Text;

        var bubbleColor = d.HasColor ? d.Color : DefaultBubbleBorder;
        SetBorderColor(bubble, bubbleColor);
        var arrowColor = d.HasColor ? d.Color : DefaultArrowColor;
        targetArrow.style.color = arrowColor;

        bubble.style.display     = DisplayStyle.Flex;
        bubbleTail.style.display = DisplayStyle.Flex;
        targetArrow.style.display = d.HasTarget && d.TargetFollow != null ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void HideAll()
    {
        hasActive = false;
        if (bubble != null) bubble.style.display = DisplayStyle.None;
        if (bubbleTail != null) bubbleTail.style.display = DisplayStyle.None;
        if (targetArrow != null) targetArrow.style.display = DisplayStyle.None;
    }

    private void Update()
    {
        if (!hasActive) return;

        if (Time.time >= activeUntil)
        {
            HideAll();
            return;
        }

        if (activeData.Follow == null)
        {
            HideAll();
            return;
        }

        if (!EnsureRefs()) return;
        if (Camera.main == null) return;

        var speakerPanelPos = RuntimePanelUtils.CameraTransformWorldToPanel(
            root.panel, activeData.Follow.position + speakerOffset, Camera.main);

        float w = float.IsNaN(bubble.resolvedStyle.width) ? 0f : bubble.resolvedStyle.width;
        float h = float.IsNaN(bubble.resolvedStyle.height) ? 0f : bubble.resolvedStyle.height;

        bubble.style.left = speakerPanelPos.x - w / 2f;
        bubble.style.top  = speakerPanelPos.y - h - 10f;

        bubbleTail.style.left = speakerPanelPos.x - 7f;
        bubbleTail.style.top  = speakerPanelPos.y - 12f;

        if (activeData.HasTarget && activeData.TargetFollow != null)
        {
            var targetPanelPos = RuntimePanelUtils.CameraTransformWorldToPanel(
                root.panel, activeData.TargetFollow.position + targetOffset, Camera.main);

            targetArrow.style.display = DisplayStyle.Flex;
            targetArrow.style.left = targetPanelPos.x - 11f;
            targetArrow.style.top  = targetPanelPos.y - 24f;
        }
        else
        {
            targetArrow.style.display = DisplayStyle.None;
        }
    }
}
}
