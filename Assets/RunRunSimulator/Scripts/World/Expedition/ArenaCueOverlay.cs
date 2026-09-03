using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

public class ArenaCueOverlay : MonoBehaviour
{
    [Required, SerializeField] private ArenaSandbox sandbox;
    [Required, SerializeField] private Material cueMaterial;
    [Required, SerializeField] private Material additiveMaterial;
    [Required, SerializeField] private CueStyleSO style;

    [SerializeField] private bool showPerception = true;
    [SerializeField] private bool showPath = true;
    [SerializeField] private bool showPercepts = true;
    [SerializeField] private bool showMinerals = true;
    [SerializeField] private bool showReticle = true;
    [SerializeField] private bool showSocial = true;

    private class CueAnim
    {
        public float Alpha;
        public bool Visible;
    }

    private class CueState
    {
        public NavMeshAgent Nav;
        public Vector3 ShownEnd;
        public bool HasShown;
        public float Alpha;
        public Vector3[] Corners;

        public readonly CueAnim PerceptionAppear = new CueAnim();
        public int LastPerceptCount;
        public float PulseElapsed = -1f;

        public readonly CueAnim DestAppear = new CueAnim();
        public Vector3 LastDestination;
        public bool HasDestination;

        public readonly CueAnim Reticle = new CueAnim();
        public Vector3 LastTargetPosition;
    }

    private readonly Dictionary<MoriMonchiController, CueState> cueCache = new();
    private readonly Dictionary<MaterialPickup, CueAnim> mineralAnims = new();
    private readonly List<Vector3> cornersBuffer = new();

    private void OnEnable()
    {
        CueDrawer.Configure(cueMaterial, additiveMaterial);
    }

    private void LateUpdate()
    {
        if (sandbox == null || style == null) return;

        float perceptionRadius = SocialTuningSO.Current != null ? SocialTuningSO.Current.PerceptionRadius : 0f;

        foreach (var controller in sandbox.Spawned)
        {
            if (controller == null || controller.DNA == null) continue;

            var state = GetCueState(controller);
            Vector3 origin = controller.transform.position + Vector3.up * style.HeightOffset;

            if (showPerception && SocialTuningSO.Current != null)
                DrawPerception(controller, state, origin, perceptionRadius);

            if (showPath) DrawPath(controller, state);

            if (showPercepts) DrawPercepts(controller, origin, perceptionRadius);

            if (showReticle) DrawReticle(controller, state);

            if (showSocial) DrawSocial(controller);
        }

        if (showMinerals) DrawMinerals();
    }

    private static float Step(CueAnim anim, bool visible, float seconds, float dt)
    {
        anim.Visible = visible;
        float target = visible ? 1f : 0f;
        anim.Alpha = seconds > 0f ? Mathf.MoveTowards(anim.Alpha, target, dt / seconds) : target;
        return anim.Alpha;
    }

    private static float AppearScale(float alpha, float from) =>
        Mathf.Lerp(from, 1f, Mathf.SmoothStep(0f, 1f, alpha));

    private void DrawPerception(MoriMonchiController controller, CueState state, Vector3 origin, float perceptionRadius)
    {
        float appear = Step(state.PerceptionAppear, true, style.AppearSeconds, Time.deltaTime);
        float radius = perceptionRadius * AppearScale(appear, style.AppearScale);

        int perceptCount = controller.Agent.Percepts.Count;
        if (perceptCount > state.LastPerceptCount) state.PulseElapsed = 0f;
        state.LastPerceptCount = perceptCount;

        if (state.PulseElapsed >= 0f)
        {
            state.PulseElapsed += Time.deltaTime;
            if (state.PulseElapsed >= style.PulseSeconds)
                state.PulseElapsed = -1f;
            else
                radius *= 1f + style.PulseAmount * Mathf.Sin(Mathf.PI * (state.PulseElapsed / style.PulseSeconds));
        }

        Color ringColor = controller.DNA.BaseColor;
        ringColor.a = style.RingAlpha;
        CueDrawer.DashedRing(origin, radius, style.RingThickness, style.RingDashCount, style.RingDashRatio, Time.time * style.RingSpinSpeed, ringColor);

        if (perceptCount == 0) return;

        var nearest = controller.Agent.Percepts[0];
        if (nearest.Source == null) return;

        Vector3 dir = nearest.Source.Position - origin;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(dir.z, dir.x);
        float half = style.AttentionArcDegrees * 0.5f * Mathf.Deg2Rad;

        Color coreColor = controller.DNA.BaseColor;
        coreColor.a = style.AttentionAlpha;
        Color edgeColor = coreColor;
        edgeColor.a = 0f;

        CueDrawer.Arc(origin, radius, style.RingThickness, angle, half, coreColor, edgeColor, true);
        CueDrawer.Arc(origin, radius, style.RingThickness, angle, -half, coreColor, edgeColor, true);
    }

