using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

public static class ArenaShapeGizmos
{
    public static void Draw(ArenaShape shape)
    {
        if (shape.OutlinePolygon.Count == 0) return;

        float y = shape.Center.y + 0.05f;

        Gizmos.color = Color.yellow;
        DrawPolygon(shape.OutlinePolygon, y);

        Gizmos.color = Color.gray;
        foreach (var poly in shape.RockPolygons) DrawPolygon(poly, y);

        Gizmos.color = Color.cyan;
        foreach (var poly in shape.LakePolygons) DrawPolygon(poly, y);

        Gizmos.color = Color.black;
        foreach (var poly in shape.PitPolygons) DrawPolygon(poly, y);

        Gizmos.color = Color.green;
        foreach (var poly in shape.GrovePolygons)
        {
            DrawPolygon(poly, y);
            if (shape.Symmetric) DrawPolygon(ArenaShapeMesher.Rotate180(poly, new Vector2(shape.Center.x, shape.Center.z)), y);
        }

        for (int pair = 0; pair < shape.EntryPairCount; pair++)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(shape.EntryPoint(pair, ExpeditionTeam.Player), 0.6f);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(shape.EntryPoint(pair, ExpeditionTeam.Rival), 0.6f);
        }
    }

    private static void DrawPolygon(IReadOnlyList<Vector2> polygon, float y)
    {
        if (polygon == null || polygon.Count < 2) return;

        for (int i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Count];
            Gizmos.DrawLine(new Vector3(a.x, y, a.y), new Vector3(b.x, y, b.y));
        }
    }
}
}
