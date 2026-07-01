using System;
namespace MoriMonchiSimulator
{
[Serializable]
public class CombatProcEvent
{
    public ModifierEffectKind Kind;
    public bool               TargetIsA;
    public float              Amount;
    public float              TargetHpAfter;
    public bool               BeforeStrike;
}
}
