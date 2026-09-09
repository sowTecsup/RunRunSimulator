using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace MoriMonchiSimulator
{

public static class CueRibbonDrawer
{
    private const float Pad = 0.04f;
    private const float MinLength = 0.05f;

    private static Material material;
    private static Material additiveMaterial;

    private static readonly List<Mesh> meshPool = new List<Mesh>();
    private static int meshIndex;
    private static int lastFrame = -1;

    private static readonly List<Vector3> samplePoints = new List<Vector3>();
    private static readonly List<float> sampleArcLengths = new List<float>();

    private static readonly List<Vector3> profilePositions = new List<Vector3>();
    private static readonly List<float> profileArcLengths = new List<float>();
    private static readonly List<float> profileHalfWidths = new List<float>();
    private static readonly List<Vector3> profileTangents = new List<Vector3>();

    private static readonly List<Vector3> vertices = new List<Vector3>();
    private static readonly List<Vector2> uv0List = new List<Vector2>();
    private static readonly List<Vector2> uv1List = new List<Vector2>();
    private static readonly List<Vector2> uv2List = new List<Vector2>();
    private static readonly List<Vector2> uv3List = new List<Vector2>();
    private static readonly List<Color> colors = new List<Color>();
    private static readonly List<int> triangles = new List<int>();

    private static MaterialPropertyBlock mpb;

    private static readonly int DashLengthID = Shader.PropertyToID("_DashLength");
    private static readonly int DashGapID = Shader.PropertyToID("_DashGap");
    private static readonly int DashOffsetID = Shader.PropertyToID("_DashOffset");

    public static void Configure(Material material, Material additiveMaterial)
    {
        CueRibbonDrawer.material = material;
        CueRibbonDrawer.additiveMaterial = additiveMaterial;
    }

    public static void Arc(Vector3 from, Vector3 to, float apexHeight, float width, float tailScale, float headWidth, float headLength, int samples, float dashLength, float dashGap, float dashOffset, Color tailColor, Color headColor, Vector3 eye, bool additive = false)
    {
        Material mat = additive ? additiveMaterial : material;
        if (mat == null) return;
        samples = Mathf.Max(4, samples);

        samplePoints.Clear();
        sampleArcLengths.Clear();

        float accumulated = 0f;
        Vector3 previous = from;
        for (int i = 0; i <= samples; i++)
        {
            float t = i / (float)samples;
            Vector3 p = Vector3.Lerp(from, to, t) + Vector3.up * (4f * apexHeight * t * (1f - t));
            if (i > 0) accumulated += Vector3.Distance(previous, p);
            samplePoints.Add(p);
            sampleArcLengths.Add(accumulated);
            previous = p;
        }

        float totalLength = sampleArcLengths[sampleArcLengths.Count - 1];
        if (totalLength < MinLength) return;

        float bodyHalf = width * 0.5f;
        float tailHalf = bodyHalf * Mathf.Clamp01(tailScale);
        float headHalf = headWidth * 0.5f;
        float tipRadius = bodyHalf;
        float headBase = Mathf.Max(0f, totalLength - headLength);

        BuildProfile(totalLength, headBase, tailHalf, bodyHalf, headHalf, tipRadius);

        int entryCount = profilePositions.Count;
        if (entryCount < 2) return;

        vertices.Clear();
        uv0List.Clear();
        uv1List.Clear();
        uv2List.Clear();
        uv3List.Clear();
        colors.Clear();
        triangles.Clear();

        Vector3 min = new Vector3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
        Vector3 max = new Vector3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);

        for (int k = 0; k < entryCount; k++)
        {
            Vector3 p = profilePositions[k];
            float s = profileArcLengths[k];
            float halfWidth = profileHalfWidths[k];
            Vector3 tangent = profileTangents[k];

            Vector3 view = (eye - p).normalized;
            Vector3 side = Vector3.Cross(tangent, view).normalized;
            if (side.sqrMagnitude < 1e-6f)
            {
                side = Vector3.Cross(tangent, Vector3.up).normalized;
                if (side.sqrMagnitude < 1e-6f) side = Vector3.right;
            }

            float meshHalf = halfWidth + Pad;
            Vector3 left = p - side * meshHalf;
            Vector3 right = p + side * meshHalf;
            Color color = Color.Lerp(tailColor, headColor, s / totalLength);

            vertices.Add(left);
            uv0List.Add(new Vector2(s, -1f));
            uv1List.Add(new Vector2(halfWidth, meshHalf));
            uv2List.Add(new Vector2(tailHalf, tipRadius));
            uv3List.Add(new Vector2(totalLength, headBase));
            colors.Add(color);

            vertices.Add(right);
            uv0List.Add(new Vector2(s, 1f));
            uv1List.Add(new Vector2(halfWidth, meshHalf));
            uv2List.Add(new Vector2(tailHalf, tipRadius));
            uv3List.Add(new Vector2(totalLength, headBase));
            colors.Add(color);

            min = Vector3.Min(min, Vector3.Min(left, right));
            max = Vector3.Max(max, Vector3.Max(left, right));
        }

        for (int k = 0; k < entryCount - 1; k++)
        {
            int baseIndex = 2 * k;
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 1);
            triangles.Add(baseIndex + 3);
            triangles.Add(baseIndex + 2);
        }

        Mesh mesh = GetPooledMesh();
        mesh.Clear(false);
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uv0List);
        mesh.SetUVs(1, uv1List);
        mesh.SetUVs(2, uv2List);
        mesh.SetUVs(3, uv3List);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);

        Bounds bounds = new Bounds();
        bounds.SetMinMax(min, max);
        bounds.Expand((headHalf + Pad) * 2f);

        if (mpb == null) mpb = new MaterialPropertyBlock();
        mpb.Clear();
        mpb.SetFloat(DashLengthID, dashLength);
        mpb.SetFloat(DashGapID, dashGap);
        mpb.SetFloat(DashOffsetID, dashOffset);

        var renderParams = new RenderParams(mat)
        {
            worldBounds = bounds,
            shadowCastingMode = ShadowCastingMode.Off,
            receiveShadows = false,
            matProps = mpb
        };
        Graphics.RenderMesh(renderParams, mesh, 0, Matrix4x4.identity);
    }

    private static void BuildProfile(float totalLength, float headBase, float tailHalf, float bodyHalf, float headHalf, float tipRadius)
    {
        profilePositions.Clear();
        profileArcLengths.Clear();
        profileHalfWidths.Clear();
        profileTangents.Clear();

        int count = samplePoints.Count;
        bool stepInserted = false;

        for (int i = 0; i < count; i++)
        {
            float s = sampleArcLengths[i];

            if (!stepInserted && s >= headBase)
            {
                InsertHeadBaseStep(i, headBase, bodyHalf, headHalf);
                stepInserted = true;
            }

            if (s < headBase)
            {
                float halfWidth = Mathf.Lerp(tailHalf, bodyHalf, headBase > 0.0001f ? s / headBase : 1f);
                AddProfileEntry(samplePoints[i], s, halfWidth, ComputeTangent(i));
            }
            else if (s > headBase)
            {
                float u = (s - headBase) / Mathf.Max(totalLength - headBase, 0.001f);
                float halfWidth = Mathf.Lerp(headHalf, tipRadius, u);
                AddProfileEntry(samplePoints[i], s, halfWidth, ComputeTangent(i));
            }
        }
    }

    private static void InsertHeadBaseStep(int index, float headBase, float bodyHalf, float headHalf)
    {
        Vector3 position;
        Vector3 tangent;
        float s = sampleArcLengths[index];

        if (Mathf.Approximately(s, headBase))
        {
            position = samplePoints[index];
            tangent = ComputeTangent(index);
        }
        else
        {
            int prev = index - 1;
            float segment = sampleArcLengths[index] - sampleArcLengths[prev];
            float f = segment > 0.0001f ? (headBase - sampleArcLengths[prev]) / segment : 0f;
            position = Vector3.Lerp(samplePoints[prev], samplePoints[index], f);
            tangent = (samplePoints[index] - samplePoints[prev]).normalized;
        }

        AddProfileEntry(position, headBase, bodyHalf, tangent);
        AddProfileEntry(position, headBase, headHalf, tangent);
    }

    private static Vector3 ComputeTangent(int index)
    {
        int n = samplePoints.Count;
        if (n < 2) return Vector3.forward;
        if (index == 0) return (samplePoints[1] - samplePoints[0]).normalized;
        if (index == n - 1) return (samplePoints[n - 1] - samplePoints[n - 2]).normalized;
        return (samplePoints[index + 1] - samplePoints[index - 1]).normalized;
    }

    private static void AddProfileEntry(Vector3 position, float s, float halfWidth, Vector3 tangent)
    {
        profilePositions.Add(position);
        profileArcLengths.Add(s);
        profileHalfWidths.Add(halfWidth);
        profileTangents.Add(tangent);
    }

    private static Mesh GetPooledMesh()
    {
        if (Time.frameCount != lastFrame)
        {
            lastFrame = Time.frameCount;
            meshIndex = 0;
        }

        Mesh mesh;
        if (meshIndex < meshPool.Count)
        {
            mesh = meshPool[meshIndex];
        }
        else
        {
            mesh = new Mesh { name = "MonchiRibbon", hideFlags = HideFlags.HideAndDontSave };
            mesh.MarkDynamic();
            meshPool.Add(mesh);
        }

        meshIndex++;
        return mesh;
    }
}
}
