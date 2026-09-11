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
    [Required, SerializeField] private Material ribbonMaterial;
    [Required, SerializeField] private Material ribbonAdditiveMaterial;
    [Required, SerializeField] private CueStyleSO style;
    [SerializeField] private ArenaCameraDirector director;

    [SerializeField] private bool showBase = true;
    [SerializeField] private bool showPerception = true;
    [SerializeField] private bool showPath = true;
    [SerializeField] private bool showPercepts = false;
    [SerializeField] private bool showReticle = true;
    [SerializeField] private bool showSocial = true;
    [SerializeField] private bool showClash = true;
    [SerializeField] private bool showMining = true;
    [SerializeField] private bool showFlee = true;
    [SerializeField] private bool showSelection = true;
    [SerializeField] private bool showAbilities = true;

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

        public readonly CueAnim Selection = new CueAnim();

        public readonly CueAnim Reveal = new CueAnim();

        public readonly CueAnim Telegraph = new CueAnim();
        public float TelegraphPhase;
        public readonly CueAnim DiveArc = new CueAnim();
        public readonly CueAnim Hold = new CueAnim();
    }

    private readonly Dictionary<MoriMonchiController, CueState> cueCache = new();

    private void OnEnable()
    {
        CueDrawer.Configure(cueMaterial, additiveMaterial);
        CueRibbonDrawer.Configure(ribbonMaterial, ribbonAdditiveMaterial);
    }

    private void LateUpdate()
    {
        if (sandbox == null || style == null) return;

        CueDrawer.AlphaScale = style.GuideAlpha;

        float globalRadius = SocialTuningSO.Current != null ? SocialTuningSO.Current.PerceptionRadius : 0f;
        Vector3 eye = Camera.main != null ? Camera.main.transform.position : Vector3.up * 30f;

        foreach (var controller in sandbox.Spawned)
        {
            if (controller == null || controller.DNA == null) continue;

            var state = GetCueState(controller);
            var intent = controller.Agent.Intent;
            if (intent == CreatureIntent.Dazed || intent == CreatureIntent.Tumbling) continue;
            Vector3 origin = controller.transform.position + Vector3.up * style.HeightOffset;
            float perceptionRadius = controller.Agent.HasVisionCone ? controller.Agent.VisionRadius : globalRadius;

            bool rival = controller.Agent.Team == ExpeditionTeam.Rival;
            bool active = ExpeditionNav.IsRevealing(controller.Agent.Intent) || (director != null && director.Pinned == controller.Agent);
            float reveal = rival ? Step(state.Reveal, active, style.RevealSeconds, Time.deltaTime) : 1f;

            if (showBase) CreatureCueDrawer.Base(style, controller, origin);

            if (showClash)
            {
                bool telegraphing = controller.Agent.ClashTelegraphing;
                bool holding = controller.Agent.ClashHolding;
                if (telegraphing && !state.Telegraph.Visible) state.TelegraphPhase = 0f;
                float tele = Step(state.Telegraph, telegraphing, style.TelegraphFadeSeconds, Time.deltaTime);
                float hold = Step(state.Hold, holding, style.TelegraphFadeSeconds, Time.deltaTime);
                float arc = Step(state.DiveArc, telegraphing && !controller.Agent.IsAirborne, style.TelegraphFadeSeconds, Time.deltaTime);
                if (tele > 0.01f)
                {
                    float hz = holding ? style.TelegraphBlinkSpeedEnd : Mathf.Lerp(style.TelegraphBlinkSpeed, style.TelegraphBlinkSpeedEnd, controller.Agent.ClashTell01);
                    state.TelegraphPhase += Time.deltaTime * hz;
                    float wave = 0.5f + 0.5f * Mathf.Sin(state.TelegraphPhase * Mathf.PI * 2f);
                    float blink = Mathf.Lerp(style.TelegraphBlinkMin, 1f, Mathf.SmoothStep(0.25f, 0.75f, wave));
                    if (holding) blink = Mathf.Max(blink, 0.75f);
                    CueDrawer.AlphaScale = 1f;
                    CreatureCueDrawer.Telegraph(style, controller, origin, tele, blink, arc, eye, hold);
                    CueDrawer.AlphaScale = style.GuideAlpha;
                }
            }

            if (rival)
            {
                if (reveal > 0.01f)
                {
                    if (showMining) CreatureCueDrawer.Mining(style, controller, origin, reveal);
                    if (showAbilities) CreatureCueDrawer.AbilityBursts(style, controller, origin, reveal);
                }

                if (showSelection) DrawSelection(controller, state, origin);
                continue;
            }

            if (showPerception && SocialTuningSO.Current != null)
                DrawPerception(controller, state, origin, perceptionRadius);

            Color pathColor = controller.Agent.Intent == CreatureIntent.Fleeing ? style.FleeColor : style.ColorFor(controller.Agent.Intent);
            if (showPath) CuePathDrawer.Draw(style, state.Path, controller.transform, pathColor, Time.deltaTime, OnScreen(controller.transform.position));

            if (showPercepts) DrawPercepts(controller, origin, perceptionRadius);

            if (showReticle) DrawReticle(controller, state);

            if (showSocial) CreatureCueDrawer.Social(style, controller);

            if (showMining) CreatureCueDrawer.Mining(style, controller, origin, 1f);

            if (showAbilities) CreatureCueDrawer.AbilityBursts(style, controller, origin, 1f);

            if (showFlee) CreatureCueDrawer.Flee(style, controller, origin);
            if (showFlee) CreatureCueDrawer.Trust(style, controller);

            if (showSelection) DrawSelection(controller, state, origin);
        }

        CueDrawer.AlphaScale = 1f;
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

        bool rivalInSight = false;
        foreach (var p in controller.Agent.Percepts)
        {
            if (p.Kind != PerceivableKind.Monchi) continue;
            if (!ExpeditionTeams.AreRivals(controller.Agent.Team, p.Team)) continue;
            rivalInSight = true;
            break;
        }

        Color tint = rivalInSight
            ? (controller.Agent.Orders.Contact == ContactChoice.Fight ? style.FoeColor : style.FleeColor)
            : controller.DNA.BaseColor;

        float fillInnerAlpha = rivalInSight ? style.ContactFillAlpha : style.VisionFillInnerAlpha;
        float fillOuterAlpha = rivalInSight ? 0f : style.VisionFillOuterAlpha;
        float edgeAlpha = rivalInSight ? style.ContactEdgeAlpha : style.VisionEdgeAlpha;

        CueDrawer.Sector(origin, radius, start, sweep, tint, fillInnerAlpha, fillOuterAlpha);

        Color rimColor = tint;
        rimColor.a = edgeAlpha;
        CueDrawer.DashedArc(origin, radius, style.RingThickness, start, sweep, style.ConeDashCount, style.ConeDashRatio, Time.time * style.ConeDashSpinSpeed, rimColor, rimColor);

        if (sweep < Mathf.PI * 2f - 0.01f)
        {
            Color sideNear = tint;
            sideNear.a = 0f;
            Color sideFar = tint;
            sideFar.a = style.VisionSideAlpha;
            Vector3 edgeA = origin + new Vector3(Mathf.Cos(start), 0f, Mathf.Sin(start)) * radius;
            Vector3 edgeB = origin + new Vector3(Mathf.Cos(start + sweep), 0f, Mathf.Sin(start + sweep)) * radius;
            CueDrawer.DashedSegment(origin, edgeA, style.RingThickness * 0.7f, style.PathDashLength, style.PathDashGap, 0f, sideNear, sideFar);
            CueDrawer.DashedSegment(origin, edgeB, style.RingThickness * 0.7f, style.PathDashLength, style.PathDashGap, 0f, sideNear, sideFar);
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
        Color color = controller.Agent.Intent == CreatureIntent.Fleeing ? style.FleeColor : style.ColorFor(controller.Agent.Intent);
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

    private void DrawSelection(MoriMonchiController controller, CueState state, Vector3 origin)
    {
        bool selected = director != null && director.Pinned != null && director.Pinned == controller.Agent;
        float alpha = Step(state.Selection, selected, style.AppearSeconds, Time.deltaTime);
        if (alpha <= 0.01f) return;

        float radius = style.SelectRadius * AppearScale(alpha, style.SelectAppearScale) * (1f + style.SelectPulseAmount * Mathf.Sin(Time.time * style.SelectPulseSpeed));
        Color color = style.SelectColor;
        color.a = alpha;

        CueDrawer.Disc(origin, radius, style.SelectColor, style.SelectGlowAlpha * alpha, 0f, true);
        CueDrawer.DashedRing(origin, radius, style.SelectThickness, style.SelectDashCount, style.SelectDashRatio, Time.time * style.SelectSpinSpeed, color, true);

        Color soft = color;
        soft.a = alpha * 0.5f;
        CueDrawer.DashedRing(origin, radius * 0.8f, style.SelectThickness * 0.6f, style.SelectDashCount, style.SelectDashRatio, -Time.time * style.SelectSpinSpeed * 1.5f, soft, true);
    }

    private static bool OnScreen(Vector3 world)
    {
        var cam = Camera.main;
        if (cam == null) return true;
        Vector3 v = cam.WorldToViewportPoint(world);
        return v.z > 0f && v.x > -0.05f && v.x < 1.05f && v.y > -0.05f && v.y < 1.05f;
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
