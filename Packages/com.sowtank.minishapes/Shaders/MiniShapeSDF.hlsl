#ifndef MINISHAPES_SDF_INCLUDED
#define MINISHAPES_SDF_INCLUDED

float SdCapsule(float2 p, float2 a, float2 b)
{
    float2 pa = p - a;
    float2 ba = b - a;
    float h = saturate(dot(pa, ba) / dot(ba, ba));
    return length(pa - ba * h);
}

float SdTriangle(float2 p, float2 p0, float2 p1, float2 p2)
{
    float2 e0 = p1 - p0;
    float2 e1 = p2 - p1;
    float2 e2 = p0 - p2;
    float2 v0 = p - p0;
    float2 v1 = p - p1;
    float2 v2 = p - p2;
    float2 pq0 = v0 - e0 * saturate(dot(v0, e0) / dot(e0, e0));
    float2 pq1 = v1 - e1 * saturate(dot(v1, e1) / dot(e1, e1));
    float2 pq2 = v2 - e2 * saturate(dot(v2, e2) / dot(e2, e2));
    float s = sign(e0.x * e2.y - e0.y * e2.x);
    float2 d0 = float2(dot(pq0, pq0), s * (v0.x * e0.y - v0.y * e0.x));
    float2 d1 = float2(dot(pq1, pq1), s * (v1.x * e1.y - v1.y * e1.x));
    float2 d2 = float2(dot(pq2, pq2), s * (v2.x * e2.y - v2.y * e2.x));
    float2 d = min(min(d0, d1), d2);
    return -sqrt(d.x) * sign(d.y);
}

void ShapeRing(float2 p, inout float d, inout float t, inout float radialT)
{
    d = abs(length(p - _Center.xz) - _Radius) - _Thickness * 0.5;
}

void ShapeDisc(float2 p, inout float d, inout float t, inout float radialT)
{
    d = length(p - _Center.xz) - _Radius;
    radialT = saturate(length(p - _Center.xz) / _Radius);
}

void ShapeSegment(float2 p, inout float d, inout float t, inout float radialT)
{
    float2 a = _PointA.xz;
    float2 b = _PointB.xz;
    float axial = SdCapsule(p, a, b);
    d = axial - _Thickness * 0.5;
    t = saturate(dot(p - a, b - a) / max(dot(b - a, b - a), 1e-6));
    radialT = saturate(axial / max(_Thickness * 0.5, 1e-5));
}

void ShapeArrow(float2 p, inout float d, inout float t, inout float radialT)
{
    float2 a = _PointA.xz;
    float2 b = _PointB.xz;
    float2 dir = b - a;
    float total = length(dir);
    float2 fwd = total > 1e-5 ? dir / total : float2(1, 0);
    float2 right = float2(-fwd.y, fwd.x);
    float2 baseCenter = b - fwd * _HeadLength;

    float dShaft = SdCapsule(p, a, baseCenter) - _Thickness * 0.5;
    float dHead = SdTriangle(p, b, baseCenter + right * (_HeadWidth * 0.5), baseCenter - right * (_HeadWidth * 0.5));

    d = min(dShaft, dHead);
    t = saturate(dot(p - a, b - a) / dot(b - a, b - a));
}

void ShapeDashedRing(float2 p, inout float d, inout float t, inout float radialT)
{
    float2 q = p - _Center.xz;
    float r = length(q);
    float ang = atan2(q.y, q.x) + _Rotation;
    float period = 6.28318530718 / max(_DashCount, 1.0);
    float local = frac(ang / period) * period - period * 0.5;
    float halfSpan = period * saturate(_DashRatio) * 0.5;
    float angDist = max(abs(local) - halfSpan, 0.0);
    float tangential = angDist * _Radius;
    float radial = r - _Radius;
    d = length(float2(radial, tangential)) - _Thickness * 0.5;
}

