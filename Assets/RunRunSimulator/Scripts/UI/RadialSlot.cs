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
            EnableInClassList("radial--ready", charge01 >= 0.999f);
            MarkDirtyRepaint();
        }
    }

    public Color FillColor { get; set; } = Color.white;
    public Color TrackColor { get; set; } = new Color(1f, 1f, 1f, 0.10f);
    public Color ReadyColor { get; set; } = new Color(1f, 1f, 1f, 0.22f);

    private bool armed;
    public bool Armed
    {
        get => armed;
        set
        {
            if (armed == value) return;
            armed = value;
            EnableInClassList("radial--armed", armed);
            MarkDirtyRepaint();
        }
    }

    public RadialSlot()
    {
        pickingMode = PickingMode.Position;
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
            p.fillColor = (Charge01 >= 0.999f || Armed) ? ReadyColor : FillColor;
            p.Fill();

            if (Charge01 >= 0.999f)
            {
                p.BeginPath();
                p.Arc(center, radius, 0f, 360f);
                p.lineWidth = 2f;
                p.strokeColor = FillColor;
                p.Stroke();
            }

            if (Armed)
            {
                p.BeginPath();
                p.Arc(center, radius - 4f, 0f, 360f);
                p.lineWidth = 1.5f;
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
