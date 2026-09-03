using System;
using UnityEngine;
namespace MoriMonchiSimulator
{

public enum ExpeditionGoal
{
    SeekMaterial = 0,
}

[Serializable]
public abstract class ExpeditionRuleBase
{
    public abstract ExpeditionGoal Goal { get; }

    public abstract bool Matches(in Percept p, MoriMochiAgent self, ExpeditionRulesSO rules, out float score);

    public abstract string Summary();
}

[Serializable]
public class SeekMaterialRule : ExpeditionRuleBase
{
    [Min(0f)] public float MaxDistance = 0f;
    [Range(-1f, 1f)] public float BoldnessBias = 0f;

    public override ExpeditionGoal Goal => ExpeditionGoal.SeekMaterial;

    public override bool Matches(in Percept p, MoriMochiAgent self, ExpeditionRulesSO rules, out float score)
    {
        score = 0f;
        if (p.Kind != PerceivableKind.Material) return false;
        if (p.Source == null) return false;
        if (!p.Source.gameObject.activeInHierarchy) return false;

        float dist = Mathf.Sqrt(p.SqrDistance);
        if (MaxDistance > 0f && dist > MaxDistance) return false;

        float boldnessFactor = self.DNA != null ? 1f + BoldnessBias * (self.DNA.Boldness - 0.5f) * 2f : 1f;
        score = (1f / (1f + dist)) * boldnessFactor;
        return true;
    }

    public override string Summary()
    {
        string maxDist = MaxDistance > 0f ? $"<= {MaxDistance:0.##}" : "sin limite";
        return $"Busca material a distancia {maxDist} (sesgo osadia {BoldnessBias:0.##})";
    }
}
}
