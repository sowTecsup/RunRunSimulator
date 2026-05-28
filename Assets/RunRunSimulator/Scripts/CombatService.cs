using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Stateless turn-based combat simulator.
// Attack order: highest Speed attacks first each round.
// Critical hit: CritChance probability → damage × CritMultiplier.
// Post-combat: winner may evolve a random part (Tier1/2 only); loser may die.
public static class CombatService
{
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

        var result = new CombatResult();
        var statsA = ComputeStats(dnaA, db);
        var statsB = ComputeStats(dnaB, db);

        result.Log.Add($"=== COMBAT START ===");
        result.Log.Add($"[A] {idA[..Mathf.Min(14, idA.Length)]}  HP:{statsA.TotalHP:F1}  ATK:{statsA.Attack:F1}  SPD:{statsA.Speed:F1}");
        result.Log.Add($"[B] {idB[..Mathf.Min(14, idB.Length)]}  HP:{statsB.TotalHP:F1}  ATK:{statsB.Attack:F1}  SPD:{statsB.Speed:F1}");

        float hpA = statsA.TotalHP;
        float hpB = statsB.TotalHP;

        // ── Turn simulation ───────────────────────────────────────

        for (int round = 1; round <= config.MaxRounds; round++)
        {
            // Faster creature attacks first; ties broken randomly
            bool aFirst = statsA.Speed > statsB.Speed ||
                          (Mathf.Approximately(statsA.Speed, statsB.Speed) && Random.value < 0.5f);

            if (aFirst)
            {
                hpB -= Strike(dnaA, dnaB, statsA.Attack, config, result.Log, round, "A→B");
                if (hpB <= 0f) break;
                hpA -= Strike(dnaB, dnaA, statsB.Attack, config, result.Log, round, "B→A");
                if (hpA <= 0f) break;
            }
            else
            {
                hpA -= Strike(dnaB, dnaA, statsB.Attack, config, result.Log, round, "B→A");
                if (hpA <= 0f) break;
                hpB -= Strike(dnaA, dnaB, statsA.Attack, config, result.Log, round, "A→B");
                if (hpB <= 0f) break;
            }
        }

        // ── Determine winner ──────────────────────────────────────

        // After MaxRounds: whoever has more HP remaining wins; tie → random
        bool aWins = hpA > hpB || (Mathf.Approximately(hpA, hpB) && Random.value < 0.5f);

        var winner = aWins ? dnaA : dnaB;
        var loser  = aWins ? dnaB : dnaA;

        result.WinnerID = winner.UniqueID;
        result.LoserID  = loser.UniqueID;
        result.Log.Add($"=== WINNER: {(aWins ? "A" : "B")} | Remaining HP — A:{hpA:F1}  B:{hpB:F1} ===");

        // ── Update combat stats ───────────────────────────────────

        winner.FightCount++;
        winner.WinCount++;
        loser.FightCount++;

        // ── Evolution (winner) ────────────────────────────────────

        if (Random.value < config.EvolutionChance)
        {
            result.EvolvedSlot  = TryEvolveRandomSlot(winner);
            result.WinnerEvolved = result.EvolvedSlot != null;
            if (result.WinnerEvolved)
                result.Log.Add($"[EVOLUTION] Winner's {result.EvolvedSlot} part evolved to Tier {GetSlotTier(winner, result.EvolvedSlot)}!");
            else
                result.Log.Add("[EVOLUTION] All parts already at max Tier.");
        }

        // ── Death (loser) ─────────────────────────────────────────

        if (Random.value < config.DeathChance)
        {
            loser.IsDead    = true;
            result.LoserDied = true;
            result.Log.Add($"[DEATH] Loser has perished. RIP.");
        }

        result.Log.Add($"=== COMBAT END === {result.Summary}");
        return result;
    }

    // ── Private helpers ───────────────────────────────────────────

    private struct Stats
    {
        public float TotalHP;
        public float Attack;
        public float Speed;
    }

    private static Stats ComputeStats(CreatureDNA dna, CreatureDatabaseSO db)
    {
        float hp  = dna.BaseHP;
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

    // Executes one attack and appends a log line. Returns damage dealt.
    private static float Strike(
        CreatureDNA attacker, CreatureDNA defender,
        float baseAttack, CombatManagerSO config,
        List<string> log, int round, string dir)
    {
        bool  isCrit = Random.value < config.CritChance;
        float damage = baseAttack * (isCrit ? config.CritMultiplier : 1f);
        log.Add($"R{round:D2} [{dir}] {(isCrit ? "CRIT! " : "")}dmg:{damage:F1}");
        return damage;
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

        string slot = eligible[Random.Range(0, eligible.Count)];
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
}
