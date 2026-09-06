using UnityEngine;
using UnityEngine.Rendering;

namespace MoriMonchiSimulator
{

public static class CueDrawer
{
    private static Material material;
    private static Material additiveMaterial;
    private static Mesh quadMesh;
    private static MaterialPropertyBlock mpb;

    private static readonly int ColorID = Shader.PropertyToID("_Color");
    private static readonly int ColorBID = Shader.PropertyToID("_ColorB");
    private static readonly int ShapeID = Shader.PropertyToID("_Shape");
    private static readonly int CenterID = Shader.PropertyToID("_Center");
    private static readonly int RadiusID = Shader.PropertyToID("_Radius");
    private static readonly int ThicknessID = Shader.PropertyToID("_Thickness");
    private static readonly int PointAID = Shader.PropertyToID("_PointA");
    private static readonly int PointBID = Shader.PropertyToID("_PointB");
    private static readonly int HeadLengthID = Shader.PropertyToID("_HeadLength");
    private static readonly int HeadWidthID = Shader.PropertyToID("_HeadWidth");
    private static readonly int DashCountID = Shader.PropertyToID("_DashCount");
    private static readonly int DashRatioID = Shader.PropertyToID("_DashRatio");
    private static readonly int RotationID = Shader.PropertyToID("_Rotation");
    private static readonly int ArcStartID = Shader.PropertyToID("_ArcStart");
    private static readonly int ArcSweepID = Shader.PropertyToID("_ArcSweep");
    private static readonly int DashLengthID = Shader.PropertyToID("_DashLength");
    private static readonly int DashGapID = Shader.PropertyToID("_DashGap");
    private static readonly int DashOffsetID = Shader.PropertyToID("_DashOffset");
    private static readonly int InnerAlphaID = Shader.PropertyToID("_InnerAlpha");
    private static readonly int OuterAlphaID = Shader.PropertyToID("_OuterAlpha");

    public static void Configure(Material material)
    {
        Configure(material, null);
    }

    public static void Configure(Material material, Material additiveMaterial)
    {
        CueDrawer.material = material;
        CueDrawer.additiveMaterial = additiveMaterial;
    }

    public static void Ring(Vector3 center, float radius, float thickness, Color color, bool additive = false)
    {
        Material mat = additive ? additiveMaterial : material;
        if (mat == null) return;
        EnsureResources();

        mpb.Clear();
        mpb.SetColor(ColorID, color);
        mpb.SetColor(ColorBID, color);
        mpb.SetFloat(InnerAlphaID, 1f);
        mpb.SetFloat(OuterAlphaID, 1f);
        mpb.SetFloat(ShapeID, 0f);
        mpb.SetVector(CenterID, center);
        mpb.SetFloat(RadiusID, radius);
        mpb.SetFloat(ThicknessID, thickness);

        float extent = radius + thickness;
        Draw(mat, center, new Vector3(2f * extent, 1f, 2f * extent));
    }

    public static void DashedRing(Vector3 center, float radius, float thickness, int dashCount, float dashRatio, float rotation, Color color, bool additive = false)
    {
        Material mat = additive ? additiveMaterial : material;
        if (mat == null) return;
        EnsureResources();

        mpb.Clear();
        mpb.SetColor(ColorID, color);
        mpb.SetColor(ColorBID, color);
        mpb.SetFloat(InnerAlphaID, 1f);
        mpb.SetFloat(OuterAlphaID, 1f);
        mpb.SetFloat(ShapeID, 4f);
        mpb.SetVector(CenterID, center);
        mpb.SetFloat(RadiusID, radius);
        mpb.SetFloat(ThicknessID, thickness);
        mpb.SetFloat(DashCountID, dashCount);
        mpb.SetFloat(DashRatioID, dashRatio);
        mpb.SetFloat(RotationID, rotation);

        float extent = radius + thickness;
        Draw(mat, center, new Vector3(2f * extent, 1f, 2f * extent));
    }

