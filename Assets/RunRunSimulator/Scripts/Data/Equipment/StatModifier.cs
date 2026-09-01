using System;
using Sirenix.OdinInspector;
namespace MoriMonchiSimulator
{

[Serializable]
public struct StatModifier
{
    [HorizontalGroup, HideLabel] public StatType    Stat;
    [HorizontalGroup, HideLabel] public ModifierType Type;
    [HorizontalGroup, HideLabel] public float        Value;
}
}
