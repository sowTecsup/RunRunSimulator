using System;
using System.Collections.Generic;
namespace MoriMonchiSimulator
{
[Serializable]
public class CombatProcEvent
{
    public ModifierEffectKind Kind;
    public bool               TargetIsA;
    public int                TargetIndex;
    public float              Amount;
    public float              TargetHpAfter;
    public bool               BeforeStrike;
    public List<CombatStatusMark> TargetStatusAfter;
}
}