    public static void Arc(Vector3 center, float radius, float thickness, float startAngle, float sweep, Color colorA, Color colorB, bool additive = false)
    {
        Material mat = additive ? additiveMaterial : material;
        if (mat == null) return;
        EnsureResources();

        mpb.Clear();
        mpb.SetColor(ColorID, colorA);
        mpb.SetColor(ColorBID, colorB);
        mpb.SetFloat(InnerAlphaID, 1f);
        mpb.SetFloat(OuterAlphaID, 1f);
        mpb.SetFloat(ShapeID, 5f);
        mpb.SetVector(CenterID, center);
        mpb.SetFloat(RadiusID, radius);
        mpb.SetFloat(ThicknessID, thickness);
        mpb.SetFloat(ArcStartID, startAngle);
        mpb.SetFloat(ArcSweepID, sweep);

        float extent = radius + thickness;
        Draw(mat, center, new Vector3(2f * extent, 1f, 2f * extent));
    }

    public static void Sector(Vector3 center, float radius, float startAngle, float sweep, Color color, float innerAlpha, float outerAlpha, bool additive = false)
    {
        Material mat = additive ? additiveMaterial : material;
        if (mat == null) return;
        EnsureResources();

        mpb.Clear();
        mpb.SetColor(ColorID, color);
        mpb.SetColor(ColorBID, color);
        mpb.SetFloat(InnerAlphaID, innerAlpha);
        mpb.SetFloat(OuterAlphaID, outerAlpha);
        mpb.SetFloat(ShapeID, 7f);
        mpb.SetVector(CenterID, center);
        mpb.SetFloat(RadiusID, radius);
        mpb.SetFloat(ArcStartID, startAngle);
        mpb.SetFloat(ArcSweepID, sweep);

        float extent = radius;
        Draw(mat, center, new Vector3(2f * extent, 1f, 2f * extent));
    }

    public static void Disc(Vector3 center, float radius, Color color, bool additive = false)
    {
        Disc(center, radius, color, 1f, 1f, additive);
    }

    public static void Disc(Vector3 center, float radius, Color color, float innerAlpha, float outerAlpha, bool additive = false)
    {
        Material mat = additive ? additiveMaterial : material;
        if (mat == null) return;
        EnsureResources();

        mpb.Clear();
        mpb.SetColor(ColorID, color);
        mpb.SetColor(ColorBID, color);
        mpb.SetFloat(InnerAlphaID, innerAlpha);
        mpb.SetFloat(OuterAlphaID, outerAlpha);
        mpb.SetFloat(ShapeID, 1f);
        mpb.SetVector(CenterID, center);
        mpb.SetFloat(RadiusID, radius);

        float extent = radius;
        Draw(mat, center, new Vector3(2f * extent, 1f, 2f * extent));
    }

    public static void Segment(Vector3 a, Vector3 b, float thickness, Color color, bool additive = false)
    {
        Segment(a, b, thickness, color, color, additive);
    }

    public static void Segment(Vector3 a, Vector3 b, float thickness, Color colorA, Color colorB, bool additive = false)
    {
        Material mat = additive ? additiveMaterial : material;
        if (mat == null) return;
        EnsureResources();

        mpb.Clear();
        mpb.SetColor(ColorID, colorA);
        mpb.SetColor(ColorBID, colorB);
        mpb.SetFloat(InnerAlphaID, 1f);
        mpb.SetFloat(OuterAlphaID, 1f);
        mpb.SetFloat(ShapeID, 2f);
        mpb.SetVector(PointAID, a);
        mpb.SetVector(PointBID, b);
        mpb.SetFloat(ThicknessID, thickness);

        Vector3 mid = new Vector3((a.x + b.x) * 0.5f, a.y, (a.z + b.z) * 0.5f);
        float pad = thickness;
        Vector3 scale = new Vector3(Mathf.Abs(b.x - a.x) + pad, 1f, Mathf.Abs(b.z - a.z) + pad);
        Draw(mat, mid, scale);
    }

