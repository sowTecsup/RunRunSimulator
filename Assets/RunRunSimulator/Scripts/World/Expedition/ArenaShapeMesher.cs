using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.ProBuilder;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.Rendering;
using UnityEngine.Splines;

namespace MoriMonchiSimulator
{

public static class ArenaShapeMesher
{
    public static List<Vector2> Sample(Spline spline, Transform space, float samplesPerMeter, bool closed)
    {
        int n = Mathf.Max(3, Mathf.CeilToInt(spline.GetLength() * samplesPerMeter));
        var result = new List<Vector2>();

        if (closed)
        {
            for (int i = 0; i < n; i++)
            {
                float t = (float)i / n;
                float3 localPosition = spline.EvaluatePosition(t);
                Vector3 worldPosition = space.TransformPoint((Vector3)localPosition);
                result.Add(new Vector2(worldPosition.x, worldPosition.z));
            }
        }
        else
        {
            for (int i = 0; i <= n; i++)
            {
                float t = (float)i / n;
                float3 localPosition = spline.EvaluatePosition(t);
                Vector3 worldPosition = space.TransformPoint((Vector3)localPosition);
                result.Add(new Vector2(worldPosition.x, worldPosition.z));
            }
        }

        return result;
    }

    public static List<Vector2> Symmetrize(IReadOnlyList<Vector2> half, Vector2 center)
    {
        var rotated = Rotate180(half, center);
        var result = new List<Vector2>(half);

        int start = 0;
        int end = rotated.Count;

        if (half.Count > 0 && rotated.Count > 0 && Vector2.Distance(rotated[0], half[half.Count - 1]) < 0.01f)
            start = 1;

        if (half.Count > 0 && rotated.Count > 0 && Vector2.Distance(rotated[rotated.Count - 1], half[0]) < 0.01f)
            end = rotated.Count - 1;

        for (int i = start; i < end; i++)
            result.Add(rotated[i]);

        return result;
    }

    public static List<Vector2> Rotate180(IReadOnlyList<Vector2> polygon, Vector2 center)
    {
        var result = new List<Vector2>(polygon.Count);
        for (int i = 0; i < polygon.Count; i++)
            result.Add(center + (center - polygon[i]));
        return result;
    }

    public static float SignedArea(IReadOnlyList<Vector2> polygon)
    {
        float area = 0f;
        int count = polygon.Count;

        for (int i = 0; i < count; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % count];
            area += a.x * b.y - b.x * a.y;
        }

        return area * 0.5f;
    }

    public static void MakeCounterClockwise(List<Vector2> polygon)
    {
        if (SignedArea(polygon) < 0f)
            polygon.Reverse();
    }

    public static void MakeClockwise(List<Vector2> polygon)
    {
        if (SignedArea(polygon) > 0f)
            polygon.Reverse();
    }

    public static bool Contains(IReadOnlyList<Vector2> polygon, Vector2 point)
    {
        bool inside = false;
        int count = polygon.Count;

        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];

