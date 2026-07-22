using System.Collections.Generic;

namespace MoriMonchiSimulator
{

// Selección determinista de objetivos para el simulador 3v3: la fila viva más
// adelantada recibe los golpes, uniforme dentro de esa fila. Todo roll sale
// del CombatRng inyectado por el caller — nunca UnityEngine.Random.
public static class CombatTargeting
{
    public static CombatRow FrontRow(List<Combatant> team)
    {
        CombatRow best = CombatRow.Back;
        bool found = false;
        foreach (var c in team)
        {
            if (!c.IsAlive) continue;
            if (!found || c.Row < best)
            {
                best = c.Row;
                found = true;
            }
        }
        return found ? best : CombatRow.Front;
    }

    public static Combatant PickFrontTarget(List<Combatant> team, CombatRng rng)
    {
        var front = FrontRow(team);
        var candidates = new List<Combatant>();
        foreach (var c in team)
            if (c.IsAlive && c.Row == front) candidates.Add(c);

        if (candidates.Count == 0) return null;
        int idx = rng.Range(0, candidates.Count);
        return candidates[idx];
    }

    public static Combatant PickBacklineTarget(List<Combatant> team, CombatRng rng)
    {
        var front = FrontRow(team);
        var candidates = new List<Combatant>();
        foreach (var c in team)
            if (c.IsAlive && c.Row != front) candidates.Add(c);

        if (candidates.Count == 0) return null;
        int idx = rng.Range(0, candidates.Count);
        return candidates[idx];
    }

    public static Combatant PickAlly(List<Combatant> team, CombatRng rng)
    {
        var candidates = new List<Combatant>();
        foreach (var c in team)
            if (c.IsAlive) candidates.Add(c);

        if (candidates.Count == 0) return null;
        int idx = rng.Range(0, candidates.Count);
        return candidates[idx];
    }

    public static Combatant LowestHpAlly(List<Combatant> team)
    {
        Combatant best = null;
        foreach (var c in team)
        {
            if (!c.IsAlive) continue;
            if (best == null || c.Hp < best.Hp || (c.Hp == best.Hp && c.Index < best.Index))
                best = c;
        }
        return best;
    }

    public static Combatant LowestHpPercentAlly(List<Combatant> team)
    {
        Combatant best = null;
        float bestPercent = 0f;
        foreach (var c in team)
        {
            if (!c.IsAlive) continue;
            float percent = c.Hp / c.MaxHp;
            if (best == null || percent < bestPercent || (percent == bestPercent && c.Index < best.Index))
            {
                best = c;
                bestPercent = percent;
            }
        }
        return best;
    }
}
}
