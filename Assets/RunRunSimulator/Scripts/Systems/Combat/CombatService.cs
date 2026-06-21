using System;
using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

// Stateless turn-based combat simulator.
// Attack order: highest Speed attacks first each round.
// Critical hit: CritChance probability → damage × CritMultiplier.
// Post-combat: winner may evolve a random part (Tier1/2 only); loser may die.
// Draw: if neither creature reaches 0 HP before MaxRounds → no consequences, FightCount++ for both.
//
// Besides the string Log (for inline display), it builds a structured list of
// CombatTurn (A = dnaA) and writes a CombatRecord onto BOTH fighters' DNA history
// (CreatureDNA.CombatHistory) — the persistent, replayable record the future
// Combat Visualizer reads. The async server motors emit the same shape.
public static class CombatService
{
    // Combat HP is intentionally swingier than the raw stat: base HP is scaled ×5
    // entering a fight so battles last several rounds. Attack/Speed are untouched.
    // The cloud motors (process-matchmaking.js / run-combat.js) apply the same ×5.
    private const float BaseHpCombatMultiplier = 5f;

    public static CombatResult Simulate(
        string             idA,
        string             idB,
        CreatureRegistrySO registry,
        CreatureDatabaseSO db,
        CombatManagerSO    config)
    {
        if (!registry.TryGet(idA, out var dnaA))
        {
            Debug.LogError($"[CombatService] ID '{idA}' not found in registry.");
            return null;
        }
        if (!registry.TryGet(idB, out var dnaB))
        {
            Debug.LogError($"[CombatService] ID '{idB}' not found in registry.");
            return null;
        }
        if (dnaA.IsDead || dnaB.IsDead)
        {
            Debug.LogError("[CombatService] Cannot simulate combat: one or both creatures are dead.");
            return null;
        }
        if (dnaA.IsBusy || dnaB.IsBusy)
        {
            Debug.LogError("[CombatService] Cannot simulate combat: one or both creatures are busy (queued for async combat).");
            return null;
        }
        if (dnaA.FightCount >= config.MaxFightCount)
        {
            Debug.LogError($"[CombatService] '{idA}' has no fights remaining ({dnaA.FightCount}/{config.MaxFightCount}).");
            return null;
        }
        if (dnaB.FightCount >= config.MaxFightCount)
        {
            Debug.LogError($"[CombatService] '{idB}' has no fights remaining ({dnaB.FightCount}/{config.MaxFightCount}).");
            return null;
        }

        var result = new CombatResult();
        var statsA = ComputeStats(dnaA, db);
        var statsB = ComputeStats(dnaB, db);

        result.Log.Add($"=== COMBAT START ===");
        result.Log.Add($"[A] \"{dnaA.CustomName}\"  {Clip(idA)}  HP:{statsA.TotalHP:F1}  ATK:{statsA.Attack:F1}  SPD:{statsA.Speed:F1}");
        result.Log.Add($"[B] \"{dnaB.CustomName}\"  {Clip(idB)}  HP:{statsB.TotalHP:F1}  ATK:{statsB.Attack:F1}  SPD:{statsB.Speed:F1}");

        float hpA = statsA.TotalHP;
        float hpB = statsB.TotalHP;

        // ── Turn simulation ───────────────────────────────────────

        bool someoneKO = false;
        for (int round = 1; round <= config.MaxRounds; round++)
        {
            bool aFirst = statsA.Speed > statsB.Speed ||
                          (Mathf.Approximately(statsA.Speed, statsB.Speed) && UnityEngine.Random.value < 0.5f);

            result.Log.Add($"--- Round {round} (first: {(aFirst ? "A" : "B")}) ---");

            if (aFirst)
            {
                hpB -= Strike(dnaA.CustomName, dnaB.CustomName, true,  statsA.Attack, hpB, config, result, round);
                if (hpB <= 0f) { someoneKO = true; break; }

                hpA -= Strike(dnaB.CustomName, dnaA.CustomName, false, statsB.Attack, hpA, config, result, round);
                if (hpA <= 0f) { someoneKO = true; break; }
            }
            else
            {
                hpA -= Strike(dnaB.CustomName, dnaA.CustomName, false, statsB.Attack, hpA, config, result, round);
                if (hpA <= 0f) { someoneKO = true; break; }

                hpB -= Strike(dnaA.CustomName, dnaB.CustomName, true,  statsA.Attack, hpB, config, result, round);
                if (hpB <= 0f) { someoneKO = true; break; }
            }
        }

        // ── Draw: MaxRounds reached with no KO ───────────────────

        if (!someoneKO)
        {
            result.IsDraw   = true;
            dnaA.FightCount++;
            dnaB.FightCount++;
            result.Log.Add($"=== DRAW — {config.MaxRounds} rounds reached. A:{hpA:F1}HP  B:{hpB:F1}HP ===");
            result.Log.Add("[DRAW] No consequences for either fighter.");
            result.Log.Add($"=== COMBAT END === DRAW");

            RecordHistory(dnaA, dnaB, CombatOutcome.Draw, died: false, evolvedSlot: null, selfIsA: true,  result.Turns);
            RecordHistory(dnaB, dnaA, CombatOutcome.Draw, died: false, evolvedSlot: null, selfIsA: false, result.Turns);
            return result;
        }

        // ── Determine winner ──────────────────────────────────────

        bool aWins  = hpA > 0f;   // whoever survived the KO check wins
        var  winner = aWins ? dnaA : dnaB;
        var  loser  = aWins ? dnaB : dnaA;

        result.WinnerID   = winner.UniqueID;
        result.LoserID    = loser.UniqueID;
        result.WinnerName = winner.CustomName;
        result.LoserName  = loser.CustomName;
        string winnerLabel = aWins ? $"A \"{dnaA.CustomName}\"" : $"B \"{dnaB.CustomName}\"";
        result.Log.Add($"=== KO === {winnerLabel} wins | A:{Mathf.Max(0, hpA):F1}HP  B:{Mathf.Max(0, hpB):F1}HP ===");

        // ── Update combat stats ───────────────────────────────────

        winner.FightCount++;
        winner.WinCount++;
        loser.FightCount++;

        // ── Evolution (winner) — always triggers, random slot ────────

        result.EvolvedSlot   = TryEvolveRandomSlot(winner);
        result.WinnerEvolved = result.EvolvedSlot != null;
        result.Log.Add(result.WinnerEvolved
            ? $"[EVOLUTION] \"{winner.CustomName}\" — {result.EvolvedSlot} evolved to Tier{GetSlotTier(winner, result.EvolvedSlot)}!"
            : $"[EVOLUTION] \"{winner.CustomName}\" — all parts already at max Tier.");

        // ── Death (loser) ─────────────────────────────────────────

        if (UnityEngine.Random.value < config.DeathChance)
        {
            loser.IsDead    = true;
            result.LoserDied = true;
            result.Log.Add("[DEATH] Loser has perished permanently.");
        }

        string evolvedLine = result.WinnerEvolved ? $" | Evolved: {result.EvolvedSlot} → Tier{GetSlotTier(winner, result.EvolvedSlot)}" : "";
        result.Log.Add($"=== COMBAT END === Winner: \"{winner.CustomName}\"  {winner.UniqueID}{evolvedLine}");

        // ── Persistent per-creature history (replayable) ──────────

        RecordHistory(
            winner, loser, CombatOutcome.Won,
            died: false, evolvedSlot: result.EvolvedSlot, selfIsA: aWins, result.Turns);
        RecordHistory(
            loser, winner, CombatOutcome.Lost,
            died: result.LoserDied, evolvedSlot: null, selfIsA: !aWins, result.Turns);

        return result;
    }

