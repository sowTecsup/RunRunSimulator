using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
namespace MoriMonchiSimulator
{

[Serializable]
public abstract class EquipmentEffectBase
{
    public abstract string Summary();
}

[Serializable]
public class StatModifierEffect : EquipmentEffectBase
{
    [ListDrawerSettings(ShowFoldout = false, DefaultExpandedState = true)]
    public List<StatModifier> Modifiers = new List<StatModifier>();

    public override string Summary()
    {
        if (Modifiers == null || Modifiers.Count == 0) return Loc.Tr("effect.stat_modifier.none");
        return string.Join(", ", Modifiers.Select(m =>
        {
            string sign = m.Value >= 0 ? "+" : "";
            string unit = m.Type == ModifierType.Flat ? "" : "%";
            return $"{sign}{m.Value:0.##}{unit} {LocEnumMaps.StatAbbrev(m.Stat)}";
        }));
    }
}
}
