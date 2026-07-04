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

public enum ProcTarget { Opponent, Self }

[Serializable]
public class StaticEffect : CombatProcEffect
{
    [LabelText("Target")]
    public ProcTarget Target = ProcTarget.Opponent;

    [PropertyRange(1, 10), LabelText("Duration (turns)")]
    public int DurationTurns = 3;

    [MinValue(0), LabelText("-SPD (points)")]
    public int Magnitude = 1;

    public override ModifierEffectKind Kind => ModifierEffectKind.Static;
    public override void Apply(ICombatContext ctx)
    {
        if (Target == ProcTarget.Self) ctx.ApplyStatusToSelf(ModifierEffectKind.Static, DurationTurns, Magnitude, "static");
        else ctx.ApplyStatusToOpponent(ModifierEffectKind.Static, DurationTurns, Magnitude, "static");
    }
    public override string Summary() => $"[{TriggerTag}] static -{Magnitude} SPD for {DurationTurns} turn(s) on {(Target == ProcTarget.Self ? "self" : "opponent")}";
}

[Serializable]
public class PulseEffect : CombatProcEffect
{
    [LabelText("Target")]
    public ProcTarget Target = ProcTarget.Self;

    [PropertyRange(1, 10), LabelText("Duration (turns)")]
    public int DurationTurns = 3;

    [MinValue(0), LabelText("Heal per turn (flat)")]
    public int Magnitude = 2;

    public override ModifierEffectKind Kind => ModifierEffectKind.Pulse;
    public override void Apply(ICombatContext ctx)
    {
        if (Target == ProcTarget.Self) ctx.ApplyStatusToSelf(ModifierEffectKind.Pulse, DurationTurns, Magnitude, "pulse");
        else ctx.ApplyStatusToOpponent(ModifierEffectKind.Pulse, DurationTurns, Magnitude, "pulse");
    }
    public override string Summary() => $"[{TriggerTag}] pulse {Magnitude} HP/turn for {DurationTurns} turn(s) on {(Target == ProcTarget.Self ? "self" : "opponent")}";
}

[Serializable]
public class SteelEffect : CombatProcEffect
{
    [LabelText("Target")]
    public ProcTarget Target = ProcTarget.Self;

    [PropertyRange(1, 10), LabelText("Duration (turns)")]
    public int DurationTurns = 3;

    [MinValue(0), LabelText("+DEF (points)")]
    public int Magnitude = 1;

    public override ModifierEffectKind Kind => ModifierEffectKind.Steel;
    public override void Apply(ICombatContext ctx)
    {
        if (Target == ProcTarget.Self) ctx.ApplyStatusToSelf(ModifierEffectKind.Steel, DurationTurns, Magnitude, "steel");
        else ctx.ApplyStatusToOpponent(ModifierEffectKind.Steel, DurationTurns, Magnitude, "steel");
    }
    public override string Summary() => $"[{TriggerTag}] steel +{Magnitude} DEF for {DurationTurns} turn(s) on {(Target == ProcTarget.Self ? "self" : "opponent")}";
}

[Serializable]
public class MistEffect : CombatProcEffect
{
    [LabelText("Target")]
    public ProcTarget Target = ProcTarget.Self;

    [PropertyRange(1, 10), LabelText("Duration (turns)")]
    public int DurationTurns = 3;

    [MinValue(0), LabelText("+EVA (points)")]
    public int Magnitude = 1;

    public override ModifierEffectKind Kind => ModifierEffectKind.Mist;
    public override void Apply(ICombatContext ctx)
    {
        if (Target == ProcTarget.Self) ctx.ApplyStatusToSelf(ModifierEffectKind.Mist, DurationTurns, Magnitude, "mist");
        else ctx.ApplyStatusToOpponent(ModifierEffectKind.Mist, DurationTurns, Magnitude, "mist");
    }
    public override string Summary() => $"[{TriggerTag}] mist +{Magnitude} EVA for {DurationTurns} turn(s) on {(Target == ProcTarget.Self ? "self" : "opponent")}";
}
}
