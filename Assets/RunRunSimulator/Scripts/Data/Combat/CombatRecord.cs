using System;
using System.Collections.Generic;
namespace MoriMonchiSimulator
{

// PERSISTENT history of one finished combat, stored on CreatureDNA.CombatHistory.
// Unlike CombatLogEntry (a transient display DTO), this rides the normal DNA
// persistence (local JSON + Cloud Save push/pull) so it survives forever, and is
// structured turn-by-turn so a future Combat Visualizer can replay the fight.
//
// Both motors emit it in the same shape: local CombatService (C#) and the async
// server scripts (process-matchmaking.js / run-combat.js) — the server is
// authoritative, the client only reads and stores. The visualizer is LOCAL and
// feeds purely off this stored data.
[Serializable]
public class CombatRecord
{
    public string        OpponentName       = "";    // the rival creature
    public string        OpponentPlayerName = "";     // async only — the rival's player; "" for local
    public DateTime      Date;                          // when it happened (UTC)
    public CombatOutcome Outcome;                       // from THIS creature's POV
    public bool          Died;                          // this creature died in this fight
    public string        EvolvedSlot;                   // part this creature evolved on a win, or null

    // The replay is symmetric (same turns for both fighters). 'AttackerIsA' inside
    // each turn refers to combatant A of the simulation; 'SelfWasA' tells the
    // visualizer whether THIS creature was A, so it can map "A" to "me" or "them".
    public bool             SelfWasA;
    public List<CombatTurn> Turns = new List<CombatTurn>();
}

// One attack within a combat — enough to drive a turn-by-turn replay (animate the
// strike and the defender's HP bar). Field names are PascalCase so they match the
// JSON the server (JS) emits and the local C# serializer, with no remapping.
[Serializable]
public class CombatTurn
{
    public int    TurnNumber;
    public string AttackerName    = "";
    public string DefenderName    = "";
    public bool   AttackerIsA;        // attacker is combatant A of the simulation
    public float  Damage;
    public bool   WasCrit;
    public float  DefenderHpAfter;    // defender HP remaining after this hit
    public bool                  NoAttack;
    public List<CombatProcEvent> Procs = new List<CombatProcEvent>();
}
}
