using UnityEngine;
using UnityEngine.UIElements;
using MoriMonchiSimulator.DragonRps;

namespace MoriMonchiSimulator
{
public class RpsTriangleElement : VisualElement
{
    private const float NodeRadius = 14f;
    private const float LabelHeight = 18f;
    private const float Pad = 6f;

    private static readonly CustomStyleProperty<Color> HornsProp = new CustomStyleProperty<Color>("--tri-horns");
    private static readonly CustomStyleProperty<Color> WingsProp = new CustomStyleProperty<Color>("--tri-wings");
    private static readonly CustomStyleProperty<Color> BackProp = new CustomStyleProperty<Color>("--tri-back");
    private static readonly CustomStyleProperty<Color> InkProp = new CustomStyleProperty<Color>("--tri-ink");
    private static readonly CustomStyleProperty<Color> HiProp = new CustomStyleProperty<Color>("--tri-hi");
    private static readonly CustomStyleProperty<Color> FillProp = new CustomStyleProperty<Color>("--tri-fill");

    private Color horns = new Color(0.2f, 0.75f, 0.65f);
    private Color wings = new Color(0.76f, 0.45f, 0.83f);
    private Color back = new Color(0.96f, 0.73f, 0.28f);
    private Color ink = new Color(0.72f, 0.6f, 0.51f);
    private Color hi = new Color(1f, 0.49f, 0.35f);
    private Color fill = new Color(0.16f, 0.12f, 0.11f);

    private readonly Label[] labels = new Label[3];
    private int highlight = -1;
    private float pulse;
    private IVisualElementScheduledItem pulseItem;

    public RpsTriangleElement()
    {
        AddToClassList("rps-triangle");
        pickingMode = PickingMode.Ignore;

        for (int i = 0; i < 3; i++)
        {
            Label label = new Label();
            label.AddToClassList("rps-triangle__label");
            label.pickingMode = PickingMode.Ignore;
            label.style.position = Position.Absolute;
            labels[i] = label;
            Add(label);
        }

        RegisterCallback<CustomStyleResolvedEvent>(OnStyleResolved);
        RegisterCallback<GeometryChangedEvent>(_ => LayoutLabels());
        generateVisualContent += OnGenerate;
    }

    public int Highlight
    {
        get => highlight;
        set
        {
            if (highlight == value) return;
            highlight = value;
            if (highlight >= 0)
            {
                pulseItem ??= schedule.Execute(Tick).Every(33);
                pulseItem.Resume();
            }
            else
            {
                pulseItem?.Pause();
                pulse = 0f;
            }
            MarkDirtyRepaint();
        }
    }

    public void SetLabels(string hornsText, string wingsText, string backText)
    {
        labels[0].text = hornsText;
        labels[1].text = wingsText;
        labels[2].text = backText;
    }

    private void Tick()
    {
        pulse = 0.5f + 0.5f * Mathf.Sin(Time.realtimeSinceStartup * 4f);
        MarkDirtyRepaint();
    }

    private void OnStyleResolved(CustomStyleResolvedEvent e)
    {
        if (e.customStyle.TryGetValue(HornsProp, out Color hornsColor)) horns = hornsColor;
        if (e.customStyle.TryGetValue(WingsProp, out Color wingsColor)) wings = wingsColor;
        if (e.customStyle.TryGetValue(BackProp, out Color backColor)) back = backColor;
        if (e.customStyle.TryGetValue(InkProp, out Color inkColor)) ink = inkColor;
        if (e.customStyle.TryGetValue(HiProp, out Color hiColor)) hi = hiColor;
        if (e.customStyle.TryGetValue(FillProp, out Color fillColor)) fill = fillColor;
        MarkDirtyRepaint();
    }

    private Color TypeColor(int t)
    {
        if (t == 0) return horns;
        if (t == 1) return wings;
        return back;
    }

    private Vector2[] ComputeNodes()
    {
        float w = contentRect.width;
        float h = contentRect.height;
        Vector2[] nodes = new Vector2[3];
        nodes[0] = new Vector2(w * 0.5f, LabelHeight + NodeRadius + Pad);
        nodes[1] = new Vector2(w - NodeRadius - Pad - 20f, h - LabelHeight - NodeRadius - Pad);
        nodes[2] = new Vector2(NodeRadius + Pad + 20f, h - LabelHeight - NodeRadius - Pad);
        return nodes;
    }

    private void LayoutLabels()
    {
        Vector2[] nodes = ComputeNodes();
        for (int t = 0; t < 3; t++)
        {
            labels[t].style.width = 74f;
            labels[t].style.left = nodes[t].x - 37f;
            labels[t].style.top = t == 0 ? nodes[0].y - NodeRadius - LabelHeight - 2f : nodes[t].y + NodeRadius + 2f;
        }
    }

    private void OnGenerate(MeshGenerationContext mgc)
    {
        Vector2[] nodes = ComputeNodes();
        Painter2D p = mgc.painter2D;
        p.lineCap = LineCap.Round;
        p.lineJoin = LineJoin.Round;

        for (int from = 0; from < 3; from++)
        {
            int to = -1;
            for (int t = 0; t < 3; t++)
            {
                if (DragonRpsRules.Beats((DragonAction)from, (DragonAction)t))
                {
                    to = t;
                    break;
                }
            }

            Vector2 a = nodes[from];
            Vector2 b = nodes[to];
            bool hot = highlight == from;
            Color color = hot ? hi : ink;
            p.strokeColor = color;
            p.lineWidth = hot ? 4f : 2.5f;

            Vector2 dir = (b - a).normalized;
            Vector2 start = a + dir * (NodeRadius + 4f);
            Vector2 end = b - dir * (NodeRadius + 4f);

            p.BeginPath();
            p.MoveTo(start);
            p.LineTo(end);
            p.Stroke();

            float head = hot ? 11f : 9f;
            Vector2 n = new Vector2(-dir.y, dir.x);
            Vector2 baseC = end - dir * head;
            p.fillColor = color;
            p.BeginPath();
            p.MoveTo(end);
            p.LineTo(baseC + n * head * 0.55f);
            p.LineTo(baseC - n * head * 0.55f);
            p.ClosePath();
            p.Fill();
        }

        for (int t = 0; t < 3; t++)
        {
            Color typeColor = TypeColor(t);
            p.fillColor = fill;
            p.strokeColor = typeColor;
            p.lineWidth = 3f;
            p.BeginPath();
            p.Arc(nodes[t], NodeRadius, Angle.Degrees(0f), Angle.Degrees(360f));
            p.ClosePath();
            p.Fill();
            p.Stroke();

            if (highlight == t)
            {
                p.strokeColor = hi;
                p.lineWidth = 2.5f;
                float radius = NodeRadius + 4f + pulse * 3f;
                p.BeginPath();
                p.Arc(nodes[t], radius, Angle.Degrees(0f), Angle.Degrees(360f));
                p.Stroke();
            }

            if (highlight >= 0 && DragonRpsRules.Beats((DragonAction)highlight, (DragonAction)t))
            {
                p.fillColor = typeColor;
                p.BeginPath();
                p.Arc(nodes[t], NodeRadius * 0.45f, Angle.Degrees(0f), Angle.Degrees(360f));
                p.ClosePath();
                p.Fill();
            }
        }
    }
}
}
