using UnityEngine;
using UnityEngine.UIElements;

namespace MoriMonchiSimulator
{
public class RadialSlot : VisualElement
{
    private float charge01;
    public float Charge01
    {
        get => charge01;
        set
        {
            float clamped = Mathf.Clamp01(value);
            if (Mathf.Abs(clamped - charge01) <= 0.005f) return;
            charge01 = clamped;
            MarkDirtyRepaint();
        }
    }

    public Color FillColor { get; set; } = Color.white;
    public Color TrackColor { get; set; } = new Color(1f, 1f, 1f, 0.10f);
    public Color ReadyColor { get; set; } = new Color(1f, 1f, 1f, 0.22f);

    public RadialSlot()
    {
        generateVisualContent += OnGenerate;
    }

    private void OnGenerate(MeshGenerationContext mgc)
    {
        var p = mgc.painter2D;
        var r = contentRect;
        if (r.width <= 0f || r.height <= 0f) return;

        Vector2 center = new Vector2(r.width * 0.5f, r.height * 0.5f);
        float radius = Mathf.Min(r.width, r.height) * 0.5f - 1f;
        if (radius <= 0f) return;

        p.BeginPath();
        p.Arc(center, radius, 0f, 360f);
        p.fillColor = TrackColor;
        p.Fill();

        if (Charge01 > 0.001f)
        {
            p.BeginPath();
            p.MoveTo(center);
            p.Arc(center, radius, -90f, -90f + Charge01 * 360f);
            p.LineTo(center);
            p.ClosePath();
            p.fillColor = Charge01 >= 0.999f ? ReadyColor : FillColor;
            p.Fill();

            if (Charge01 >= 0.999f)
            {
                p.BeginPath();
                p.Arc(center, radius, 0f, 360f);
                p.lineWidth = 2f;
                p.strokeColor = FillColor;
                p.Stroke();
            }
        }
    }

    public void Pulse()
    {
        AddToClassList("radial--fired");
        schedule.Execute(() => RemoveFromClassList("radial--fired")).StartingIn(320);
    }
}
}
