using System.Collections.Generic;
using UnityEngine;

namespace MoriMonchiSimulator
{

public static class ArenaGrassBlades
{
    public static Mesh Build(IReadOnlyList<Vector3> roots, System.Random rng, Vector2 heightRange, Vector2 widthRange, float tilt, Vector3 origin)
    {
        if (roots == null || roots.Count == 0) return null;

        var vertices = new List<Vector3>(roots.Count * 3);
        var normals = new List<Vector3>(roots.Count * 3);
        var uvs = new List<Vector2>(roots.Count * 3);
        var colors = new List<Color>(roots.Count * 3);
        var triangles = new List<int>(roots.Count * 3);

        for (int i = 0; i < roots.Count; i++)
        {
            float height = (float)(rng.NextDouble() * (heightRange.y - heightRange.x) + heightRange.x);
            float width = (float)(rng.NextDouble() * (widthRange.y - widthRange.x) + widthRange.x);
            float yaw = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            float side = Mathf.Cos(yaw);
            float forward = Mathf.Sin(yaw);
            var sideAxis = new Vector3(-forward, 0f, side);

            float leanAngle = (float)(rng.NextDouble() * tilt);
            float leanDirection = (float)(rng.NextDouble() * Mathf.PI * 2.0);
            var leanAxis = new Vector3(Mathf.Cos(leanDirection), 0f, Mathf.Sin(leanDirection));

            var root = roots[i] - origin;

            var tipOffset = leanAxis * (Mathf.Sin(leanAngle) * height);

            var rootLeft = root + sideAxis * (width * 0.5f);
            var rootRight = root - sideAxis * (width * 0.5f);
            var tip = root + Vector3.up * height + tipOffset;

            int baseIndex = vertices.Count;
            vertices.Add(rootLeft);
            vertices.Add(rootRight);
            vertices.Add(tip);

            var normal = (Vector3.up + sideAxis * 0.25f).normalized;
            normals.Add(normal);
            normals.Add(normal);
            normals.Add(normal);

            uvs.Add(new Vector2(0f, 0f));
            uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(0.5f, 1f));

            float hue = (float)rng.NextDouble();
            var bladeColor = new Color(hue, hue, hue, 1f);
            colors.Add(bladeColor);
            colors.Add(bladeColor);
            colors.Add(bladeColor);

            triangles.Add(baseIndex + 0);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 1);
        }

        var mesh = new Mesh
        {
            name = "ArenaGrassBlades",
            hideFlags = HideFlags.DontSave
        };

        mesh.indexFormat = vertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateBounds();

        return mesh;
    }
}
}
