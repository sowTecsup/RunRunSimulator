using System;
using Sirenix.OdinInspector;
namespace MoriMonchiSimulator
{

[Serializable]
public abstract class CombatProcEffect : EquipmentEffectBase
{
    [LabelText("Trigger")]
    public TriggerType Trigger = TriggerType.Offensive;

    [PropertyRange(0, 100), LabelText("Proc %")]
    public int ProcChance = 100;

    public abstract ModifierEffectKind Kind { get; }

    public abstract void Apply(ICombatContext ctx);

    protected string TriggerTag => Trigger switch
    {
        TriggerType.Offensive => "on hit",
        TriggerType.Defensive => "when hit",
        TriggerType.Passive   => "passive",
        _                     => "",
    };
}

[Serializable]
public class ReturnDamageEffect : CombatProcEffect
{
    [MinValue(0), LabelText("Reflect (flat)")]
    public int Amount = 5;

    public override ModifierEffectKind Kind => ModifierEffectKind.ReturnDamage;
    public override void Apply(ICombatContext ctx) => ctx.DamageOpponent(Amount, "thorns");
    public override string Summary() => $"[{TriggerTag}] reflects {Amount} damage";
}

[Serializable]
public class HealEffect : CombatProcEffect
{
    [MinValue(0), LabelText("Heal (flat HP)")]
    public int Amount = 5;

    public override ModifierEffectKind Kind => ModifierEffectKind.Heal;
    public override void Apply(ICombatContext ctx) => ctx.HealSelf(Amount, "heal");
    public override string Summary() => $"[{TriggerTag}] heals {Amount} HP";
}

[Serializable]
public class StunEffect : CombatProcEffect
{
    [PropertyRange(1, 10), LabelText("Duration (turns)")]
    public int DurationTurns = 1;

    public override ModifierEffectKind Kind => ModifierEffectKind.Stun;
    public override void Apply(ICombatContext ctx) => ctx.StunOpponent(DurationTurns);
    public override string Summary() => $"[{TriggerTag}] stuns {DurationTurns} turn(s)";
}

[Serializable]
public abstract class PeriodicProcEffect : CombatProcEffect
{
    [PropertyRange(1, 10), LabelText("Duration (turns)")]
    public int DurationTurns = 3;

    [MinValue(0), LabelText("Per turn (flat)")]
    public int Magnitude = 3;
}

[Serializable]
public class PoisonEffect : PeriodicProcEffect
{
    public override ModifierEffectKind Kind => ModifierEffectKind.Poison;
    public override void Apply(ICombatContext ctx) => ctx.ApplyStatusToOpponent(ModifierEffectKind.Poison, DurationTurns, Magnitude, "poison");
    public override string Summary() => $"[{TriggerTag}] poison {Magnitude}/turn for {DurationTurns} turn(s)";
}

[Serializable]
public class BurnEffect : PeriodicProcEffect
{
    public override ModifierEffectKind Kind => ModifierEffectKind.Burn;
    public override void Apply(ICombatContext ctx) => ctx.ApplyStatusToOpponent(ModifierEffectKind.Burn, DurationTurns, Magnitude, "burn");
    public override string Summary() => $"[{TriggerTag}] burn {Magnitude}/turn for {DurationTurns} turn(s)";
}

[Serializable]
public class RegenEffect : PeriodicProcEffect
{
    public override ModifierEffectKind Kind => ModifierEffectKind.Regen;
    public override void Apply(ICombatContext ctx) => ctx.ApplyStatusToSelf(ModifierEffectKind.Regen, DurationTurns, Magnitude, "regen");
    public override string Summary() => $"[{TriggerTag}] regen {Magnitude}/turn for {DurationTurns} turn(s)";
}
}
