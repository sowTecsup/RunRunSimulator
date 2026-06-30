using System;
using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

// Stateless turn-based combat simulator.
// Attack order: highest Speed attacks first each round.
// Per turn: tick the attacker's active statuses → fire passive procs → (if not stunned)
// roll offensive procs at turn start → attack (evasion → crit → DEF) → on connect apply
// the rolled offensive procs plus the defender's defensive procs. Combat procs come from
// equipment (CombatProcEffect) and act through ICombatContext so a future stack can
// intercept. Local-only for now; seed + online parity land next.
public static class CombatService
{
    public const float BaseHpCombatMultiplier = 5f;

    public static CombatResult Simulate(
        string              idA,
        string              idB,
        CreatureRegistrySO  registry,
        CreatureDatabaseSO  db,
        CombatManagerSO     config,
        EquipmentDatabaseSO equipDb)
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
        var A = BuildCombatant(dnaA, db, equipDb, true);
        var B = BuildCombatant(dnaB, db, equipDb, false);

        result.Log.Add("=== COMBAT START ===");
        result.Log.Add($"[A] \"{A.Name}\"  {Clip(idA)}  HP:{A.MaxHp:F1}  ATK:{A.Attack:F1}  SPD:{A.Speed:F1}  DEF:{A.Defense:F0}  LCK:{A.Luck:F0}  EVA:{A.Evasion:F0}");
        result.Log.Add($"[B] \"{B.Name}\"  {Clip(idB)}  HP:{B.MaxHp:F1}  ATK:{B.Attack:F1}  SPD:{B.Speed:F1}  DEF:{B.Defense:F0}  LCK:{B.Luck:F0}  EVA:{B.Evasion:F0}");

        var resolver = new Resolver { Result = result };
        bool someoneKO = false;

        for (int round = 1; round <= config.MaxRounds; round++)
        {
            bool aFirst = A.Speed > B.Speed ||
                          (Mathf.Approximately(A.Speed, B.Speed) && UnityEngine.Random.value < 0.5f);

            result.Log.Add($"--- Round {round} (first: {(aFirst ? "A" : "B")}) ---");

            var first  = aFirst ? A : B;
            var second = aFirst ? B : A;

            if (TakeTurn(first,  second, config, result, round, resolver)) { someoneKO = true; break; }
            if (TakeTurn(second, first,  config, result, round, resolver)) { someoneKO = true; break; }
        }

        if (!someoneKO)
        {
            result.IsDraw = true;
            dnaA.FightCount++;
            dnaB.FightCount++;
            result.Log.Add($"=== DRAW — {config.MaxRounds} rounds reached. A:{A.Hp:F1}HP  B:{B.Hp:F1}HP ===");
            result.Log.Add("[DRAW] No consequences for either fighter.");
            result.Log.Add("=== COMBAT END === DRAW");

            RecordHistory(dnaA, dnaB, CombatOutcome.Draw, false, null, true,  result.Turns);
            RecordHistory(dnaB, dnaA, CombatOutcome.Draw, false, null, false, result.Turns);
            return result;
        }

        bool aWins  = A.Hp > 0f;
        var  winner = aWins ? dnaA : dnaB;
        var  loser  = aWins ? dnaB : dnaA;

        result.WinnerID   = winner.UniqueID;
        result.LoserID    = loser.UniqueID;
        result.WinnerName = winner.CustomName;
        result.LoserName  = loser.CustomName;
        string winnerLabel = aWins ? $"A \"{dnaA.CustomName}\"" : $"B \"{dnaB.CustomName}\"";
        result.Log.Add($"=== KO === {winnerLabel} wins | A:{Mathf.Max(0f, A.Hp):F1}HP  B:{Mathf.Max(0f, B.Hp):F1}HP ===");

        winner.FightCount++;
        winner.WinCount++;
        loser.FightCount++;