    // ── Private helpers ───────────────────────────────────────────

    private struct Stats
    {
        public float TotalHP;
        public float Attack;
        public float Speed;
    }

    // Effective stats for display (e.g. the detail window): base (DNA) plus each
    // part's stat and its +(tier-1) bonus. Same math the combat sim uses.
    public readonly struct EffectiveStats
    {
        public readonly float HP;
        public readonly float Attack;
        public readonly float Speed;
        public EffectiveStats(float hp, float atk, float spd) { HP = hp; Attack = atk; Speed = spd; }
    }

    public static EffectiveStats GetEffectiveStats(CreatureDNA dna, CreatureDatabaseSO db)
    {
        var s = ComputeStats(dna, db);
        return new EffectiveStats(s.TotalHP, s.Attack, s.Speed);
    }

    private static Stats ComputeStats(CreatureDNA dna, CreatureDatabaseSO db)
    {
        float hp  = dna.BaseHP * BaseHpCombatMultiplier;
        float atk = dna.BaseAttack;
        float spd = dna.BaseSpeed;

        AccumulatePart(db.GetBodyShape(dna.BodyShapeID), dna.BodyTier,  ref hp, ref atk, ref spd);
        AccumulatePart(db.GetArm(dna.ArmID),             dna.ArmTier,   ref hp, ref atk, ref spd);
        AccumulatePart(db.GetEye(dna.EyeID),             dna.EyeTier,   ref hp, ref atk, ref spd);
        AccumulatePart(db.GetMouth(dna.MouthID),         dna.MouthTier, ref hp, ref atk, ref spd);

        return new Stats { TotalHP = hp, Attack = atk, Speed = spd };
    }

