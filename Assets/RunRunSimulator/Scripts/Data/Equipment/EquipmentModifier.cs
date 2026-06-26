using System;
using Sirenix.OdinInspector;
namespace MoriMonchiSimulator
{

// One configured tier of a modifier effect. Lives inside EquipmentModifierDatabaseSO;
// equipment never stores these directly — it stores an EquipmentModifierRef (Kind+Tier)
// and resolves against the database. Fields are generic so one shape covers every kind:
// Magnitude = damage returned / heal amount / damage-per-turn; DurationTurns + Status
// only matter for ApplyStatus. Closed and declarative for async JS parity.
[Serializable]
public struct ModifierTierDef
{
    [HorizontalGroup("h"), LabelText("Label"), LabelWidth(50)] public string Label;
    [LabelWidth(110)] public float Magnitude;
    [LabelWidth(110)] public int DurationTurns;
    [LabelWidth(110)] public StatusEffect Status;
}

// The light reference an EquipmentSO stores: which effect, which tier (index into the
// kind's tier list in EquipmentModifierDatabaseSO). Serializes as a tiny (enum, int)
// pair — cloud-safe and JS-mirrorable, same spirit as storing an equipment ID instead
// of the whole asset.
[Serializable]
public struct EquipmentModifierRef
{
    [HorizontalGroup, HideLabel] public ModifierEffectKind Kind;
    [HorizontalGroup(90), LabelText("Tier"), LabelWidth(30)] public ModifierTier Tier;
}
}
