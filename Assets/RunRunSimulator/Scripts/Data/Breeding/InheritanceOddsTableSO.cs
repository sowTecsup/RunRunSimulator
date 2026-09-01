using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "InheritanceOddsTable", menuName = "RunRunSimulator/Breeding/Inheritance Odds Table")]
public class InheritanceOddsTableSO : SerializedScriptableObject
{
    public enum Slot { Parent, Grandparent, GreatGrandparent, Mutation, Base }

    [InfoBox("Relative weights — normalized internally.")]
    [LabelWidth(190)] public float ParentWeight           = 40f;
    [LabelWidth(190)] public float GrandparentWeight      = 20f;
    [LabelWidth(190)] public float GreatGrandparentWeight = 10f;
    [LabelWidth(190)] public float MutationWeight         = 20f;
    [LabelWidth(190)] public float BaseWeight             = 10f;

    [ShowInInspector, ReadOnly, LabelWidth(190)]
    private float TotalWeight =>
        ParentWeight + GrandparentWeight + GreatGrandparentWeight + MutationWeight + BaseWeight;

    [InfoBox("Solo display/referencia. La duración real está hardcoded en start-breeding.js (server-side).")]
    [LabelWidth(190)] public int BreedDurationMinutes = 30;

    [LabelWidth(190), Range(0f, 1f)] public float ElementMutationChance = 0.10f;

    public enum DialSlot { Average, Copy, Mutation }

    [LabelWidth(190)] public float DialAverageWeight  = 50f;
    [LabelWidth(190)] public float DialCopyWeight     = 30f;
    [LabelWidth(190)] public float DialMutationWeight = 20f;
    [LabelWidth(190), Range(0f, 0.2f)] public float DialJitter = 0.05f;

    public DialSlot RollDial()
    {
        float total = DialAverageWeight + DialCopyWeight + DialMutationWeight;
        if (total <= 0f) return DialSlot.Average;
        float roll = UnityEngine.Random.Range(0f, total);
        if (roll < DialAverageWeight) return DialSlot.Average;
        if (roll < DialAverageWeight + DialCopyWeight) return DialSlot.Copy;
        return DialSlot.Mutation;
    }

    public Slot Roll()
    {
        float total = TotalWeight;
        if (total <= 0f) return Slot.Parent;

        float roll = UnityEngine.Random.Range(0f, total);
        float c    = 0f;

        c += ParentWeight;            if (roll < c) return Slot.Parent;
        c += GrandparentWeight;       if (roll < c) return Slot.Grandparent;
        c += GreatGrandparentWeight;  if (roll < c) return Slot.GreatGrandparent;
        c += MutationWeight;          if (roll < c) return Slot.Mutation;
        return Slot.Base;
    }
}
}
