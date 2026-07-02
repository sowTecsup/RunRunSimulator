using System;
using System.Collections.Generic;
namespace MoriMonchiSimulator
{

// PERSISTENT history of one finished combat, stored on CreatureDNA.CombatHistory.
// Unlike CombatLogEntry (a transient display DTO), this rides the normal DNA
// persistence (local JSON + Cloud Save push/pull) so it survives forever, and is
// structured turn-by-turn so a future Combat Visualizer can replay the fight.
//
// Local and async combats are produced by the SAME engine: local CombatService (C#)
// runs it synchronously, and async combat runs that exact sim seeded with a shared
// Seed plus both fighters' DNA snapshots — every client that replays it (winner and
// loser) derives an identical record. The server-side JS (process-matchmaking.js /
// run-combat.js) only matches opponents and hands out the seed; it no longer
// simulates the fight. The visualizer is LOCAL and feeds purely off this stored data.
[Serializable]
public class CombatRecord
{
    public string        OpponentName       = "";    // the rival creature
    public string        OpponentPlayerName = "";     // async only — the rival's player; "" for local
    public DateTime      Date;                          // when it happened (UTC)
    public CombatOutcome Outcome;                       // from THIS creature's POV
    public bool          Died;                          // this creature died in this fight
    public string        EvolvedSlot;                   // part this creature evolved on a win, or null

    public CombatFighterSnapshot SelfStats;
    public CombatFighterSnapshot OpponentStats;

    public int    Seed;
    public string OpponentDnaId    = "";
    public string OpponentPlayerId = "";

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

    public List<CombatStatusMark> StatusA = new List<CombatStatusMark>();
    public List<CombatStatusMark> StatusB = new List<CombatStatusMark>();
}

// Effective stats (post-equipment) of one fighter at the moment of the combat.
// Field names are PascalCase so they match the JSON contract, same as CombatTurn.
[Serializable]
public class CombatFighterSnapshot
{
    public float MaxHp;
    public float Attack;
    public float Speed;
    public float Defense;
    public float Luck;
    public float Evasion;

    public int    BodyTier;
    public int    ArmTier;
    public int    EyeTier;
    public int    MouthTier;
    public string ColorHex = "";
}

[Serializable]
public class CombatStatusMark
{
    public ModifierEffectKind Kind;
    public int Stacks;
}
}