    private static void AccumulatePart(BodyPart part, Tier tier, ref float hp, ref float atk, ref float spd)
    {
        if (part == null) return;
        int bonus = (int)tier - 1;   // Tier1=0, Tier2=+1, Tier3=+2
        hp  += part.HP     + bonus;
        atk += part.Attack + bonus;
        spd += part.Speed  + bonus;
    }

    // Executes one attack. defenderHP is HP before the hit. Logs a line AND appends
    // a structured CombatTurn (attackerIsA = the striker is combatant A). Returns
    // damage dealt.
    private static float Strike(
        string attackerName, string defenderName, bool attackerIsA,
        float attack, float defenderHP,
        CombatManagerSO config, CombatResult result, int round)
    {
        bool  isCrit  = UnityEngine.Random.value < config.CritChance;
        float damage  = attack * (isCrit ? config.CritMultiplier : 1f);
        float hpAfter = Mathf.Max(0f, defenderHP - damage);

        string dir = attackerIsA ? "A→B" : "B→A";
        result.Log.Add($"  [{dir}]{(isCrit ? " CRIT!" : "")} dmg:{damage:F1}  defender HP after:{hpAfter:F1}");

        result.Turns.Add(new CombatTurn
        {
            TurnNumber      = round,
            AttackerName    = attackerName,
            DefenderName    = defenderName,
            AttackerIsA     = attackerIsA,
            Damage          = damage,
            WasCrit         = isCrit,
            DefenderHpAfter = hpAfter,
        });
        return damage;
    }

    // Appends one finished-combat record to a creature's persistent history (POV of
    // 'self'). The Turns list is shared with the opponent's record (the replay is
    // symmetric); selfIsA tells the visualizer which side "self" was.
    private static void RecordHistory(
        CreatureDNA self, CreatureDNA opponent, CombatOutcome outcome,
        bool died, string evolvedSlot, bool selfIsA, List<CombatTurn> turns)
    {
        self.CombatHistory ??= new List<CombatRecord>();
        self.CombatHistory.Add(new CombatRecord
        {
            OpponentName       = opponent.CustomName,
            OpponentPlayerName = "",                       // local combat — same player
            Date               = DateTime.UtcNow,
            Outcome            = outcome,
            Died               = died,
            EvolvedSlot        = outcome == CombatOutcome.Won ? evolvedSlot : null,
            SelfWasA           = selfIsA,
            Turns              = turns,
        });
    }

    // Picks a random slot not already at Tier3 and advances it by one.
    // Returns the slot name, or null if all slots are maxed.
    private static string TryEvolveRandomSlot(CreatureDNA dna)
    {
        var eligible = new List<string>();
        if (dna.BodyTier  < Tier.Tier3) eligible.Add("Body");
        if (dna.ArmTier   < Tier.Tier3) eligible.Add("Arm");
        if (dna.EyeTier   < Tier.Tier3) eligible.Add("Eye");
        if (dna.MouthTier < Tier.Tier3) eligible.Add("Mouth");

        if (eligible.Count == 0) return null;

        string slot = eligible[UnityEngine.Random.Range(0, eligible.Count)];
        switch (slot)
        {
            case "Body":  dna.BodyTier  = (Tier)((int)dna.BodyTier  + 1); break;
            case "Arm":   dna.ArmTier   = (Tier)((int)dna.ArmTier   + 1); break;
            case "Eye":   dna.EyeTier   = (Tier)((int)dna.EyeTier   + 1); break;
            case "Mouth": dna.MouthTier = (Tier)((int)dna.MouthTier + 1); break;
        }
        return slot;
    }

    private static int GetSlotTier(CreatureDNA dna, string slot) => slot switch
    {
        "Body"  => (int)dna.BodyTier,
        "Arm"   => (int)dna.ArmTier,
        "Eye"   => (int)dna.EyeTier,
        "Mouth" => (int)dna.MouthTier,
        _       => 0
    };

    private static string Clip(string id) => id[..Mathf.Min(14, id.Length)];
}
}
