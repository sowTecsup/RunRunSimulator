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
    public Role        Role;
    public CombatRow   Row;
    public int         Index;
    public float       Hp;
    public float       MaxHp;
    public float       Shield;
    public float       Attack;
    public float       Speed;
    public float       Defense;
    public float       Luck;
    public float       Evasion;
    public int         StunTurns;
    public int         StunImmunityTurns;
    public Element     Element;
    public int         Affinity;
    public int         Energy;
    public List<ItemUseState>     Uses   = new List<ItemUseState>();
    public List<ActiveEffect>     Active = new List<ActiveEffect>();
    public List<ElementMark>      Marks  = new List<ElementMark>();
    public List<ElementalState>   States = new List<ElementalState>();

    public bool  IsAlive    => Hp > 0f;
    public bool  HasState(ElementalState s) => States.Contains(s);
    public bool  ConsumeState(ElementalState s) => States.Remove(s);
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

public class ItemUseState
{
    public ItemUseEffect Effect;
    public int Remaining;
}
}
