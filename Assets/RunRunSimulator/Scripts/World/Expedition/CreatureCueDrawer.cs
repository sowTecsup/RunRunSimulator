using UnityEngine;
namespace MoriMonchiSimulator
{

public static class CreatureCueDrawer
{
    public static void Base(CueStyleSO style, MoriMonchiController controller, Vector3 origin)
    {
        var team = controller.Agent.Team;
        Color c = team == ExpeditionTeam.Player ? style.FriendColor
                : team == ExpeditionTeam.Rival ? style.FoeColor
                : controller.DNA.BaseColor;

        CueDrawer.Disc(origin, style.BaseRadius, c, style.BaseInnerAlpha, 0f);

        Color ring = c;
        ring.a = style.BaseRingAlpha;
        CueDrawer.Ring(origin, style.BaseRadius, style.BaseRingThickness, ring);
    }

    public static void Mining(CueStyleSO style, MoriMonchiController controller, Vector3 origin, float alphaMul)
    {
        var agent = controller.Agent;
        int capacity = agent.CarryCapacity;
        int carried = agent.Carried;
        bool mining = agent.Intent == CreatureIntent.Taking;
        if (capacity <= 0 || (carried <= 0 && !mining)) return;

        float gap = 0.14f;
        float slot = Mathf.PI * 2f / capacity;
        float start = Mathf.PI * 0.5f;

        Color track = style.ColorFor(CreatureIntent.Taking);
        track.a = 0.15f * alphaMul;
        Color full = style.ColorFor(CreatureIntent.Carrying);
        full.a = style.MiningArcAlpha * alphaMul;
        Color live = style.ColorFor(CreatureIntent.Taking);
        live.a = style.MiningArcAlpha * alphaMul;

        for (int k = 0; k < capacity; k++)
        {
            float from = start + k * slot + gap * 0.5f;
            float sweep = slot - gap;
            CueDrawer.Arc(origin, style.MiningArcRadius, style.MiningArcThickness, from, sweep, track, track);

            if (k < carried)
                CueDrawer.Arc(origin, style.MiningArcRadius, style.MiningArcThickness, from, sweep, full, full, true);
            else if (k == carried && mining && agent.MiningProgress > 0f)
                CueDrawer.Arc(origin, style.MiningArcRadius, style.MiningArcThickness, from, sweep * agent.MiningProgress, live, live, true);
        }
    }

    public static void Flee(CueStyleSO style, MoriMonchiController controller, Vector3 origin)
    {
        if (controller.Agent.Intent != CreatureIntent.Fleeing) return;

        Color color = style.FleeColor;
        color.a = 0.45f + 0.45f * Mathf.Sin(Time.time * style.FleePulseSpeed);
        CueDrawer.Ring(origin, style.FleeRingRadius, style.FleeRingThickness, color);

        Color outerColor = style.FleeColor;
        outerColor.a = color.a * 0.5f;
        CueDrawer.Ring(origin, style.FleeRingRadius * 1.5f, style.FleeRingThickness, outerColor);
    }

    public static void Trust(CueStyleSO style, MoriMonchiController controller)
    {
        var guardian = controller.Agent.TrustedGuardian;
        if (guardian == null) return;

        Vector3 a = controller.transform.position + Vector3.up * style.HeightOffset;
        Vector3 b = guardian.transform.position + Vector3.up * style.HeightOffset;

        Color color = style.FriendColor;
        color.a = 0.55f + 0.25f * Mathf.Sin(Time.time * style.FleePulseSpeed * 0.5f);

        CueDrawer.DashedSegment(a, b, style.SocialLinkThickness, style.PerceptDashLength, style.PerceptDashGap, Time.time * style.PerceptFlowSpeed, color, color);
    }

    public static void Social(CueStyleSO style, MoriMonchiController controller)
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

    public static void AbilityBursts(CueStyleSO style, MoriMonchiController controller, Vector3 origin, float alphaMul)
    {
        var agent = controller.Agent;
        for (int i = 0; i < agent.AbilityCount; i++)
        {
            var ability = agent.Ability(i);
            if (ability == null) continue;

            float fired = agent.AbilityFiredAt(i);
            if (fired < 0f) continue;

            float age = Time.time - fired;
            if (age < 0f || age > style.AbilityBurstSeconds) continue;

            float k = age / style.AbilityBurstSeconds;
            float radius = Mathf.Lerp(style.AbilityBurstRadiusFrom, style.AbilityBurstRadiusTo, Mathf.SmoothStep(0f, 1f, k));

            Color c = ability.Color;
            c.a = style.AbilityBurstAlpha * (1f - k) * alphaMul;
            CueDrawer.Ring(origin, radius, style.AbilityBurstThickness, c, true);
        }
    }

    private const float HoldEdgeThicknessBoost = 0.6f;
    private const float HoldFillBoost = 1.6f;
    private const float HoldRingScale = 1.15f;
    private const float HoldRingAlpha = 0.5f;
    private const float HoldDiveArcTailAlpha = 0.6f;

