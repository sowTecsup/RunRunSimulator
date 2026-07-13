namespace MoriMonchiSimulator
{

// Marcas elementales del sim 3v3: cada MoriMochi tiene un Element innato; los
// efectos pueden marcar a un objetivo con un elemento desde una de dos
// fuentes separadas (aliada/enemiga). Un portador nunca acumula más de una
// marca del mismo Element+fuente — la segunda marca de ese par simplemente se
// ignora (log, no duplica). Cuando un portador junta DOS marcas de la MISMA
// fuente pero de Elements DISTINTOS, ambas se consumen y detonan la reacción
// que resuelva ElementTableSO.FindReaction (CombatManagerSO.Elements) para
// ese par + fuente — mismo Element nunca reacciona consigo mismo, y sin tabla
// no hay reacción (las marcas quedan). Esta clase solo ejecuta: la
// definición de reacciones y sus ReactionEffectBase (instantáneos como
// Cleanse/OverGrow/Leech/PisoTierra o estados armados de un solo uso como
// Energizado/Vaporizado/GolpePreciso/Charcoal/Boiling/Debilidad/Confuso/
// Mareado) vive en ElementTableSO. Determinista: todo roll sale del
// CombatRng inyectado; un pick solo consume rng si hay candidatos (patrón
// CombatTargeting).
public class ElementMark
{
    public Element Element;
    public bool AllySource;
}

public static class CombatElements
{
    public static void AddMark(Combatant target, Element element, bool allySource, Combatant reactor, CombatManagerSO config, CombatResult result, CombatResolver r, CombatRng rng)
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
        r.RecordElement(ElementEventKind.MarkApplied, target, element: element, allySource: allySource);

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
        var reaction = config.Elements != null ? config.Elements.FindReaction(other.Element, element, allySource) : null;
        if (reaction == null) return;

        target.Marks.Remove(other);
        target.Marks.Remove(justAdded);

        result.Log.Add($"    [reacción] ¡{reaction.Name}! ({other.Element} × {element}, fuente {fuente}) sobre {target.Name}");
        r.RecordElement(ElementEventKind.Reaction, target, element: other.Element, elementB: element, allySource: allySource, reactionName: reaction.Name);
        foreach (var e in reaction.Effects) e.Apply(target, reactor, reaction.Name, result, r, rng);
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