    private void DrawPath(MoriMonchiController controller, CueState state)
    {
        var nav = state.Nav;

        bool hasValidPath = nav != null && nav.enabled && nav.isOnNavMesh && nav.hasPath && nav.path.corners.Length >= 2;
        Vector3 destination = default;
        if (hasValidPath) destination = nav.path.corners[nav.path.corners.Length - 1];

        if (hasValidPath && Vector3.Distance(controller.transform.position, destination) > 0.3f)
        {
            var corners = nav.path.corners;

            if (!state.HasShown)
            {
                state.ShownEnd = destination;
                state.HasShown = true;
            }
            else
            {
                state.ShownEnd = Vector3.Lerp(state.ShownEnd, destination, 1f - Mathf.Exp(-style.PathSmoothing * Time.deltaTime));
            }

            if (!state.HasDestination || Vector3.Distance(state.LastDestination, destination) > 1f)
            {
                state.DestAppear.Alpha = 0f;
                state.LastDestination = destination;
                state.HasDestination = true;
            }

            state.Alpha = Mathf.MoveTowards(state.Alpha, 1f, Time.deltaTime / style.PathFadeSeconds);
            state.Corners = corners;
        }
        else
        {
            state.Alpha = Mathf.MoveTowards(state.Alpha, 0f, Time.deltaTime / style.PathFadeSeconds);
            if (state.Alpha <= 0f)
            {
                state.HasShown = false;
                state.HasDestination = false;
            }
        }

        Step(state.DestAppear, hasValidPath, style.AppearSeconds, Time.deltaTime);

        if (state.Alpha <= 0.01f || state.Corners == null) return;

        Color baseColor = style.ColorFor(controller.Agent.Intent);

        cornersBuffer.Clear();
        cornersBuffer.Add(controller.transform.position + Vector3.up * style.HeightOffset);
        for (int i = 1; i < state.Corners.Length - 1; i++)
            cornersBuffer.Add(state.Corners[i] + Vector3.up * style.HeightOffset);
        cornersBuffer.Add(state.ShownEnd + Vector3.up * style.HeightOffset);

        Vector3 forward = controller.transform.forward;
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;

        Vector3 first = cornersBuffer[0];
        Vector3 last = cornersBuffer[cornersBuffer.Count - 1];
        Vector3 secondToLast = cornersBuffer.Count >= 2 ? cornersBuffer[cornersBuffer.Count - 2] : first;

        Vector3 virtualStart = first - forward * style.StartTangent;
        Vector3 virtualEnd = last + (last - secondToLast);

        int segmentCount = cornersBuffer.Count - 1;
        float traveledLength = 0f;

        for (int seg = 0; seg < segmentCount; seg++)
        {
            Vector3 p0 = seg == 0 ? virtualStart : cornersBuffer[seg - 1];
            Vector3 p1 = cornersBuffer[seg];
            Vector3 p2 = cornersBuffer[seg + 1];
            Vector3 p3 = seg == segmentCount - 1 ? virtualEnd : cornersBuffer[seg + 2];

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

        DrawDestinationMarker(state, baseColor);
    }

    private void DrawDestinationMarker(CueState state, Color intentColor)
    {
        if (state.DestAppear.Alpha <= 0.01f) return;

        float pulse = 1f + style.DestPulseAmount * Mathf.Sin(Time.time * style.DestPulseSpeed);
        float radius = style.DestMarkerRadius * pulse * AppearScale(state.DestAppear.Alpha, style.ReticleAppearScale);
        float alpha = 0.6f * state.Alpha * state.DestAppear.Alpha;

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

    private void DrawPercepts(MoriMonchiController controller, Vector3 origin, float perceptionRadius)
    {
        foreach (var p in controller.Agent.Percepts)
        {
            if (p.Kind != PerceivableKind.Monchi || p.Source == null) continue;

            float t = (p.Affinity + 1f) * 0.5f;
            Color color = Color.Lerp(style.FoeColor, style.FriendColor, t);

            float distance = Mathf.Sqrt(p.SqrDistance);
            float falloff = perceptionRadius > 0f ? Mathf.Clamp01(distance / perceptionRadius) : 1f;
            color.a = style.PerceptAlpha * (1f - falloff);

            Color colorB = color;
            colorB.a = style.PerceptFarAlpha;

            Vector3 target = p.Source.Position + Vector3.up * style.HeightOffset;
            CueDrawer.DashedSegment(origin, target, style.PerceptThickness, style.PerceptDashLength, style.PerceptDashGap, Time.time * style.PerceptFlowSpeed, color, colorB);
        }
    }

    private void DrawReticle(MoriMonchiController controller, CueState state)
    {
        var target = controller.Agent.ExpeditionTarget;
        bool hasTarget = target != null;
        if (hasTarget) state.LastTargetPosition = target.position;

        float alpha = Step(state.Reticle, hasTarget, style.AppearSeconds, Time.deltaTime);
        if (alpha <= 0.01f) return;

        float radius = style.ReticleRadius * AppearScale(alpha, style.ReticleAppearScale);
        Color color = style.ColorFor(controller.Agent.Intent);
        color.a *= alpha;

        Vector3 center = state.LastTargetPosition + Vector3.up * style.HeightOffset;
        float sweep = style.ReticleSweepDegrees * Mathf.Deg2Rad;
        float half = sweep * 0.5f;
        float spin = Time.time * style.ReticleSpinSpeed;

        for (int k = 0; k < 4; k++)
        {
            float centerAngle = (45f + k * 90f) * Mathf.Deg2Rad + spin;
            CueDrawer.Arc(center, radius, style.ReticleThickness, centerAngle - half, sweep, color, color, true);
        }
    }

    private void DrawMinerals()
    {
        if (sandbox.Minerals == null) return;

        foreach (var m in sandbox.Minerals)
        {
            if (m == null) continue;

            var anim = GetMineralAnim(m);
            float alpha = Step(anim, !m.Taken, style.AppearSeconds, Time.deltaTime);
            if (alpha <= 0.01f) continue;

            Vector3 center = m.transform.position + Vector3.up * style.HeightOffset;
            float radius = style.MineralDiscRadius * (m.Value > 1 ? 1.6f : 1f);

            CueDrawer.Disc(center, radius, style.MineralColor, style.MineralInnerAlpha * alpha, style.MineralOuterAlpha * alpha);

            Color ringColor = style.MineralColor;
            ringColor.a = style.MineralRingAlpha * alpha;
            CueDrawer.Ring(center, radius, style.MineralRingThickness, ringColor);

            if (m.Value > 1)
                CueDrawer.DashedRing(center, radius, style.MineralRingThickness, style.RingDashCount, style.RingDashRatio, Time.time * -style.RingSpinSpeed, ringColor);
        }
    }

    private void DrawSocial(MoriMonchiController controller)
    {
        var partner = controller.Agent.SocialPartner;
        if (partner == null) return;
        if (controller.Agent.GetInstanceID() >= partner.GetInstanceID()) return;

        Vector3 a = controller.transform.position + Vector3.up * style.HeightOffset;
        Vector3 b = partner.transform.position + Vector3.up * style.HeightOffset;

        bool fighting = controller.Agent.Intent == CreatureIntent.Fighting;
        Color color = fighting ? style.FightColor : style.SocialLinkColor;
        if (fighting) color.a = 0.5f + 0.5f * Mathf.Sin(Time.time * style.FightPulseSpeed);

        CueDrawer.DashedSegment(a, b, style.SocialLinkThickness, style.PerceptDashLength, style.PerceptDashGap, Time.time * style.PerceptFlowSpeed, color, color, fighting);
    }

    private CueState GetCueState(MoriMonchiController controller)
    {
        if (cueCache.TryGetValue(controller, out var state)) return state;
        state = new CueState { Nav = controller.GetComponent<NavMeshAgent>() };
        cueCache[controller] = state;
        return state;
    }

    private CueAnim GetMineralAnim(MaterialPickup mineral)
    {
        if (mineralAnims.TryGetValue(mineral, out var anim)) return anim;
        anim = new CueAnim();
        mineralAnims[mineral] = anim;
        return anim;
    }
}
}
