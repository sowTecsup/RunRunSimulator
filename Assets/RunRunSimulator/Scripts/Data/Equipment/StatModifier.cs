using System;
using Sirenix.OdinInspector;
namespace MoriMonchiSimulator
{

// One atomic stat change: which stat, by how much, and how it stacks. The combat
// stat pipeline (Stage 2) aggregates a list of these per stat: Flat first, then
// PercentAdd summed, then each PercentMult compounded.
[Serializable]
public struct StatModifier
{
    [HorizontalGroup, HideLabel] public StatType    Stat;
    [HorizontalGroup, HideLabel] public ModifierType Type;
    [HorizontalGroup, HideLabel] public float        Value;
}
}
