using Sirenix.OdinInspector;
namespace MoriMonchiSimulator
{

// Efectos que aplica una SynergyRule sobre el portador al detonar: la regla ya quemó
// los stacks que la dispararon, estos leaves solo aportan el payload extra (daño/cura/
// estado/stun). Autorables inline en un único SynergyTableSO.
public abstract class SynergyEffectBase
{
    public abstract void Apply(CombatResolver r, Combatant bearer);
    public abstract string Summary();
}

public class SynergyDamageEffect : SynergyEffectBase
{
    [MinValue(0)]
    public float Amount = 10f;

    public override void Apply(CombatResolver r, Combatant bearer) => r.DamageBearer(bearer, Amount, "synergy");
    public override string Summary() => $"deals {Amount} damage to the bearer";
}

public class SynergyHealEffect : SynergyEffectBase
{
    [MinValue(0)]
    public float Amount = 10f;

    public override void Apply(CombatResolver r, Combatant bearer) => r.HealBearer(bearer, Amount, "synergy");
    public override string Summary() => $"heals the bearer {Amount} HP";
}

public class SynergyStatusEffect : SynergyEffectBase
{
    public ModifierEffectKind Kind = ModifierEffectKind.Burn;

    [MinValue(1)]
    public int Turns = 3;

    [MinValue(1)]
    public int Magnitude = 2;

    public override void Apply(CombatResolver r, Combatant bearer) => r.AddStatusTo(bearer, Kind, Turns, Magnitude, "synergy");
    public override string Summary() => $"applies {Kind} {Magnitude}/turn for {Turns}t to the bearer";
}

public class SynergyStunEffect : SynergyEffectBase
{
    [PropertyRange(1, 10)]
    public int Turns = 1;

    public override void Apply(CombatResolver r, Combatant bearer) => r.StunBearer(bearer, Turns);
    public override string Summary() => $"stuns the bearer for {Turns} turn(s)";
}
}
