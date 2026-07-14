using System;
using Sirenix.OdinInspector;
namespace MoriMonchiSimulator
{

// Elemental reaction effects: triggered when two marks of the same source
// but different Elements land on the same bearer (see CombatElements.AddMark).
// Serialized templates inside ElementReaction.Effects, executed in order —
// same rng consumption and log strings as the values they replace.
[Serializable]
public abstract class ReactionEffectBase
{
    public abstract void Apply(Combatant bearer, Combatant reactor, string reactionName, CombatResult result, CombatResolver r, CombatRng rng);

    public abstract string Summary();
}

[Serializable]
public class ArmStateEffect : ReactionEffectBase
{
    [LabelText("State")]
    public ElementalState State;

    public override void Apply(Combatant bearer, Combatant reactor, string reactionName, CombatResult result, CombatResolver r, CombatRng rng)
    {
        if (bearer.States.Contains(State))
        {
            result.Log.Add($"    [estado] {bearer.Name} ya tiene {State} — sin efecto");
            return;
        }

        bearer.States.Add(State);
        result.Log.Add($"    [estado] {bearer.Name} queda {State} (armado)");
        r.RecordElement(ElementEventKind.StateArmed, bearer, state: State, reactionName: reactionName);
    }

    public override string Summary()
    {
        return $"Arma el estado {State} sobre el portador";
    }
}

[Serializable]
public class CleanseEffect : ReactionEffectBase
{
    [PropertyRange(0f, 1f), LabelText("Heal % (sin negativos)")]
    public float HealPercent = 0.20f;

    public override void Apply(Combatant bearer, Combatant reactor, string reactionName, CombatResult result, CombatResolver r, CombatRng rng)
    {
        ElementalState? negative = null;
        foreach (var s in bearer.States)
        {
            if (CombatElements.IsNegative(s)) { negative = s; break; }
        }

        if (negative != null)
        {
            bearer.States.Remove(negative.Value);
            result.Log.Add($"    [estado] {reactionName} purga {negative.Value} de {bearer.Name}");
            r.RecordElement(ElementEventKind.StateRemoved, bearer, state: negative.Value, reactionName: reactionName);
        }
        else
        {
            float heal = bearer.MaxHp * HealPercent;
            float before = bearer.Hp;
            bearer.Hp = System.Math.Min(bearer.MaxHp, bearer.Hp + heal);
            float healed = bearer.Hp - before;
            result.Log.Add($"    [estado] {reactionName} cura a {bearer.Name} +{healed:F1} → {bearer.Hp:F1}");
            r.RecordElement(ElementEventKind.Heal, bearer, amount: healed, reactionName: reactionName);
        }
    }

    public override string Summary()
    {
        return $"Purga el primer estado negativo o cura {HealPercent:P0} de MaxHp";
    }
}

[Serializable]
public class DoubleShieldEffect : ReactionEffectBase
{
    public override void Apply(Combatant bearer, Combatant reactor, string reactionName, CombatResult result, CombatResolver r, CombatRng rng)
    {
        bearer.Shield *= 2f;
        result.Log.Add($"    [estado] {reactionName} duplica el escudo de {bearer.Name} → {bearer.Shield:F1}");
        r.RecordElement(ElementEventKind.ShieldDoubled, bearer, amount: bearer.Shield, reactionName: reactionName);
    }

    public override string Summary()
    {
        return "Duplica el escudo actual del portador";
    }
}

[Serializable]
public class LeechEffect : ReactionEffectBase
{
    [MinValue(0), LabelText("Drain (flat HP)")]
    public float Amount = 4f;

    public override void Apply(Combatant bearer, Combatant reactor, string reactionName, CombatResult result, CombatResolver r, CombatRng rng)
    {
        float drained = System.Math.Min(bearer.Hp, Amount);
        bearer.Hp = System.Math.Max(0f, bearer.Hp - Amount);
        if (reactor != null)
        {
            float before = reactor.Hp;
            reactor.Hp = System.Math.Min(reactor.MaxHp, reactor.Hp + drained);
            result.Log.Add($"    [estado] {reactionName} drena {drained:F1} de {bearer.Name} → {bearer.Hp:F1} y cura {reactor.Name} +{reactor.Hp - before:F1} → {reactor.Hp:F1}");
            r.RecordElement(ElementEventKind.Damage, bearer, amount: drained, reactionName: reactionName);
            r.RecordElement(ElementEventKind.Heal, reactor, amount: reactor.Hp - before, reactionName: reactionName);
        }
        else
        {
            result.Log.Add($"    [estado] {reactionName} drena {drained:F1} de {bearer.Name} → {bearer.Hp:F1}");
            r.RecordElement(ElementEventKind.Damage, bearer, amount: drained, reactionName: reactionName);
        }
    }

    public override string Summary()
    {
        return $"Drena {Amount:F0} HP del portador y cura al reactor";
    }
}

[Serializable]
public class RemoveRandomMarkEffect : ReactionEffectBase
{
    public override void Apply(Combatant bearer, Combatant reactor, string reactionName, CombatResult result, CombatResolver r, CombatRng rng)
    {
        if (bearer.Marks.Count == 0)
        {
            result.Log.Add($"    [estado] {reactionName} sobre {bearer.Name} — sin marcas que remover");
            return;
        }

        int idx = rng.Range(0, bearer.Marks.Count);
        var removed = bearer.Marks[idx];
        bearer.Marks.RemoveAt(idx);
        result.Log.Add($"    [estado] {reactionName} remueve marca {removed.Element} ({(removed.AllySource ? "aliada" : "enemiga")}) de {bearer.Name}");
        r.RecordElement(ElementEventKind.MarkRemoved, bearer, element: removed.Element, allySource: removed.AllySource, reactionName: reactionName);
    }

    public override string Summary()
    {
        return "Remueve una marca elemental aleatoria del portador";
    }
}

[Serializable]
public class HealEffect : ReactionEffectBase
{
    [MinValue(0), LabelText("Heal (flat HP)")]
    public float Amount = 5f;

    public override void Apply(Combatant bearer, Combatant reactor, string reactionName, CombatResult result, CombatResolver r, CombatRng rng)
    {
        float before = bearer.Hp;
        bearer.Hp = System.Math.Min(bearer.MaxHp, bearer.Hp + Amount);
        result.Log.Add($"    [estado] {reactionName} cura a {bearer.Name} +{bearer.Hp - before:F1} → {bearer.Hp:F1}");
        r.RecordElement(ElementEventKind.Heal, bearer, amount: bearer.Hp - before, reactionName: reactionName);
    }

    public override string Summary()
    {
        return $"Cura {Amount:F0} HP planos al portador";
    }
}

[Serializable]
public class DamageEffect : ReactionEffectBase
{
    [MinValue(0), LabelText("Damage (flat)")]
    public float Amount = 5f;

    public override void Apply(Combatant bearer, Combatant reactor, string reactionName, CombatResult result, CombatResolver r, CombatRng rng)
    {
        bearer.Hp = System.Math.Max(0f, bearer.Hp - Amount);
        result.Log.Add($"    [estado] {reactionName} daña a {bearer.Name} -{Amount:F1} → {bearer.Hp:F1}");
        r.RecordElement(ElementEventKind.Damage, bearer, amount: Amount, reactionName: reactionName);
    }

    public override string Summary()
    {
        return $"Inflige {Amount:F0} de daño plano al portador";
    }
}

}
