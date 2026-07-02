using System.Collections.Generic;

namespace MoriMonchiSimulator
{
// Modelo mutable de un combatiente durante la simulación de combate,
// junto con sus estados activos (buffs/debuffs/stun) en curso.

public class Combatant
{
    public CreatureDNA Dna;
    public string      Name;
    public bool        IsA;
    public float       Hp;
    public float       MaxHp;
    public float       Attack;
    public float       Speed;
    public float       Defense;
    public float       Luck;
    public float       Evasion;
    public int         StunTurns;
    public int         StunImmunityTurns;
    public List<CombatProcEffect> Procs  = new List<CombatProcEffect>();
    public List<ActiveEffect>     Active = new List<ActiveEffect>();
}

public class ActiveEffect
{
    public ModifierEffectKind Kind;
    public int RemainingTurns;
    public int Magnitude;
}
}
