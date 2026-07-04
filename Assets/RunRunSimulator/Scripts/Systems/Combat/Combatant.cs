using System.Collections.Generic;
using UnityEngine;

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

    public float EffDefense => Defense + StackSum(ModifierEffectKind.Steel);
    public float EffEvasion => Evasion + StackSum(ModifierEffectKind.Mist);
    public float EffSpeed   => Mathf.Max(0f, Speed - StackSum(ModifierEffectKind.Static));
    public float LifestealPercent => Mathf.Min(1f, StackSum(ModifierEffectKind.Lifesteal) / 100f);

    private float StackSum(ModifierEffectKind kind)
    {
        float sum = 0f;
        foreach (var a in Active)
            if (a.Kind == kind) sum += a.Magnitude;
        return sum;
    }
}

public class ActiveEffect
{
    public ModifierEffectKind Kind;
    public int RemainingTurns;
    public int Magnitude;
}
}