            if (((pi.y > point.y) != (pj.y > point.y)) &&
                (point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x))
                inside = !inside;
        }

        return inside;
    }

    public static float DistanceToEdge(IReadOnlyList<Vector2> polygon, Vector2 point)
    {
        float minDistance = float.MaxValue;
        int count = polygon.Count;

        for (int i = 0; i < count; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % count];
            float distance = DistanceToSegment(point, a, b);
            if (distance < minDistance)
                minDistance = distance;
        }

        return minDistance;
    }

    static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float lengthSquared = ab.sqrMagnitude;

        if (lengthSquared < 1e-12f)
            return Vector2.Distance(point, a);

        float t = Mathf.Clamp01(Vector2.Dot(point - a, ab) / lengthSquared);
        Vector2 projection = a + t * ab;
        return Vector2.Distance(point, projection);
    }

    public static UnityEngine.Bounds Bounds(IReadOnlyList<Vector2> polygon)
    {
        if (polygon.Count < 3)
            return new UnityEngine.Bounds();

        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 p = polygon[i];
            if (p.x < minX) minX = p.x;
            if (p.x > maxX) maxX = p.x;
            if (p.y < minZ) minZ = p.y;
            if (p.y > maxZ) maxZ = p.y;
        }

        Vector3 center = new Vector3((minX + maxX) * 0.5f, 0f, (minZ + maxZ) * 0.5f);
        Vector3 size = new Vector3(maxX - minX, 0f, maxZ - minZ);
        return new UnityEngine.Bounds(center, size);
    }

    public static Mesh Floor(IReadOnlyList<Vector2> outline, IReadOnlyList<IReadOnlyList<Vector2>> holes, float y, float uvScale)
    {
        var outlineCopy = new List<Vector2>(outline);
        MakeCounterClockwise(outlineCopy);

        var points3 = new List<Vector3>(outlineCopy.Count);
        for (int i = 0; i < outlineCopy.Count; i++)
            points3.Add(new Vector3(outlineCopy[i].x, y, outlineCopy[i].y));

        List<IList<Vector3>> holes3 = null;
        if (holes != null && holes.Count > 0)
        {
            holes3 = new List<IList<Vector3>>(holes.Count);
            for (int i = 0; i < holes.Count; i++)
            {
                var holeCopy = new List<Vector2>(holes[i]);
                MakeClockwise(holeCopy);

                var hole3 = new List<Vector3>(holeCopy.Count);
                for (int j = 0; j < holeCopy.Count; j++)
                    hole3.Add(new Vector3(holeCopy[j].x, y, holeCopy[j].y));

                holes3.Add(hole3);
            }
        }

        var go = new GameObject("ArenaTriangulator") { hideFlags = HideFlags.HideAndDontSave };
        var pb = go.AddComponent<UnityEngine.ProBuilder.ProBuilderMesh>();

        ActionResult result = pb.CreateShapeFromPolygon(points3, 0f, false, holes3);
        if (!result)
        {
            Object.DestroyImmediate(go);
            return null;
        }

        pb.ToMesh();
        pb.Refresh();

        Mesh src = go.GetComponent<MeshFilter>().sharedMesh;
        Vector3[] srcVertices = src.vertices;
        int[] srcTriangles = src.triangles;

        var vertices = new Vector3[srcVertices.Length];
        var normals = new Vector3[srcVertices.Length];
        var uvs = new Vector2[srcVertices.Length];

        for (int i = 0; i < srcVertices.Length; i++)
        {
            vertices[i] = srcVertices[i];
            normals[i] = Vector3.up;
            uvs[i] = new Vector2(srcVertices[i].x, srcVertices[i].z) * uvScale;
        }

        var triangles = new int[srcTriangles.Length];
        System.Array.Copy(srcTriangles, triangles, srcTriangles.Length);

        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = vertices[triangles[i]];
            Vector3 b = vertices[triangles[i + 1]];
            Vector3 c = vertices[triangles[i + 2]];

            if (Vector3.Cross(b - a, c - a).y < 0f)
            {
                int temp = triangles[i + 1];
                triangles[i + 1] = triangles[i + 2];
                triangles[i + 2] = temp;
            }
        }

        var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.normals = normals;
        mesh.uv = uvs;
        mesh.RecalculateBounds();
        mesh.name = "ArenaFloor";

        Object.DestroyImmediate(go);
        return mesh;
    }

    public static Mesh Cap(IReadOnlyList<Vector2> polygon, float y, float uvScale)
    {
        Mesh mesh = Floor(polygon, null, y, uvScale);
        if (mesh != null)
            mesh.name = "ArenaCap";
        return mesh;
    }

    public static Mesh Skirt(IReadOnlyList<Vector2> loop, float top, float bottom, bool faceInterior, float uvScale)
    {
        int count = loop.Count;
        bool counterClockwise = SignedArea(loop) > 0f;

        var vertices = new List<Vector3>(count * 4);
        var normals = new List<Vector3>(count * 4);
        var uvs = new List<Vector2>(count * 4);
        var triangles = new List<int>(count * 6);

        float accumulatedLength = 0f;

        for (int i = 0; i < count; i++)
        {
            Vector2 a2 = loop[i];
            Vector2 b2 = loop[(i + 1) % count];
            Vector2 edge = b2 - a2;
            float edgeLength = edge.magnitude;

            Vector3 normal = counterClockwise
                ? new Vector3(-edge.y, 0f, edge.x).normalized
                : new Vector3(edge.y, 0f, -edge.x).normalized;

            if (!faceInterior)
                normal = -normal;

            float uStart = accumulatedLength * uvScale;
            float uEnd = (accumulatedLength + edgeLength) * uvScale;

            Vector3 aBottom = new Vector3(a2.x, bottom, a2.y);
            Vector3 bBottom = new Vector3(b2.x, bottom, b2.y);
            Vector3 bTop = new Vector3(b2.x, top, b2.y);
            Vector3 aTop = new Vector3(a2.x, top, a2.y);

            int i0 = vertices.Count;
            int i1 = i0 + 1;
            int i2 = i0 + 2;
            int i3 = i0 + 3;

            vertices.Add(aBottom);
            vertices.Add(bBottom);
            vertices.Add(bTop);
            vertices.Add(aTop);

            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);

            uvs.Add(new Vector2(uStart, 0f));
            uvs.Add(new Vector2(uEnd, 0f));
            uvs.Add(new Vector2(uEnd, 1f));
            uvs.Add(new Vector2(uStart, 1f));

            AddQuadTriangles(triangles, vertices, i0, i1, i2, i3, normal);

            accumulatedLength += edgeLength;
        }

        var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateBounds();
        mesh.name = "ArenaSkirt";

        return mesh;
    }

    static void AddQuadTriangles(List<int> triangles, List<Vector3> vertices, int i0, int i1, int i2, int i3, Vector3 normal)
    {
        Vector3 a = vertices[i0];
        Vector3 b = vertices[i1];
        Vector3 c = vertices[i2];

        if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f)
        {
            triangles.Add(i0);
            triangles.Add(i2);
            triangles.Add(i1);
            triangles.Add(i0);
            triangles.Add(i3);
            triangles.Add(i2);
        }
        else
        {
            triangles.Add(i0);
            triangles.Add(i1);
            triangles.Add(i2);
            triangles.Add(i0);
            triangles.Add(i2);
            triangles.Add(i3);
        }
    }
}
}
