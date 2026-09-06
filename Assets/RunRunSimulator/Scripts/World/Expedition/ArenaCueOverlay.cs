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
    [SerializeField] private bool showReticle = true;
    [SerializeField] private bool showSocial = true;
    [SerializeField] private bool showClash = true;
    [SerializeField] private bool showMining = true;

    private class CueAnim
    {
        public float Alpha;
        public bool Visible;
    }

    private class CueState
    {
        public readonly PathCueState Path = new PathCueState();

        public readonly CueAnim PerceptionAppear = new CueAnim();
        public int LastPerceptCount;
        public float PulseElapsed = -1f;

        public readonly CueAnim Reticle = new CueAnim();
        public Vector3 LastTargetPosition;

        public float FacingAngle;
        public bool HasFacing;
    }

    private readonly Dictionary<MoriMonchiController, CueState> cueCache = new();

    private void OnEnable()
    {
        CueDrawer.Configure(cueMaterial, additiveMaterial);
    }

    private void LateUpdate()
    {
        if (sandbox == null || style == null) return;

        float globalRadius = SocialTuningSO.Current != null ? SocialTuningSO.Current.PerceptionRadius : 0f;

        foreach (var controller in sandbox.Spawned)
        {
            if (controller == null || controller.DNA == null) continue;

            var state = GetCueState(controller);
            Vector3 origin = controller.transform.position + Vector3.up * style.HeightOffset;
            float perceptionRadius = controller.Agent.HasVisionCone ? controller.Agent.VisionRadius : globalRadius;

            if (showPerception && SocialTuningSO.Current != null)
                DrawPerception(controller, state, origin, perceptionRadius);

            if (showPath) CuePathDrawer.Draw(style, state.Path, controller.transform, style.ColorFor(controller.Agent.Intent), Time.deltaTime);

            if (showPercepts) DrawPercepts(controller, origin, perceptionRadius);

            if (showReticle) DrawReticle(controller, state);

            if (showSocial) DrawSocial(controller);

            if (showClash) DrawClash(controller);

            if (showMining) DrawMining(controller, origin);
        }
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

        if (controller.Agent.HasVisionCone)
            DrawVisionCone(controller, state, origin, radius);
        else
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

    private void DrawVisionCone(MoriMonchiController controller, CueState state, Vector3 origin, float radius)
    {
        float facing = VisionProfile.FacingAngle(controller.transform.forward);
        if (!state.HasFacing)
        {
            state.FacingAngle = facing;
            state.HasFacing = true;
        }
        else
        {
            float delta = Mathf.DeltaAngle(state.FacingAngle * Mathf.Rad2Deg, facing * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            state.FacingAngle += delta * (1f - Mathf.Exp(-style.VisionTurnSmoothing * Time.deltaTime));
        }

        float sweep = controller.Agent.VisionDegrees * Mathf.Deg2Rad;
        float start = state.FacingAngle - sweep * 0.5f;
        Color tint = controller.DNA.BaseColor;

        CueDrawer.Sector(origin, radius, start, sweep, tint, style.VisionFillInnerAlpha, style.VisionFillOuterAlpha);

        Color rimColor = tint;
        rimColor.a = style.VisionEdgeAlpha;
        CueDrawer.Arc(origin, radius, style.RingThickness, start, sweep, rimColor, rimColor);

        if (sweep < Mathf.PI * 2f - 0.01f)
        {
            Color sideNear = tint;
            sideNear.a = 0f;
            Color sideFar = tint;
            sideFar.a = style.VisionSideAlpha;
            Vector3 edgeA = origin + new Vector3(Mathf.Cos(start), 0f, Mathf.Sin(start)) * radius;
            Vector3 edgeB = origin + new Vector3(Mathf.Cos(start + sweep), 0f, Mathf.Sin(start + sweep)) * radius;
            CueDrawer.Segment(origin, edgeA, style.RingThickness * 0.7f, sideNear, sideFar);
            CueDrawer.Segment(origin, edgeB, style.RingThickness * 0.7f, sideNear, sideFar);
        }

        float nearRadius = controller.Agent.NearSenseRadius;
        if (nearRadius > 0f)
        {
            Color nearColor = tint;
            nearColor.a = style.NearRingAlpha;
            CueDrawer.DashedRing(origin, nearRadius, style.RingThickness * 0.8f, Mathf.Max(8, style.RingDashCount / 3), style.RingDashRatio, Time.time * style.RingSpinSpeed, nearColor);
        }
    }

    private void DrawPercepts(MoriMonchiController controller, Vector3 origin, float perceptionRadius)
    {
        foreach (var p in controller.Agent.Percepts)
        {
            if (p.Kind != PerceivableKind.Monchi || p.Source == null) continue;

            var mine = controller.Agent.Team;
            Color color = ExpeditionTeams.AreRivals(mine, p.Team) ? style.FoeColor
                        : ExpeditionTeams.AreAllies(mine, p.Team) ? style.FriendColor
                        : Color.Lerp(style.FoeColor, style.FriendColor, (p.Affinity + 1f) * 0.5f);

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

    private void DrawMining(MoriMonchiController controller, Vector3 origin)
    {
        if (controller.Agent.Intent != CreatureIntent.Taking) return;

        float progress = controller.Agent.MiningProgress;
        if (progress <= 0f) return;

        Color trackColor = style.ColorFor(CreatureIntent.Taking);
        trackColor.a = 0.15f;
        CueDrawer.Ring(origin, style.MiningArcRadius, style.MiningArcThickness, trackColor);

        Color arcColor = style.ColorFor(CreatureIntent.Taking);
        arcColor.a = style.MiningArcAlpha;

        float startAngle = Mathf.PI * 0.5f;
        float sweep = progress * Mathf.PI * 2f;
        CueDrawer.Arc(origin, style.MiningArcRadius, style.MiningArcThickness, startAngle, sweep, arcColor, arcColor, true);
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

    private void DrawClash(MoriMonchiController controller)
    {
        var target = controller.Agent.ClashTarget;
        if (target == null) return;

        Vector3 a = controller.transform.position + Vector3.up * style.HeightOffset;
        Vector3 b = target.transform.position + Vector3.up * style.HeightOffset;

        Color head = style.FightColor;
        head.a = 0.55f + 0.45f * Mathf.Sin(Time.time * style.FightPulseSpeed);
        Color tail = head;
        tail.a *= style.PathTailAlpha;

        CueDrawer.Arrow(a, b, style.PathThickness * 1.5f, style.HeadLength, style.HeadWidth, tail, head, true);
    }

    private CueState GetCueState(MoriMonchiController controller)
    {
        if (cueCache.TryGetValue(controller, out var state)) return state;
        state = new CueState();
        state.Path.Nav = controller.GetComponent<NavMeshAgent>();
        cueCache[controller] = state;
        return state;
    }
}
}
