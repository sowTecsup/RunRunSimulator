using UnityEngine;

namespace MoriMonchiSimulator
{

// Outcome of a single basic-attack strike: dodge/crit rolls, on-connect
// damage math (DEF reduction, Sudden Death round multiplier, Boiling,
// Shield absorption, Charcoal reflect), the enemy-sourced elemental mark
// and the DODGE/CRIT log line.
public class StrikeOutcome
{
    public bool  Dodged;
    public bool  Crit;
    public float Damage;
    public float DefenderHpAfter;
    public float DefenderShieldAfter;
}

public static class CombatStrike
{
    public static StrikeOutcome Execute(Combatant actor, Combatant target, CombatManagerSO config, CombatResult result, CombatResolver r, CombatRng rng)
    {
        float evaChance = target.EffEvasion * config.EvasionPerPoint;
        evaChance      += target.HasState(ElementalState.Vaporizado) ? (config.Elements != null ? config.Elements.StatePercent(ElementalState.Vaporizado) : 0f) : 0f;
        float evaRoll   = rng.NextFloat();
        bool  dodged    = evaRoll < evaChance;
        if (dodged && target.ConsumeState(ElementalState.Vaporizado))
        {
            result.Log.Add($"    [estado] Vaporizado consumido — {target.Name} esquivó");
            r.RecordElement(ElementEventKind.StateConsumed, target, state: ElementalState.Vaporizado);
        }
        bool  crit        = false;
        float critChance  = 0f;
        float critRoll    = 1f;
        float damage      = 0f;
        float absorbed    = 0f;
        float suddenDeath = 1f;
        if (!dodged)
        {
            critChance      = config.CritChance + actor.Luck * config.LuckCritPerPoint;
            critChance     += actor.HasState(ElementalState.GolpePreciso) ? (config.Elements != null ? config.Elements.StatePercent(ElementalState.GolpePreciso) : 0f) : 0f;
            critRoll        = rng.NextFloat();
            crit             = critRoll < critChance;
            if (crit && actor.ConsumeState(ElementalState.GolpePreciso))
            {
                result.Log.Add($"    [estado] GolpePreciso consumido — {actor.Name} conecta un crítico");
                r.RecordElement(ElementEventKind.StateConsumed, actor, state: ElementalState.GolpePreciso);
            }
            float raw       = actor.Attack * (crit ? config.CritMultiplier : 1f);
            float reduction = Mathf.Clamp01(target.EffDefense * config.DefenseReductionPerPoint);
            if (target.ConsumeState(ElementalState.Debilidad))
            {
                reduction = 0f;
                result.Log.Add("    [estado] Debilidad — el golpe ignora DEF");
                r.RecordElement(ElementEventKind.StateConsumed, target, state: ElementalState.Debilidad);
            }
            damage          = raw * (1f - reduction);
            suddenDeath     = config.SuddenDeathMultiplier(r.Round);
            if (suddenDeath > 1f) damage *= suddenDeath;
            if (target.ConsumeState(ElementalState.Boiling))
            {
                damage *= (1f + (config.Elements != null ? config.Elements.StatePercent(ElementalState.Boiling) : 0f));
                result.Log.Add($"    [estado] Boiling — daño amplificado a {damage:F1}");
                r.RecordElement(ElementEventKind.StateConsumed, target, state: ElementalState.Boiling, amount: damage);
            }
            absorbed        = Mathf.Min(target.Shield, damage);
            target.Shield  -= absorbed;
            target.Hp       = Mathf.Max(0f, target.Hp - (damage - absorbed));
        }

        string dir         = $"{(actor.IsA ? "A" : "B")}{actor.Index}→{(target.IsA ? "A" : "B")}{target.Index}";
        string shieldNote  = absorbed > 0f ? $" (escudo -{absorbed:F1}, quedan {target.Shield:F1})" : "";
        result.Log.Add(dodged
            ? $"    [{dir}] DODGE!  {target.Name} HP:{target.Hp:F1}   (eva {evaRoll * 100f:F0} vs {evaChance * 100f:F0}%)"
            : $"    [{dir}]{(crit ? " CRIT!" : "")} dmg:{damage:F1}{(suddenDeath > 1f ? $" MSx{suddenDeath:F1}" : "")}{shieldNote}  {target.Name} HP:{target.Hp:F1}   (eva {evaRoll * 100f:F0} vs {evaChance * 100f:F0}% · crit {critRoll * 100f:F0} vs {critChance * 100f:F0}%)");

        if (!dodged)
        {
            if (target.ConsumeState(ElementalState.Charcoal))
            {
                float reflect = damage * (config.Elements != null ? config.Elements.StatePercent(ElementalState.Charcoal) : 0f);
                actor.Hp = Mathf.Max(0f, actor.Hp - reflect);
                result.Log.Add($"    [estado] Charcoal — {target.Name} devuelve {reflect:F1} a {actor.Name} → {actor.Hp:F1}");
                r.RecordElement(ElementEventKind.StateConsumed, target, state: ElementalState.Charcoal, amount: reflect);
                r.RecordElement(ElementEventKind.Damage, actor, amount: reflect);
            }

            if (damage > 0f)
                CombatElements.AddMark(target, actor.Element, false, actor, config, result, r, rng);
        }
        float defHpAfterStrike    = target.Hp;
        float defShieldAfterHit   = target.Shield;

        return new StrikeOutcome { Dodged = dodged, Crit = crit, Damage = damage, DefenderHpAfter = defHpAfterStrike, DefenderShieldAfter = defShieldAfterHit };
    }
}
}
