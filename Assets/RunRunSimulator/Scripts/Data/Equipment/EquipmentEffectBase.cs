using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
namespace MoriMonchiSimulator
{

// Base type for everything an EquipmentSO can do. Odin serializes the polymorphic
// list natively, so the inspector offers a "+" dropdown to mix passive stat
// modifiers and (Stage 2) combat procs freely inside one item. Each concrete
// effect is a closed, parameterized kind — no free-form logic — so the JS combat
// motor can mirror it for async parity.
[Serializable]
public abstract class EquipmentEffectBase
{
    public abstract string Summary();
}

// Passive stat changes: +10 ATK / -5 SPD, +10 DEF, etc. Carries one or more
// StatModifier. No combat hook — applied to the stat pipeline.
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
