using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

public class PathCueState
{
    public NavMeshAgent Nav;
    public Vector3 ShownEnd;
    public bool HasShown;
    public float Alpha;
    public Vector3[] Corners;
    public float DestAlpha;
    public Vector3 LastDestination;
    public bool HasDestination;
}

public static class CuePathDrawer
{
    private static readonly List<Vector3> corners = new();

    public static void Draw(CueStyleSO style, PathCueState state, Transform body, Color baseColor, float dt)
    {
        var nav = state.Nav;

        bool hasValidPath = nav != null && nav.enabled && nav.isOnNavMesh && nav.hasPath && nav.path.corners.Length >= 2;
        Vector3 destination = default;
        if (hasValidPath) destination = nav.path.corners[nav.path.corners.Length - 1];

        if (hasValidPath && Vector3.Distance(body.position, destination) > 0.3f)
        {
            var pathCorners = nav.path.corners;

            if (!state.HasShown)
            {
                state.ShownEnd = destination;
                state.HasShown = true;
            }
            else
            {
                state.ShownEnd = Vector3.Lerp(state.ShownEnd, destination, 1f - Mathf.Exp(-style.PathSmoothing * dt));
            }

            if (!state.HasDestination || Vector3.Distance(state.LastDestination, destination) > 1f)
            {
                state.DestAlpha = 0f;
                state.LastDestination = destination;
                state.HasDestination = true;
            }

            state.Alpha = Mathf.MoveTowards(state.Alpha, 1f, dt / style.PathFadeSeconds);
            state.Corners = pathCorners;
        }
        else
        {
            state.Alpha = Mathf.MoveTowards(state.Alpha, 0f, dt / style.PathFadeSeconds);
            if (state.Alpha <= 0f)
            {
                state.HasShown = false;
                state.HasDestination = false;
            }
        }

        float destTarget = hasValidPath ? 1f : 0f;
        state.DestAlpha = style.AppearSeconds > 0f ? Mathf.MoveTowards(state.DestAlpha, destTarget, dt / style.AppearSeconds) : destTarget;

        if (state.Alpha <= 0.01f || state.Corners == null) return;

        corners.Clear();
        corners.Add(body.position + Vector3.up * style.HeightOffset);
        for (int i = 1; i < state.Corners.Length - 1; i++)
            corners.Add(state.Corners[i] + Vector3.up * style.HeightOffset);
        corners.Add(state.ShownEnd + Vector3.up * style.HeightOffset);

        Vector3 forward = body.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;

        Vector3 first = corners[0];
        Vector3 last = corners[corners.Count - 1];
        Vector3 secondToLast = corners.Count >= 2 ? corners[corners.Count - 2] : first;

        Vector3 virtualStart = first - forward * style.StartTangent;
        Vector3 virtualEnd = last + (last - secondToLast);

        int segmentCount = corners.Count - 1;
        float traveledLength = 0f;

        for (int seg = 0; seg < segmentCount; seg++)
        {
            Vector3 p0 = seg == 0 ? virtualStart : corners[seg - 1];
            Vector3 p1 = corners[seg];
            Vector3 p2 = corners[seg + 1];
            Vector3 p3 = seg == segmentCount - 1 ? virtualEnd : corners[seg + 2];

            bool isLastSegment = seg == segmentCount - 1;

            Vector3 prevPoint = CatmullRom(p0, p1, p2, p3, 0f);
            for (int s = 1; s <= style.CurveSamples; s++)
            {
                float t = (float)s / style.CurveSamples;
                Vector3 point = CatmullRom(p0, p1, p2, p3, t);

                float tPrev = (seg + (float)(s - 1) / style.CurveSamples) / segmentCount;
                float tCur = (seg + t) / segmentCount;

                Color colorA = baseColor;
                colorA.a = Mathf.Lerp(style.PathTailAlpha, 1f, tPrev) * state.Alpha;
                Color colorB = baseColor;
                colorB.a = Mathf.Lerp(style.PathTailAlpha, 1f, tCur) * state.Alpha;

                if (isLastSegment && s == style.CurveSamples)
                {
                    CueDrawer.Arrow(prevPoint, point, style.PathThickness, style.HeadLength, style.HeadWidth, colorA, colorB);
                }
                else
                {
                    float dashOffset = Time.time * style.PathFlowSpeed - traveledLength;
                    CueDrawer.DashedSegment(prevPoint, point, style.PathThickness, style.PathDashLength, style.PathDashGap, dashOffset, colorA, colorB);
                }

                traveledLength += Vector3.Distance(prevPoint, point);
                prevPoint = point;
            }
        }

        DrawDestinationMarker(style, state, baseColor);
    }

    private static void DrawDestinationMarker(CueStyleSO style, PathCueState state, Color intentColor)
    {
        if (state.DestAlpha <= 0.01f) return;

        float pulse = 1f + style.DestPulseAmount * Mathf.Sin(Time.time * style.DestPulseSpeed);
        float appear = Mathf.Lerp(style.ReticleAppearScale, 1f, Mathf.SmoothStep(0f, 1f, state.DestAlpha));
        float radius = style.DestMarkerRadius * pulse * appear;
        float alpha = 0.6f * state.Alpha * state.DestAlpha;

        CueDrawer.Disc(state.ShownEnd + Vector3.up * style.HeightOffset, radius, intentColor, alpha, 0f, true);
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;

        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3
        );
    }
}
}
