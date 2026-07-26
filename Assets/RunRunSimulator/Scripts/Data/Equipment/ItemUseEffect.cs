using System;
using Sirenix.OdinInspector;
namespace MoriMonchiSimulator
{

// Active item effects with N uses per combat and a trigger rule (no probability
// rolls). Remaining uses live in Combatant at runtime, not here; these classes
// are serialized templates inside EquipmentSO.Effects. Consumed by
// Combatant/CombatService, emitting actions via ICombatContext without
// mutating state directly.
public enum UseRule
{
    Always = 0,
    SelfHpBelow = 1
}

[Serializable]
public abstract class ItemUseEffect : EquipmentEffectBase
{
    [PropertyRange(1, 10), LabelText("Uses")]
    public int Uses = 3;

    [LabelText("Rule")]
    public UseRule Rule = UseRule.Always;

    [ShowIf("Rule", UseRule.SelfHpBelow), PropertyRange(1, 100), LabelText("HP below (%)")]
    public int HpThresholdPercent = 50;

    public abstract void Apply(ICombatContext ctx);

    protected string RuleTag => Rule == UseRule.SelfHpBelow ? $"HP<{HpThresholdPercent}%" : "always";
}

[Serializable]
public class HealUseEffect : ItemUseEffect
{
    [MinValue(0), LabelText("Heal (flat HP)")]
    public int Amount = 5;

    public override void Apply(ICombatContext ctx)
    {
        ctx.HealSelf(Amount, "item");
    }

    public override string Summary()
    {
        return Loc.Tr("effect.item_heal.summary", Uses, RuleTag, Amount);
    }
}

[Serializable]
public class DamageUseEffect : ItemUseEffect
{
    [MinValue(0), LabelText("Damage (flat)")]
    public int Amount = 5;

    public override void Apply(ICombatContext ctx)
    {
        ctx.DamageOpponent(Amount, "item");
    }

    public override string Summary()
    {
        return Loc.Tr("effect.item_damage.summary", Uses, RuleTag, Amount);
    }
}
}