        result.EvolvedSlot   = TryEvolveRandomSlot(winner);
        result.WinnerEvolved = result.EvolvedSlot != null;
        result.Log.Add(result.WinnerEvolved
            ? $"[EVOLUTION] \"{winner.CustomName}\" — {result.EvolvedSlot} evolved to Tier{GetSlotTier(winner, result.EvolvedSlot)}!"
            : $"[EVOLUTION] \"{winner.CustomName}\" — all parts already at max Tier.");

        if (UnityEngine.Random.value < config.DeathChance)
        {
            loser.IsDead     = true;
            result.LoserDied = true;
            result.Log.Add("[DEATH] Loser has perished permanently.");
        }

        string evolvedLine = result.WinnerEvolved ? $" | Evolved: {result.EvolvedSlot} → Tier{GetSlotTier(winner, result.EvolvedSlot)}" : "";
        result.Log.Add($"=== COMBAT END === Winner: \"{winner.CustomName}\"  {winner.UniqueID}{evolvedLine}");

        RecordHistory(winner, loser,  CombatOutcome.Won,  false,            result.EvolvedSlot, aWins,  result.Turns);
        RecordHistory(loser,  winner, CombatOutcome.Lost, result.LoserDied, null,               !aWins, result.Turns);

        return result;
    }

    // ── Turn ───────────────────────────────────────────────────────

    // Returns true if combat should end (someone reached 0 HP).
    private static bool TakeTurn(Combatant atk, Combatant def, CombatManagerSO config, CombatResult result, int round, Resolver r)
    {
        TickStatuses(atk, result);
        if (atk.Hp <= 0f) { result.Log.Add($"  [{atk.Name}] succumbs to its afflictions."); return true; }

        FireProcs(atk, def, TriggerType.Passive, result, r, true);
        if (atk.Hp <= 0f || def.Hp <= 0f) return true;

        if (atk.StunTurns > 0)
        {
            atk.StunTurns--;
            result.Log.Add($"  [{atk.Name}] is stunned — skips turn ({atk.StunTurns} left)");
            return false;
        }

        var armed = new List<CombatProcEffect>();
        foreach (var p in atk.Procs)
            if (p.Trigger == TriggerType.Offensive && UnityEngine.Random.value < p.ProcChance / 100f)
                armed.Add(p);

        bool  dodged = UnityEngine.Random.value < def.Evasion * config.EvasionPerPoint;
        bool  crit   = false;
        float damage = 0f;
        if (!dodged)
        {
            float critChance = config.CritChance + atk.Luck * config.LuckCritPerPoint;
            crit             = UnityEngine.Random.value < critChance;
            float raw        = atk.Attack * (crit ? config.CritMultiplier : 1f);
            float reduction  = Mathf.Clamp01(def.Defense * config.DefenseReductionPerPoint);
            damage           = raw * (1f - reduction);
            def.Hp           = Mathf.Max(0f, def.Hp - damage);
        }

        string dir = atk.IsA ? "A→B" : "B→A";
        result.Log.Add(dodged
            ? $"  [{dir}] DODGE! {def.Name} HP:{def.Hp:F1}"
            : $"  [{dir}]{(crit ? " CRIT!" : "")} dmg:{damage:F1}  {def.Name} HP:{def.Hp:F1}");

        result.Turns.Add(new CombatTurn
        {
            TurnNumber      = round,
            AttackerName    = atk.Name,
            DefenderName    = def.Name,
            AttackerIsA     = atk.IsA,
            Damage          = damage,
            WasCrit         = crit,
            DefenderHpAfter = def.Hp,
        });

        if (!dodged)
        {
            foreach (var p in armed) { r.Self = atk; r.Opponent = def; p.Apply(r); }
            FireProcs(def, atk, TriggerType.Defensive, result, r, true);
        }

        return atk.Hp <= 0f || def.Hp <= 0f;
    }

    private static void TickStatuses(Combatant c, CombatResult result)
    {
        for (int i = c.Active.Count - 1; i >= 0; i--)
        {
            var a = c.Active[i];
            switch (a.Kind)
            {
                case ModifierEffectKind.Poison:
                case ModifierEffectKind.Burn:
                    c.Hp = Mathf.Max(0f, c.Hp - a.Magnitude);
                    result.Log.Add($"  [{a.Kind}] {c.Name} -{a.Magnitude} → {c.Hp:F1}");
                    break;
                case ModifierEffectKind.Regen:
                    c.Hp = Mathf.Min(c.MaxHp, c.Hp + a.Magnitude);
                    result.Log.Add($"  [Regen] {c.Name} +{a.Magnitude} → {c.Hp:F1}");
                    break;
            }
            if (--a.RemainingTurns <= 0) c.Active.RemoveAt(i);
        }
    }

    private static void FireProcs(Combatant owner, Combatant opponent, TriggerType trigger, CombatResult result, Resolver r, bool roll)
    {
        foreach (var p in owner.Procs)
        {
            if (p.Trigger != trigger) continue;
            if (roll && UnityEngine.Random.value >= p.ProcChance / 100f) continue;
            r.Self     = owner;
            r.Opponent = opponent;
            p.Apply(r);
        }
    }

    // ── Combatant model ────────────────────────────────────────────

    private class Combatant
    {
        public CreatureDNA Dna;
        public string      Name;
        public bool        IsA;
        public float       Hp;
        public float       MaxHp;
        public float       Attack;
        public float       Speed;
        public float       Defense;
        public float       Luck;
        public float       Evasion;
        public int         StunTurns;
        public List<CombatProcEffect> Procs  = new List<CombatProcEffect>();
        public List<ActiveEffect>     Active = new List<ActiveEffect>();
    }

    private class ActiveEffect
    {
        public ModifierEffectKind Kind;
        public int RemainingTurns;
        public int Magnitude;
    }

    private class Resolver : ICombatContext
    {
        public CombatResult Result;
        public Combatant    Self;
        public Combatant    Opponent;

        public void DamageOpponent(float amount, string source)
        {
            Opponent.Hp = Mathf.Max(0f, Opponent.Hp - amount);
            Result.Log.Add($"  [{source}] {Opponent.Name} -{amount:F1} → {Opponent.Hp:F1}");
        }

        public void HealSelf(float amount, string source)
        {
            Self.Hp = Mathf.Min(Self.MaxHp, Self.Hp + amount);
            Result.Log.Add($"  [{source}] {Self.Name} +{amount:F1} → {Self.Hp:F1}");
        }

        public void ApplyStatusToOpponent(ModifierEffectKind kind, int turns, int magnitude, string source) =>
            AddStatus(Opponent, kind, turns, magnitude, source);

        public void ApplyStatusToSelf(ModifierEffectKind kind, int turns, int magnitude, string source) =>
            AddStatus(Self, kind, turns, magnitude, source);

        public void StunOpponent(int turns)
        {
            if (turns > Opponent.StunTurns) Opponent.StunTurns = turns;
            Result.Log.Add($"  [stun] {Opponent.Name} stunned {turns} turn(s)");
        }

        private void AddStatus(Combatant t, ModifierEffectKind kind, int turns, int magnitude, string source)
        {
            var existing = t.Active.Find(a => a.Kind == kind);
            if (existing != null)
            {
                if (turns > existing.RemainingTurns) existing.RemainingTurns = turns;
                existing.Magnitude = magnitude;
            }
            else
            {
                t.Active.Add(new ActiveEffect { Kind = kind, RemainingTurns = turns, Magnitude = magnitude });
            }
            Result.Log.Add($"  [{source}] {t.Name} gains {kind} ({magnitude}/turn, {turns}t)");
        }
    }

    private static Combatant BuildCombatant(CreatureDNA dna, CreatureDatabaseSO db, EquipmentDatabaseSO equipDb, bool isA)
    {
        var s   = ComputeStats(dna, db);
        var eff = EquipmentStats.Apply(
            new EffectiveStats(s.Constitution, s.Attack, s.Speed, s.Defense, s.Luck, s.Evasion), dna, equipDb);

        var c = new Combatant
        {
            Dna     = dna,
            Name    = dna.CustomName,
            IsA     = isA,
            MaxHp   = eff.Constitution * BaseHpCombatMultiplier,
            Attack  = eff.Attack,
            Speed   = eff.Speed,
            Defense = eff.Defense,
            Luck    = eff.Luck,
            Evasion = eff.Evasion,
            Procs   = CollectProcs(dna, equipDb),
        };
        c.Hp = c.MaxHp;
        return c;
    }

    private static List<CombatProcEffect> CollectProcs(CreatureDNA dna, EquipmentDatabaseSO equipDb)
    {
        var list = new List<CombatProcEffect>();
        if (equipDb == null || dna.Equipped == null) return list;
        foreach (var id in dna.Equipped.Values)
        {
            var item = equipDb.GetByID(id);
            if (item?.Effects == null) continue;
            foreach (var e in item.Effects)
                if (e is CombatProcEffect proc) list.Add(proc);
        }
        return list;
    }

    // ── Stats ──────────────────────────────────────────────────────

    private struct Stats
    {
        public float Constitution;
        public float Attack;
        public float Speed;
        public float Defense;
        public float Luck;
        public float Evasion;
    }

    public readonly struct EffectiveStats
    {
        public readonly float Constitution;
        public readonly float Attack;
        public readonly float Speed;
        public readonly float Defense;
        public readonly float Luck;
        public readonly float Evasion;
        public EffectiveStats(float con, float atk, float spd, float def, float lck, float eva)
        { Constitution = con; Attack = atk; Speed = spd; Defense = def; Luck = lck; Evasion = eva; }
    }

    public static EffectiveStats GetEffectiveStats(CreatureDNA dna, CreatureDatabaseSO db)
    {
        var s = ComputeStats(dna, db);
        return new EffectiveStats(s.Constitution, s.Attack, s.Speed, s.Defense, s.Luck, s.Evasion);
    }

    private static Stats ComputeStats(CreatureDNA dna, CreatureDatabaseSO db)
    {
        float con = dna.BaseConstitution;
        float atk = dna.BaseAttack;
        float spd = dna.BaseSpeed;

        AccumulatePart(db.GetBodyShape(dna.BodyShapeID), dna.BodyTier,  ref con, ref atk, ref spd);
        AccumulatePart(db.GetArm(dna.ArmID),             dna.ArmTier,   ref con, ref atk, ref spd);
        AccumulatePart(db.GetEye(dna.EyeID),             dna.EyeTier,   ref con, ref atk, ref spd);
        AccumulatePart(db.GetMouth(dna.MouthID),         dna.MouthTier, ref con, ref atk, ref spd);

        return new Stats
        {
            Constitution = con,
            Attack       = atk,
            Speed        = spd,
            Defense      = dna.BaseDefense,
            Luck         = dna.BaseLuck,
            Evasion      = dna.BaseEvasion,
        };
    }

    private static void AccumulatePart(BodyPart part, Tier tier, ref float con, ref float atk, ref float spd)
    {
        if (part == null) return;
        int bonus = (int)tier - 1;
        con += part.HP     + bonus;
        atk += part.Attack + bonus;
        spd += part.Speed  + bonus;
    }

    private static void RecordHistory(
        CreatureDNA self, CreatureDNA opponent, CombatOutcome outcome,
        bool died, string evolvedSlot, bool selfIsA, List<CombatTurn> turns)
    {
        self.CombatHistory ??= new List<CombatRecord>();
        self.CombatHistory.Add(new CombatRecord
        {
            OpponentName       = opponent.CustomName,
            OpponentPlayerName = "",
            Date               = DateTime.UtcNow,
            Outcome            = outcome,
            Died               = died,
            EvolvedSlot        = outcome == CombatOutcome.Won ? evolvedSlot : null,
            SelfWasA           = selfIsA,
            Turns              = turns,
        });
    }

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
