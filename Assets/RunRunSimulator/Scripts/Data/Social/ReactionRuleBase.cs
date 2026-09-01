using System;
using UnityEngine;
namespace MoriMonchiSimulator
{

[Serializable]
public abstract class ReactionRuleBase
{
    [Min(0f)] public float Cooldown = 10f;

    public abstract SocialAction Action { get; }

    public abstract bool Matches(in Percept p, MoriMochiAgent self, SocialTuningSO tuning, out float score);

    public abstract string Summary();

    protected static bool TargetFree(in Percept p)
    {
        var monchi = p.Source != null ? p.Source.Monchi : null;
        if (monchi == null) return false;
        return !monchi.IsHeld && !monchi.IsAirborne && !monchi.IsCourting && !monchi.IsSocializing && !monchi.IsPenned;
    }
}

public enum SocialAction
{
    Approach = 0,
    Avoid = 1,
    PlayChase = 2,
    SleepTogether = 3,
    Fight = 4,
}

[Serializable]
public class ApproachFriendRule : ReactionRuleBase
{
    [Range(-1f, 1f)] public float MinAffinity = 0.25f;

    public override SocialAction Action => SocialAction.Approach;

    public override bool Matches(in Percept p, MoriMochiAgent self, SocialTuningSO tuning, out float score)
    {
        score = 0f;
        if (p.Kind != PerceivableKind.Monchi) return false;
        float minAff = MinAffinity - SocialTuningSO.DialShift(self.DNA != null ? self.DNA.Sociability : 0.5f, tuning.SociabilityAffinityShift);
        if (p.Affinity < minAff) return false;
        if (!TargetFree(p)) return false;
        score = p.Affinity;
        return true;
    }

    public override string Summary() => $"Se acerca si afinidad >= {MinAffinity:0.##}";
}

[Serializable]
public class AvoidDislikedRule : ReactionRuleBase
{
    [Range(-1f, 1f)] public float MaxAffinity = -0.3f;

    public override SocialAction Action => SocialAction.Avoid;

    public override bool Matches(in Percept p, MoriMochiAgent self, SocialTuningSO tuning, out float score)
    {
        score = 0f;
        if (p.Kind != PerceivableKind.Monchi) return false;
        float maxAff = MaxAffinity - SocialTuningSO.DialShift(self.DNA != null ? self.DNA.Boldness : 0.5f, tuning.BoldnessAvoidShift);
        if (p.Affinity > maxAff) return false;
        score = -p.Affinity;
        return true;
    }

    public override string Summary() => $"Evita si afinidad <= {MaxAffinity:0.##}";
}

[Serializable]
public class PlayChaseRule : ReactionRuleBase
{
    [Range(-1f, 1f)] public float MinAffinity = 0.35f;
    [Min(0f)] public float PriorityBonus = 0.15f;

    public override SocialAction Action => SocialAction.PlayChase;

    public override bool Matches(in Percept p, MoriMochiAgent self, SocialTuningSO tuning, out float score)
    {
        score = 0f;
        if (p.Kind != PerceivableKind.Monchi) return false;
        float minAff = MinAffinity - SocialTuningSO.DialShift(self.DNA != null ? self.DNA.Sociability : 0.5f, tuning.SociabilityAffinityShift);
        if (p.Affinity < minAff) return false;
        if (self.Condition != CreatureCondition.Healthy) return false;
        if (self.DNA.Needs.Energy < tuning.MinEnergyToPlay) return false;
        if (!TargetFree(p)) return false;

        var other = p.Source.Monchi;
        if (other.Condition != CreatureCondition.Healthy) return false;
        if (other.DNA.Needs.Energy < tuning.MinEnergyToPlay) return false;

        score = p.Affinity + PriorityBonus;
        return true;
    }

    public override string Summary() => $"Invita a jugar si afinidad >= {MinAffinity:0.##} y ambos tienen energia";
}

[Serializable]
public class SleepTogetherRule : ReactionRuleBase
{
    [Range(-1f, 1f)] public float MinAffinity = 0.3f;
    [Min(0f)] public float PriorityBonus = 0.2f;

    public override SocialAction Action => SocialAction.SleepTogether;

    public override bool Matches(in Percept p, MoriMochiAgent self, SocialTuningSO tuning, out float score)
    {
        score = 0f;
        if (p.Kind != PerceivableKind.Monchi) return false;
        float minAff = MinAffinity - SocialTuningSO.DialShift(self.DNA != null ? self.DNA.Sociability : 0.5f, tuning.SociabilityAffinityShift);
        if (p.Affinity < minAff) return false;
        if (!TargetFree(p)) return false;
        if (self.Condition == CreatureCondition.Sick) return false;
        if (self.DNA.Needs.Energy > tuning.MaxEnergyToSleep) return false;

        var other = p.Source.Monchi;
        if (other.Condition == CreatureCondition.Sick) return false;
        if (other.DNA.Needs.Energy > tuning.MaxEnergyToSleep) return false;

        score = p.Affinity + PriorityBonus;
        return true;
    }

    public override string Summary() => $"Duerme junto a otro si afinidad >= {MinAffinity:0.##} y ambos tienen sueno";
}

[Serializable]
public class GremlinFightRule : ReactionRuleBase
{
    [Range(-1f, 1f)] public float MaxAffinity = 0.05f;
    [Min(0f)] public float PriorityBonus = 0.15f;

    public override SocialAction Action => SocialAction.Fight;

    public override bool Matches(in Percept p, MoriMochiAgent self, SocialTuningSO tuning, out float score)
    {
        score = 0f;
        if (p.Kind != PerceivableKind.Monchi) return false;
        float maxAff = MaxAffinity + SocialTuningSO.DialShift(self.DNA != null ? self.DNA.Boldness : 0.5f, tuning.BoldnessFightShift);
        if (p.Affinity > maxAff) return false;
        if (!TargetFree(p)) return false;
        if (self.Condition != CreatureCondition.Healthy) return false;
        if (self.DNA.Needs.Energy < tuning.MinEnergyToPlay) return false;

        var other = p.Source.Monchi;
        if (other.Condition != CreatureCondition.Healthy) return false;
        if (other.DNA.Needs.Energy < tuning.MinEnergyToPlay) return false;

        score = -p.Affinity + PriorityBonus;
        return true;
    }

    public override string Summary() => $"Busca pelea de juego si afinidad <= {MaxAffinity:0.##} y ambos estan sanos";
}
}
