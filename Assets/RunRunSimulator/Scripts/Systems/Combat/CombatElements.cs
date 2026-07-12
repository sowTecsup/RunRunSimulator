namespace MoriMonchiSimulator
{

// Marcas y reacciones elementales del sim 3v3: cada MoriMochi tiene un Element
// innato; los efectos pueden marcar a un objetivo con un elemento desde una de
// dos fuentes separadas (aliada/enemiga). Un portador nunca acumula más de una
// marca del mismo Element+fuente — la segunda marca de ese par simplemente se
// ignora (log, no duplica). Cuando un portador junta DOS marcas de la MISMA
// fuente pero de Elements DISTINTOS, ambas se consumen y detonan una reacción
// (tabla ALIADA si allySource, tabla OFENSIVA si no) — mismo Element nunca
// reacciona consigo mismo. Las reacciones instantáneas (Cleanse/OverGrow/
// Leech/PisoTierra) se resuelven en el momento; las armadas (Energizado,
// Vaporizado, GolpePreciso, Charcoal, Boiling, Debilidad, Confuso, Mareado)
// se agregan a Combatant.States como estados de un solo uso — sin duplicados.
// Determinista: todo roll sale del CombatRng inyectado; un pick solo consume
// rng si hay candidatos (patrón CombatTargeting).
public class ElementMark
{
    public Element Element;
    public bool AllySource;
}

public static class CombatElements
{
    public static ElementalState? ReactionFor(Element a, Element b, bool allySource)
    {
        if (a == b) return null;

        Element lo = a < b ? a : b;
        Element hi = a < b ? b : a;

        if (allySource)
        {
            if (lo == Element.Agua && hi == Element.Fuego)         return ElementalState.Vaporizado;
            if (lo == Element.Agua && hi == Element.Electricidad)  return ElementalState.GolpePreciso;
            if (lo == Element.Agua && hi == Element.Planta)        return ElementalState.Cleanse;
            if (lo == Element.Fuego && hi == Element.Electricidad) return ElementalState.Energizado;
            if (lo == Element.Fuego && hi == Element.Planta)       return ElementalState.Charcoal;
            if (lo == Element.Electricidad && hi == Element.Planta) return ElementalState.OverGrow;
        }
        else
        {
            if (lo == Element.Agua && hi == Element.Fuego)         return ElementalState.Boiling;
            if (lo == Element.Agua && hi == Element.Electricidad)  return ElementalState.Confuso;
            if (lo == Element.Agua && hi == Element.Planta)        return ElementalState.Leech;
            if (lo == Element.Fuego && hi == Element.Electricidad) return ElementalState.Mareado;
            if (lo == Element.Fuego && hi == Element.Planta)       return ElementalState.Debilidad;
            if (lo == Element.Electricidad && hi == Element.Planta) return ElementalState.PisoTierra;
        }

        return null;
    }

    public static void AddMark(Combatant target, Element element, bool allySource, Combatant reactor, CombatManagerSO config, CombatResult result, CombatRng rng)
    {
        string fuente = allySource ? "aliada" : "enemiga";

        foreach (var m in target.Marks)
        {
            if (m.Element == element && m.AllySource == allySource)
            {
                result.Log.Add($"    [marca] {target.Name} ya tiene marca {element} ({fuente})");
                return;
            }
        }

        target.Marks.Add(new ElementMark { Element = element, AllySource = allySource });
        result.Log.Add($"    [marca] {target.Name} recibe marca {element} ({fuente})");

        ElementMark other = null;
        foreach (var m in target.Marks)
        {
            if (m.AllySource == allySource && m.Element != element)
            {
                other = m;
                break;
            }
        }

        if (other == null) return;

        var justAdded = target.Marks[target.Marks.Count - 1];
        var state = ReactionFor(other.Element, element, allySource);
        if (state == null) return;

        target.Marks.Remove(other);
        target.Marks.Remove(justAdded);

        result.Log.Add($"    [reacción] ¡{state.Value}! ({other.Element} × {element}, fuente {fuente}) sobre {target.Name}");
        ApplyState(target, state.Value, reactor, config, result, rng);
    }

    public static void ApplyState(Combatant bearer, ElementalState state, Combatant reactor, CombatManagerSO config, CombatResult result, CombatRng rng)
    {
        switch (state)
        {
            case ElementalState.Cleanse:
            {
                ElementalState? negative = null;
                foreach (var s in bearer.States)
                {
                    if (IsNegative(s)) { negative = s; break; }
                }

                if (negative != null)
                {
                    bearer.States.Remove(negative.Value);
                    result.Log.Add($"    [estado] Cleanse purga {negative.Value} de {bearer.Name}");
                }
                else
                {
                    float heal = bearer.MaxHp * config.CleanseHealPercent;
                    float before = bearer.Hp;
                    bearer.Hp = System.Math.Min(bearer.MaxHp, bearer.Hp + heal);
                    result.Log.Add($"    [estado] Cleanse cura a {bearer.Name} +{bearer.Hp - before:F1} → {bearer.Hp:F1}");
                }
                return;
            }
            case ElementalState.OverGrow:
            {
                bearer.Shield *= 2f;
                result.Log.Add($"    [estado] OverGrow duplica el escudo de {bearer.Name} → {bearer.Shield:F1}");
                return;
            }
            case ElementalState.Leech:
            {
                float drained = System.Math.Min(bearer.Hp, config.LeechAmount);
                bearer.Hp = System.Math.Max(0f, bearer.Hp - config.LeechAmount);
                if (reactor != null)
                {
                    float before = reactor.Hp;
                    reactor.Hp = System.Math.Min(reactor.MaxHp, reactor.Hp + drained);
                    result.Log.Add($"    [estado] Leech drena {drained:F1} de {bearer.Name} → {bearer.Hp:F1} y cura {reactor.Name} +{reactor.Hp - before:F1} → {reactor.Hp:F1}");
                }
                else
                {
                    result.Log.Add($"    [estado] Leech drena {drained:F1} de {bearer.Name} → {bearer.Hp:F1}");
                }
                return;
            }
            case ElementalState.PisoTierra:
            {
                if (bearer.Marks.Count == 0)
                {
                    result.Log.Add($"    [estado] PisoTierra sobre {bearer.Name} — sin marcas que remover");
                    return;
                }

                int idx = rng.Range(0, bearer.Marks.Count);
                var removed = bearer.Marks[idx];
                bearer.Marks.RemoveAt(idx);
                result.Log.Add($"    [estado] PisoTierra remueve marca {removed.Element} ({(removed.AllySource ? "aliada" : "enemiga")}) de {bearer.Name}");
                return;
            }
        }

        if (bearer.States.Contains(state))
        {
            result.Log.Add($"    [estado] {bearer.Name} ya tiene {state} — sin efecto");
            return;
        }

        bearer.States.Add(state);
        result.Log.Add($"    [estado] {bearer.Name} queda {state} (armado)");
    }

    public static bool IsNegative(ElementalState s)
    {
        return s == ElementalState.Boiling
            || s == ElementalState.Debilidad
            || s == ElementalState.Confuso
            || s == ElementalState.Leech
            || s == ElementalState.Mareado
            || s == ElementalState.PisoTierra;
    }
}
}