void ShapeArc(float2 p, inout float d, inout float t, inout float radialT)
{
    float2 q = p - _Center.xz;
    float ang = atan2(q.y, q.x);
    float tau = 6.28318530718;
    float rel = ang - _ArcStart;
    rel = rel - tau * floor(rel / tau);
    float sweep = max(_ArcSweep, 1e-5);

    if (rel <= sweep)
    {
        d = abs(length(q) - _Radius) - _Thickness * 0.5;
        t = rel / sweep;
    }
    else
    {
        float2 endStart = _Radius * float2(cos(_ArcStart), sin(_ArcStart));
        float2 endStop = _Radius * float2(cos(_ArcStart + sweep), sin(_ArcStart + sweep));
        float distStart = length(q - endStart);
        float distStop = length(q - endStop);

        if (distStart <= distStop)
        {
            d = distStart - _Thickness * 0.5;
            t = 0;
        }
        else
        {
            d = distStop - _Thickness * 0.5;
            t = 1;
        }
    }
}

void ShapeDashedSegment(float2 p, inout float d, inout float t, inout float radialT)
{
    float2 a = _PointA.xz;
    float2 b = _PointB.xz;
    float len = length(b - a);
    float2 dir = len > 1e-5 ? (b - a) / len : float2(1, 0);
    float2 perp = float2(-dir.y, dir.x);
    float u = dot(p - a, dir);
    float v = abs(dot(p - a, perp));

    float period = max(_DashLength + _DashGap, 1e-5);
    float local = frac((u - _DashOffset) / period) * period;
    local -= _DashLength * 0.5;
    float along = max(abs(local) - _DashLength * 0.5, 0.0);
    along = max(along, max(-u, u - len));

    d = length(float2(along, v)) - _Thickness * 0.5;
    t = saturate(u / len);
}

void ShapeSector(float2 p, inout float d, inout float t, inout float radialT)
{
    float2 q = p - _Center.xz;
    float r = length(q);
    float ang = atan2(q.y, q.x);
    float tau = 6.28318530718;
    float rel = ang - _ArcStart;
    rel = rel - tau * floor(rel / tau);
    float sweep = max(_ArcSweep, 1e-5);

    if (rel <= sweep)
    {
        d = r - _Radius;
        t = rel / sweep;
    }
    else
    {
        float2 edgeStart = _Radius * float2(cos(_ArcStart), sin(_ArcStart));
        float2 edgeStop = _Radius * float2(cos(_ArcStart + sweep), sin(_ArcStart + sweep));
        float dStart = SdCapsule(q, float2(0, 0), edgeStart);
        float dStop = SdCapsule(q, float2(0, 0), edgeStop);
        d = min(dStart, dStop);
        t = dStart <= dStop ? 0 : 1;
    }

    radialT = saturate(r / _Radius);
}

void ShapeDashedArc(float2 p, inout float d, inout float t, inout float radialT)
{
    float2 q = p - _Center.xz;
    float r = length(q);
    float tau = 6.28318530718;

    float angArc = atan2(q.y, q.x);
    float rel = angArc - _ArcStart;
    rel = rel - tau * floor(rel / tau);
    float sweep = max(_ArcSweep, 1e-5);

    float sdfArc;
    if (rel <= sweep)
    {
        sdfArc = abs(r - _Radius) - _Thickness * 0.5;
        t = rel / sweep;
    }
    else
    {
        float2 endStart = _Radius * float2(cos(_ArcStart), sin(_ArcStart));
        float2 endStop = _Radius * float2(cos(_ArcStart + sweep), sin(_ArcStart + sweep));
        float distStart = length(q - endStart);
        float distStop = length(q - endStop);

        if (distStart <= distStop)
        {
            sdfArc = distStart - _Thickness * 0.5;
            t = 0;
        }
        else
        {
            sdfArc = distStop - _Thickness * 0.5;
            t = 1;
        }
    }

    float angDash = angArc + _Rotation;
    float period = tau / max(_DashCount, 1.0);
    float local = frac(angDash / period) * period - period * 0.5;
    float halfSpan = period * saturate(_DashRatio) * 0.5;
    float angDist = max(abs(local) - halfSpan, 0.0);
    float tangential = angDist * _Radius;
    float radial = r - _Radius;
    float sdfDash = length(float2(radial, tangential)) - _Thickness * 0.5;

    d = max(sdfArc, sdfDash);
}

void ShapeCapsuleOutline(float2 p, inout float d, inout float t, inout float radialT)
{
    float2 a = _PointA.xz;
    float2 b = _PointB.xz;
    d = abs(SdCapsule(p, a, b) - _Radius) - _Thickness * 0.5;
    t = saturate(dot(p - a, b - a) / max(dot(b - a, b - a), 1e-6));
}

#endif
