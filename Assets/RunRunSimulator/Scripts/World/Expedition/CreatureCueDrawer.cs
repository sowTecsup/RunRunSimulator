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

    public static void Clash(CueStyleSO style, MoriMonchiController controller, float alphaMul)
    {
        var target = controller.Agent.ClashTarget;
        if (target == null) return;

        Vector3 a = controller.transform.position + Vector3.up * style.HeightOffset;
        Vector3 b = target.transform.position + Vector3.up * style.HeightOffset;

        Color head = style.FightColor;
        head.a = (0.55f + 0.45f * Mathf.Sin(Time.time * style.FightPulseSpeed)) * alphaMul;
        Color tail = head;
        tail.a *= style.PathTailAlpha;

        CueDrawer.Arrow(a, b, style.PathThickness * 1.5f, style.HeadLength, style.HeadWidth, tail, head, true);
    }
}
}
