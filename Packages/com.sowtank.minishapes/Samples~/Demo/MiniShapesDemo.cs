using System.Collections.Generic;
using UnityEngine;

namespace Sowtank.MiniShapes.Samples
{

public class MiniShapesDemo : MonoBehaviour
{
    public float spacing = 3f;
    public Color colorA = Color.cyan;
    public Color colorB = Color.magenta;

    private readonly PathStyle pathStyle = new PathStyle();
    private readonly PathState pathState = new PathState();
    private readonly List<Vector3> pathPoints = new();

    private void LateUpdate()
    {
        Vector3 origin = transform.position;
        int index = 0;

        DrawRing(origin, index++);
        DrawDashedRing(origin, index++);
        DrawArc(origin, index++);
        DrawDashedArc(origin, index++);
        DrawSector(origin, index++);
        DrawDisc(origin, index++);
        DrawSegment(origin, index++);
        DrawCapsule(origin, index++);
        DrawCapsuleOutline(origin, index++);
        DrawDashedSegment(origin, index++);
        DrawArrow(origin, index++);
        DrawPath(origin);
    }

    private Vector3 Slot(Vector3 origin, int index)
    {
        return origin + new Vector3(spacing * index, 0f, 0f);
    }

    private void DrawRing(Vector3 origin, int index)
    {
        ShapeDraw.Ring(Slot(origin, index), 1f, 0.1f, colorA);
    }

    private void DrawDashedRing(Vector3 origin, int index)
    {
        float rotation = Time.time * 45f;
        ShapeDraw.DashedRing(Slot(origin, index), 1f, 0.1f, 12, 0.6f, rotation, colorA);
    }

    private void DrawArc(Vector3 origin, int index)
    {
        ShapeDraw.Arc(Slot(origin, index), 1f, 0.1f, 0f, 270f, colorA, colorB);
    }

    private void DrawDashedArc(Vector3 origin, int index)
    {
        ShapeDraw.DashedArc(Slot(origin, index), 1f, 0.1f, 0f, 270f, 8, 0.6f, 0f, colorA, colorB);
    }

    private void DrawSector(Vector3 origin, int index)
    {
        ShapeDraw.Sector(Slot(origin, index), 1.2f, -30f, 60f, colorA, 0.35f, 0f);
    }

    private void DrawDisc(Vector3 origin, int index)
    {
        ShapeDraw.Disc(Slot(origin, index), 1f, colorA, 1f, 0f);
    }

    private void DrawSegment(Vector3 origin, int index)
    {
        Vector3 center = Slot(origin, index);
        ShapeDraw.Segment(center + Vector3.left * 0.8f, center + Vector3.right * 0.8f, 0.15f, colorA, colorB);
    }

    private void DrawCapsule(Vector3 origin, int index)
    {
        Vector3 center = Slot(origin, index);
        ShapeDraw.Capsule(center + Vector3.left * 0.6f, center + Vector3.right * 0.6f, 0.3f, colorA, 1f, 1f);
    }

    private void DrawCapsuleOutline(Vector3 origin, int index)
    {
        Vector3 center = Slot(origin, index);
        ShapeDraw.CapsuleOutline(center + Vector3.left * 0.6f, center + Vector3.right * 0.6f, 0.3f, 0.08f, colorA, colorB);
    }

    private void DrawDashedSegment(Vector3 origin, int index)
    {
        Vector3 center = Slot(origin, index);
        float offset = Time.time * 1.5f;
        ShapeDraw.DashedSegment(center + Vector3.left * 0.8f, center + Vector3.right * 0.8f, 0.12f, 0.3f, 0.2f, offset, colorA, colorB);
    }

    private void DrawArrow(Vector3 origin, int index)
    {
        Vector3 center = Slot(origin, index);
        ShapeDraw.Arrow(center + Vector3.left * 0.8f, center + Vector3.right * 0.8f, 0.12f, 0.4f, 0.35f, colorA, colorB);
    }

    private void DrawPath(Vector3 origin)
    {
        pathPoints.Clear();
        float sway = Mathf.Sin(Time.time) * 0.5f;
        Vector3 start = origin + new Vector3(0f, 0f, 2.5f);
        pathPoints.Add(start);
        pathPoints.Add(start + new Vector3(1f + sway, 0f, 1f));
        pathPoints.Add(start + new Vector3(sway, 0f, 2f));
        pathPoints.Add(start + new Vector3(1.5f + sway, 0f, 3f));

        PathDrawer.Draw(pathPoints, transform.forward, pathStyle, pathState, colorB, Time.deltaTime);
    }
}
}
