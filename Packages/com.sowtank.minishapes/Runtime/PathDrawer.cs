using System.Collections.Generic;
using UnityEngine;

namespace Sowtank.MiniShapes
{

public static class PathDrawer
{
    private static readonly List<Vector3> corners = new();

    public static void Draw(IReadOnlyList<Vector3> points, Vector3 startForward, PathStyle style, PathState state, Color color, float dt, bool visible = true)
    {
        bool hasValidPath = points != null && points.Count >= 2 && visible;
        Vector3 destination = default;
        if (hasValidPath) destination = points[points.Count - 1];

        if (hasValidPath && Vector3.Distance(points[0], destination) > style.MinLength)
        {
            if (!state.HasShown)
            {
                state.ShownEnd = destination;
                state.HasShown = true;
            }
            else
            {
                state.ShownEnd = Vector3.Lerp(state.ShownEnd, destination, 1f - Mathf.Exp(-style.Smoothing * dt));
            }

            if (!state.HasDestination || Vector3.Distance(state.LastDestination, destination) > style.DestRetargetDistance)
            {
                state.DestAlpha = 0f;
                state.LastDestination = destination;
                state.HasDestination = true;
            }

            state.Alpha = Mathf.MoveTowards(state.Alpha, 1f, dt / style.FadeSeconds);

            state.Points.Clear();
            for (int i = 0; i < points.Count; i++) state.Points.Add(points[i]);
        }
        else
        {
            state.Alpha = Mathf.MoveTowards(state.Alpha, 0f, dt / style.FadeSeconds);
            if (state.Alpha <= 0f)
            {
                state.HasShown = false;
                state.HasDestination = false;
            }
        }

        float destTarget = hasValidPath ? 1f : 0f;
        state.DestAlpha = style.AppearSeconds > 0f ? Mathf.MoveTowards(state.DestAlpha, destTarget, dt / style.AppearSeconds) : destTarget;

        if (state.Alpha <= 0.01f || state.Points.Count < 2) return;

        corners.Clear();
        corners.Add(state.Points[0] + Vector3.up * style.HeightOffset);
        for (int i = 1; i < state.Points.Count - 1; i++)
            corners.Add(state.Points[i] + Vector3.up * style.HeightOffset);
        corners.Add(state.ShownEnd + Vector3.up * style.HeightOffset);

        Vector3 forward = startForward;
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

                Color colorA = color;
                colorA.a = Mathf.Lerp(style.TailAlpha, 1f, tPrev) * state.Alpha;
                Color colorB = color;
                colorB.a = Mathf.Lerp(style.TailAlpha, 1f, tCur) * state.Alpha;

                if (isLastSegment && s == style.CurveSamples)
                {
                    ShapeDraw.Arrow(prevPoint, point, style.Thickness, style.HeadLength, style.HeadWidth, colorA, colorB);
                }
                else
                {
                    float dashOffset = Time.time * style.FlowSpeed - traveledLength;
                    ShapeDraw.DashedSegment(prevPoint, point, style.Thickness, style.DashLength, style.DashGap, dashOffset, colorA, colorB);
                }

                traveledLength += Vector3.Distance(prevPoint, point);
                prevPoint = point;
            }
        }

        DrawDestinationMarker(style, state, color);
    }

    private static void DrawDestinationMarker(PathStyle style, PathState state, Color color)
    {
        if (state.DestAlpha <= 0.01f) return;

        float pulse = 1f + style.DestPulseAmount * Mathf.Sin(Time.time * style.DestPulseSpeed);
        float appear = Mathf.Lerp(style.DestAppearScale, 1f, Mathf.SmoothStep(0f, 1f, state.DestAlpha));
        float radius = style.DestMarkerRadius * pulse * appear;
        float alpha = style.DestAlpha * state.Alpha * state.DestAlpha;

        ShapeDraw.Disc(state.ShownEnd + Vector3.up * style.HeightOffset, radius, color, alpha, 0f, true);
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