    public static void DashedSegment(Vector3 a, Vector3 b, float thickness, float dashLength, float dashGap, float dashOffset, Color colorA, Color colorB, bool additive = false)
    {
        Material mat = additive ? additiveMaterial : material;
        if (mat == null) return;
        EnsureResources();

        mpb.Clear();
        mpb.SetColor(ColorID, colorA);
        mpb.SetColor(ColorBID, colorB);
        mpb.SetFloat(InnerAlphaID, 1f);
        mpb.SetFloat(OuterAlphaID, 1f);
        mpb.SetFloat(ShapeID, 6f);
        mpb.SetVector(PointAID, a);
        mpb.SetVector(PointBID, b);
        mpb.SetFloat(ThicknessID, thickness);
        mpb.SetFloat(DashLengthID, dashLength);
        mpb.SetFloat(DashGapID, dashGap);
        mpb.SetFloat(DashOffsetID, dashOffset);

        Vector3 mid = new Vector3((a.x + b.x) * 0.5f, a.y, (a.z + b.z) * 0.5f);
        float pad = thickness;
        Vector3 scale = new Vector3(Mathf.Abs(b.x - a.x) + pad, 1f, Mathf.Abs(b.z - a.z) + pad);
        Draw(mat, mid, scale);
    }

    public static void Arrow(Vector3 a, Vector3 b, float thickness, float headLength, float headWidth, Color color, bool additive = false)
    {
        Arrow(a, b, thickness, headLength, headWidth, color, color, additive);
    }

    public static void Arrow(Vector3 a, Vector3 b, float thickness, float headLength, float headWidth, Color colorA, Color colorB, bool additive = false)
    {
        Material mat = additive ? additiveMaterial : material;
        if (mat == null) return;
        EnsureResources();

        mpb.Clear();
        mpb.SetColor(ColorID, colorA);
        mpb.SetColor(ColorBID, colorB);
        mpb.SetFloat(InnerAlphaID, 1f);
        mpb.SetFloat(OuterAlphaID, 1f);
        mpb.SetFloat(ShapeID, 3f);
        mpb.SetVector(PointAID, a);
        mpb.SetVector(PointBID, b);
        mpb.SetFloat(ThicknessID, thickness);
        mpb.SetFloat(HeadLengthID, headLength);
        mpb.SetFloat(HeadWidthID, headWidth);

        Vector3 mid = new Vector3((a.x + b.x) * 0.5f, a.y, (a.z + b.z) * 0.5f);
        float pad = thickness + headWidth;
        Vector3 scale = new Vector3(Mathf.Abs(b.x - a.x) + pad, 1f, Mathf.Abs(b.z - a.z) + pad);
        Draw(mat, mid, scale);
    }

    private static void Draw(Material mat, Vector3 center, Vector3 scale)
    {
        Matrix4x4 matrix = Matrix4x4.TRS(center, Quaternion.identity, scale);
        var renderParams = new RenderParams(mat)
        {
            matProps = mpb,
            worldBounds = new Bounds(center, scale),
            shadowCastingMode = ShadowCastingMode.Off,
            receiveShadows = false
        };
        Graphics.RenderMesh(renderParams, quadMesh, 0, matrix);
    }

    private static void EnsureResources()
    {
        if (mpb == null) mpb = new MaterialPropertyBlock();
        if (quadMesh == null) quadMesh = BuildQuadMesh();
    }

    private static Mesh BuildQuadMesh()
    {
        var mesh = new Mesh { name = "MonchiCueQuad", hideFlags = HideFlags.HideAndDontSave };

        var vertices = new Vector3[]
        {
            new Vector3(-0.5f, 0f, -0.5f),
            new Vector3(-0.5f, 0f, 0.5f),
            new Vector3(0.5f, 0f, 0.5f),
            new Vector3(0.5f, 0f, -0.5f)
        };

        var normals = new Vector3[]
        {
            Vector3.up, Vector3.up, Vector3.up, Vector3.up
        };

        var uvs = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(0f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 0f)
        };

        var triangles = new int[] { 0, 1, 2, 0, 2, 3 };

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.triangles = triangles;

        return mesh;
    }
}
}
