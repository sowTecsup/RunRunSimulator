using UnityEngine;

namespace Sowtank.MiniShapes
{

[System.Serializable]
public class PathStyle
{
    public float HeightOffset = 0.03f;
    public float Thickness = 0.08f;
    public float DashLength = 0.35f;
    public float DashGap = 0.25f;
    public float FlowSpeed = 1.5f;
    public float TailAlpha = 0.15f;
    public float HeadLength = 0.5f;
    public float HeadWidth = 0.4f;
    public float StartTangent = 1.2f;
    public int CurveSamples = 10;
    public float Smoothing = 8f;
    public float FadeSeconds = 0.35f;
    public float AppearSeconds = 0.25f;
    public float MinLength = 0.3f;
    public float DestMarkerRadius = 0.35f;
    public float DestPulseAmount = 0.15f;
    public float DestPulseSpeed = 2.5f;
    public float DestAppearScale = 1.4f;
    public float DestAlpha = 0.6f;
    public float DestRetargetDistance = 1f;
}
}
