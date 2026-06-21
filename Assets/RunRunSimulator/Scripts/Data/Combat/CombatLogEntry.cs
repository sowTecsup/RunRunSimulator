using System;
using System.Collections.Generic;
namespace MoriMonchiSimulator
{

// Display-friendly record of one finished combat, from the perspective of ONE of
// our creatures. The async path consumes the cloud result when it applies it, so
// we capture the log here (via GameEvents.OnCombatLogged) for the Results tab.
[Serializable]
public class CombatLogEntry
{
    public string CreatureId;
    public string CreatureName;
    public string OpponentLabel;                     // "Slimy Goo" or "Slimy Goo (Manolito)"
    public List<string> Lines = new List<string>();  // turn-by-turn
    public bool Won;
    public bool Died;

    public string Outcome => Won ? "¡Ganaste!" : "¡Perdiste!";
}
}