    public static void Telegraph(CueStyleSO style, MoriMonchiController controller, Vector3 origin, float alpha, float blink, float arcAlpha, Vector3 eye, float hold)
    {
        var agent = controller.Agent;
        var move = agent.ClashMove;
        if (move == null || alpha <= 0.01f) return;

        float k = agent.ClashTell01;
        bool striking = k >= 1f && agent.ClashTelegraphing;

        float appear = Mathf.Lerp(0.85f, 1f, Mathf.SmoothStep(0f, 1f, alpha));
        float pulse = striking ? 1f + style.TelegraphPulseAmount * Mathf.Sin(Time.time * style.TelegraphPulseSpeed) : 1f;

        var team = agent.Team;
        Color teamColor = team == ExpeditionTeam.Player ? style.FriendColor
                : team == ExpeditionTeam.Rival ? style.FoeColor
                : controller.DNA.BaseColor;

        Color edgeColor = style.FightColor;
        for (int i = 0; i < agent.AbilityCount; i++)
        {
            var ability = agent.Ability(i);
            if (ability == null || ability.Move != move) continue;
            edgeColor = ability.Color;
            break;
        }

        float edgeThickness = style.TelegraphEdgeThickness * (1f + HoldEdgeThicknessBoost * hold);
        float edgeBlend = Mathf.Lerp(style.TelegraphEdgeAlpha * blink, 1f, hold);

        float trackAlpha = style.TelegraphTrackAlpha * alpha;
        Color fill = teamColor;
        Color rim = edgeColor;
        rim.a = edgeBlend * alpha;
        float fillAlpha = style.TelegraphFillAlpha * alpha * Mathf.Lerp(0.55f, 1f, blink);
        fillAlpha = Mathf.Min(1f, Mathf.Lerp(fillAlpha, fillAlpha * HoldFillBoost, hold));
        float fillOuterAlpha = style.TelegraphFillOuterAlpha * alpha * Mathf.Lerp(0.55f, 1f, blink);

        float groundY = agent.IsAirborne ? agent.ClashImpactPoint.y : controller.transform.position.y;
        Vector3 impact = new Vector3(agent.ClashImpactPoint.x, groundY + style.HeightOffset, agent.ClashImpactPoint.z);
        Vector3 foot = new Vector3(origin.x, groundY + style.HeightOffset, origin.z);

        switch (move.Slot)
        {
            case ClashSlot.Horn:
            {
                float r = move.HitRadius * appear;
                Vector3 b = impact;
                Vector3 planar = b - foot;
                planar.y = 0f;
                if (planar.magnitude < 0.05f) b = foot + controller.transform.forward * 0.05f;

                CueDrawer.Capsule(foot, b, r, fill, trackAlpha, trackAlpha);
                if (k > 0.01f)
                    CueDrawer.Capsule(foot, Vector3.Lerp(foot, b, k), r, fill, fillAlpha, fillOuterAlpha);
                CueDrawer.CapsuleOutline(foot, b, r * pulse, edgeThickness, rim, rim, true);
                if (hold > 0.01f)
                {
                    Color clic = edgeColor;
                    clic.a = HoldRingAlpha * hold;
                    CueDrawer.CapsuleOutline(foot, b, r * pulse * HoldRingScale, style.TelegraphEdgeThickness, clic, clic, true);
                }
                break;
            }
            case ClashSlot.Back:
            {
                float R = move.SweepRadius * appear;
                CueDrawer.Disc(foot, R, fill, trackAlpha, trackAlpha * 0.5f);
                if (k > 0.01f)
                    CueDrawer.Disc(foot, R * k, fill, fillAlpha, fillOuterAlpha);
                CueDrawer.Ring(foot, R * pulse, edgeThickness, rim, true);
                if (hold > 0.01f)
                {
                    Color clic = edgeColor;
                    clic.a = HoldRingAlpha * hold;
                    CueDrawer.Ring(foot, R * pulse * HoldRingScale, style.TelegraphEdgeThickness, clic, true);
                }
                break;
            }
            case ClashSlot.Wings:
            {
                float r = move.HitRadius * appear;
                CueDrawer.Disc(impact, r, fill, trackAlpha, trackAlpha * 0.5f);
                if (k > 0.01f)
                    CueDrawer.Disc(impact, r * k, fill, fillAlpha, fillOuterAlpha);

                float ringR = Mathf.Lerp(r * style.TelegraphRingScale, r, Mathf.SmoothStep(0f, 1f, k)) * pulse;
                Color closing = rim;
                closing.a = rim.a * Mathf.Lerp(0.35f, 1f, k);
                CueDrawer.Ring(impact, ringR, edgeThickness, closing, true);
                if (hold > 0.01f)
                {
                    Color clic = edgeColor;
                    clic.a = HoldRingAlpha * hold;
                    CueDrawer.Ring(impact, ringR * HoldRingScale, style.TelegraphEdgeThickness, clic, true);
                }

                if (arcAlpha > 0.01f)
                {
                    Vector3 start = controller.transform.position + Vector3.up * style.DiveArcStartHeight;
                    Vector3 flat = impact - start;
                    flat.y = 0f;
                    float span = flat.magnitude;
                    if (span > 0.05f)
                    {
                        float apex = span * Mathf.Tan(move.LaunchAngle * Mathf.Deg2Rad) * 0.25f * style.DiveArcHeightScale;
                        Color tail = teamColor;
                        tail.a = Mathf.Lerp(style.DiveArcTailAlpha, HoldDiveArcTailAlpha, hold) * arcAlpha;
                        Color head = edgeColor;
                        head.a = edgeBlend * arcAlpha;
                        CueRibbonDrawer.Arc(start, impact, apex, style.DiveArcWidth, style.DiveArcTailScale, style.DiveArcHeadWidth, style.DiveArcHeadLength, style.DiveArcSamples, style.DiveArcDashLength, style.DiveArcDashGap, Time.time * style.DiveArcFlowSpeed, tail, head, eye, true);
                    }
                }
                break;
            }
        }
    }
}
}
