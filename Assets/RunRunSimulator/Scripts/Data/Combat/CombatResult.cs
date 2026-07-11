using System;
using System.Collections.Generic;
namespace MoriMonchiSimulator
{

// TRANSIENT result of one finished 3v3 team combat (never persisted — the per-unit
// CombatRecord written to each fighter's CreatureDNA.CombatHistory is the durable copy).
[Serializable]
public class CombatResult
{
    public bool          TeamAWon;
    public bool          IsDraw;
    public string        EvolvedSlot;        // "Body" | "Arm" | "Eye" | "Mouth" | null
    public string        EvolvedUnitId;      // winning-team unit that evolved, "" if none
    public string        EvolvedUnitName;
    public string        DiedUnitId;         // losing-team unit that died permanently, "" if none
    public string        DiedUnitName;
    public List<string>  Log = new List<string>();

    public List<CombatFighterSnapshot> TeamA = new List<CombatFighterSnapshot>();
    public List<CombatFighterSnapshot> TeamB = new List<CombatFighterSnapshot>();

    // Structured turn-by-turn data (A = TeamA). Mirrors what the server emits, and
    // feeds the per-creature CombatRecord stored on each fighter's DNA.
    public List<CombatTurn> Turns = new List<CombatTurn>();

    public string Summary => IsDraw
        ? "DRAW — Max rounds reached. No consequences."
        : $"Winner: Team {(TeamAWon ? "A" : "B")}" +
          $"{(!string.IsNullOrEmpty(EvolvedUnitName) ? $" [EVOLVED \"{EvolvedUnitName}\" {EvolvedSlot}]" : "")}" +
          $"{(!string.IsNullOrEmpty(DiedUnitName)    ? $" [DIED \"{DiedUnitName}\"]"                     : "")}";
}
}
