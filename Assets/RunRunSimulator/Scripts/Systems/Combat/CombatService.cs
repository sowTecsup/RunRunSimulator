using System;
using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

// Deterministic turn-based combat simulator: same seed + same DNA snapshots
// always produce the same CombatResult (and therefore the same CombatRecord)
// on any client, local or async. All randomness is drawn from an injected
// CombatRng — never UnityEngine.Random.
// Fixed roll order, per attacker's turn, each round: tick active statuses →
// fire passive procs → stun check (skip turn if stunned, grant wake-up
// immunity when the stun just expired, otherwise consume one immunity turn)
// → roll offensive procs → evasion → crit → on-connect resolves the rolled
// offensive procs plus the defender's defensive procs. Both combatants take
// their turn (higher Speed first) every round until someone reaches 0 HP,
// then: evolution roll → death roll. Combat procs come from equipment
// (CombatProcEffect) and act through ICombatContext / CombatResolver, which
// also owns the anti-permastun guard (no re-stun while already stunned,
// wake-up immunity window) and status stacking.
// SimulateCore is the pure deterministic core: no registry, no validation, no
// CombatRecord writes — it only mutates the CreatureDNA it's given, so async
// combat can run it against ephemeral DNA snapshots on both clients and land
// on an identical record. Simulate is the local wrapper: registry validation,
// then SimulateCore, then BuildRecord appends a CombatRecord to both live
// DNA's CombatHistory. BuildRecord is also used by the async flow to build
// each side's record from the shared seeded result.
public static class CombatService
{
    public static CombatResult Simulate(
        string              idA,
        string              idB,
        CreatureRegistrySO  registry,
        CreatureDatabaseSO  db,
        CombatManagerSO     config,
        EquipmentDatabaseSO equipDb,
        int                 seed)
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

        var rng    = new CombatRng(seed);
        var result = SimulateCore(dnaA, dnaB, db, config, equipDb, rng);
        if (result == null) return null;

        var date = System.DateTime.UtcNow;
        dnaA.CombatHistory ??= new List<CombatRecord>();
        dnaB.CombatHistory ??= new List<CombatRecord>();
        dnaA.CombatHistory.Add(BuildRecord(result, dnaA, dnaB, true,  "", "", seed, date));
        dnaB.CombatHistory.Add(BuildRecord(result, dnaB, dnaA, false, "", "", seed, date));
        return result;
    }

    public static CombatResult SimulateCore(
        CreatureDNA         dnaA,
        CreatureDNA         dnaB,
        CreatureDatabaseSO  db,
        CombatManagerSO     config,
        EquipmentDatabaseSO equipDb,
        CombatRng           rng)
    {
        var result = new CombatResult();
        var A = BuildCombatant(dnaA, db, equipDb, true);
        var B = BuildCombatant(dnaB, db, equipDb, false);
        result.StatsA = Snapshot(A);
        result.StatsB = Snapshot(B);

        result.Log.Add("=== COMBAT START ===");
        result.Log.Add($"[A] \"{A.Name}\"  {Clip(dnaA.UniqueID)}  HP:{A.MaxHp:F1}  ATK:{A.Attack:F1}  SPD:{A.Speed:F1}  DEF:{A.Defense:F0}  LCK:{A.Luck:F0}  EVA:{A.Evasion:F0}");
        result.Log.Add($"[B] \"{B.Name}\"  {Clip(dnaB.UniqueID)}  HP:{B.MaxHp:F1}  ATK:{B.Attack:F1}  SPD:{B.Speed:F1}  DEF:{B.Defense:F0}  LCK:{B.Luck:F0}  EVA:{B.Evasion:F0}");

        var resolver = new CombatResolver { Result = result, Synergies = config.Synergies };
        bool someoneKO = false;

        for (int round = 1; round <= config.MaxRounds; round++)
        {
            bool aFirst = A.EffSpeed > B.EffSpeed ||
                          (Mathf.Approximately(A.EffSpeed, B.EffSpeed) && rng.NextFloat() < 0.5f);

            result.Log.Add($"--- Round {round} (first: {(aFirst ? "A" : "B")}) ---");

            var first  = aFirst ? A : B;
            var second = aFirst ? B : A;

            if (TakeTurn(first,  second, config, result, round, resolver, rng)) { someoneKO = true; break; }
            if (TakeTurn(second, first,  config, result, round, resolver, rng)) { someoneKO = true; break; }
        }

        if (!someoneKO)
        {
            result.IsDraw = true;
            dnaA.FightCount++;
            dnaB.FightCount++;
            result.Log.Add($"=== DRAW — {config.MaxRounds} rounds reached. A:{A.Hp:F1}HP  B:{B.Hp:F1}HP ===");
            result.Log.Add("[DRAW] No consequences for either fighter.");
            result.Log.Add("=== COMBAT END === DRAW");
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

        result.EvolvedSlot   = CombatEvolution.TryEvolveRandomSlot(winner, rng);
        result.WinnerEvolved = result.EvolvedSlot != null;
        result.Log.Add(result.WinnerEvolved
            ? $"[EVOLUTION] \"{winner.CustomName}\" — {result.EvolvedSlot} evolved to Tier{CombatEvolution.GetSlotTier(winner, result.EvolvedSlot)}!"
            : $"[EVOLUTION] \"{winner.CustomName}\" — all parts already at max Tier.");

        if (rng.NextFloat() < config.DeathChance)
        {
            loser.IsDead     = true;
            result.LoserDied = true;
            result.Log.Add("[DEATH] Loser has perished permanently.");
        }

        string evolvedLine = result.WinnerEvolved ? $" | Evolved: {result.EvolvedSlot} → Tier{CombatEvolution.GetSlotTier(winner, result.EvolvedSlot)}" : "";
        result.Log.Add($"=== COMBAT END === Winner: \"{winner.CustomName}\"  {winner.UniqueID}{evolvedLine}");

        return result;
    }

    public static CombatRecord BuildRecord(CombatResult result, CreatureDNA self, CreatureDNA opponent,
        bool selfWasA, string opponentPlayerName, string opponentPlayerId, int seed, System.DateTime date)
    {
        bool won = !result.IsDraw && result.WinnerID == self.UniqueID;
        return new CombatRecord
        {
            OpponentName       = opponent.CustomName,
            OpponentPlayerName = opponentPlayerName ?? "",
            OpponentPlayerId   = opponentPlayerId ?? "",
            OpponentDnaId      = opponent.UniqueID,
            Seed               = seed,
            Date               = date,
            Outcome            = result.IsDraw ? CombatOutcome.Draw : (won ? CombatOutcome.Won : CombatOutcome.Lost),
            Died               = !won && !result.IsDraw && result.LoserDied,
            EvolvedSlot        = won ? result.EvolvedSlot : null,
            SelfStats          = selfWasA ? result.StatsA : result.StatsB,
            OpponentStats      = selfWasA ? result.StatsB : result.StatsA,
            SelfWasA           = selfWasA,
            Turns              = result.Turns,
        };
    }

    // ── Turn ───────────────────────────────────────────────────────

    // Returns true if combat should end (someone reached 0 HP).
    private static bool TakeTurn(Combatant atk, Combatant def, CombatManagerSO config, CombatResult result, int round, CombatResolver r, CombatRng rng)
    {
        var procs = new List<CombatProcEvent>();
        r.TurnProcs = procs;

        result.Log.Add($"  » Turno de {atk.Name}");

        r.BeforeStrike = true;
        TickStatuses(atk, result, r);
        if (atk.Hp <= 0f)
        {
            result.Log.Add($"    [{atk.Name}] succumbs to its afflictions.");
            EmitTurn(result, round, atk, def, true, 0f, false, def.Hp, procs);
            return true;
        }

        FireProcs(atk, def, TriggerType.Passive, result, r, true, rng);
        if (atk.Hp <= 0f || def.Hp <= 0f)
        {
            EmitTurn(result, round, atk, def, true, 0f, false, def.Hp, procs);
            return true;
        }

        if (atk.StunTurns > 0)
        {
            atk.StunTurns--;
            if (atk.StunTurns == 0) atk.StunImmunityTurns = config.StunImmunityTurns;
            result.Log.Add($"    [{atk.Name}] is stunned — skips turn ({atk.StunTurns} left)");
            r.Record(ModifierEffectKind.Stun, atk, atk.StunTurns + 1);
            EmitTurn(result, round, atk, def, true, 0f, false, def.Hp, procs);
            return false;
        }
        if (atk.StunImmunityTurns > 0) atk.StunImmunityTurns--;

        var armed = new List<CombatProcEffect>();
        foreach (var p in atk.Procs)
            if (p.Trigger == TriggerType.Offensive && RollProc(p, atk, result, rng))
                armed.Add(p);

        float evaChance = def.EffEvasion * config.EvasionPerPoint;
        float evaRoll   = rng.NextFloat();
        bool  dodged    = evaRoll < evaChance;
        bool  crit       = false;
        float critChance = 0f;
        float critRoll   = 1f;
        float damage     = 0f;
        if (!dodged)
        {
            critChance      = config.CritChance + atk.Luck * config.LuckCritPerPoint;
            critRoll        = rng.NextFloat();
            crit            = critRoll < critChance;
            float raw       = atk.Attack * (crit ? config.CritMultiplier : 1f);
            float reduction = Mathf.Clamp01(def.EffDefense * config.DefenseReductionPerPoint);
            damage          = raw * (1f - reduction);
            def.Hp          = Mathf.Max(0f, def.Hp - damage);
        }
        float defHpAfterStrike = def.Hp;

        string dir = atk.IsA ? "A→B" : "B→A";
        result.Log.Add(dodged
            ? $"    [{dir}] DODGE!  {def.Name} HP:{def.Hp:F1}   (eva {evaRoll * 100f:F0} vs {evaChance * 100f:F0}%)"
            : $"    [{dir}]{(crit ? " CRIT!" : "")} dmg:{damage:F1}  {def.Name} HP:{def.Hp:F1}   (eva {evaRoll * 100f:F0} vs {evaChance * 100f:F0}% · crit {critRoll * 100f:F0} vs {critChance * 100f:F0}%)");

        r.BeforeStrike = false;
        if (!dodged && damage > 0f && atk.LifestealPercent > 0f)
        {
            float steal = damage * atk.LifestealPercent;
            atk.Hp = Mathf.Min(atk.MaxHp, atk.Hp + steal);
            result.Log.Add($"    [Lifesteal] {atk.Name} +{steal:F1} → {atk.Hp:F1}");
            r.Record(ModifierEffectKind.Lifesteal, atk, steal);
        }
        if (!dodged && def.Hp > 0f)
        {
            foreach (var p in armed) { r.Self = atk; r.Opponent = def; p.Apply(r); }
            FireProcs(def, atk, TriggerType.Defensive, result, r, true, rng);
        }

        EmitTurn(result, round, atk, def, false, damage, crit, defHpAfterStrike, procs);
        return atk.Hp <= 0f || def.Hp <= 0f;
    }

    private static void EmitTurn(CombatResult result, int round, Combatant atk, Combatant def,
        bool noAttack, float damage, bool crit, float defHp, List<CombatProcEvent> procs)
    {
        result.Turns.Add(new CombatTurn
        {
            TurnNumber      = round,
            AttackerName    = atk.Name,
            DefenderName    = def.Name,
            AttackerIsA     = atk.IsA,
            Damage          = damage,
            WasCrit         = crit,
            DefenderHpAfter = defHp,
            NoAttack        = noAttack,
            Procs           = procs,
            StatusA         = CombatResolver.StatusMarks(atk.IsA ? atk : def),
            StatusB         = CombatResolver.StatusMarks(atk.IsA ? def : atk),
        });
    }

    private static void TickStatuses(Combatant c, CombatResult result, CombatResolver r)
    {
        for (int i = c.Active.Count - 1; i >= 0; i--)
        {
            var a    = c.Active[i];
            int left = a.RemainingTurns - 1;
            switch (a.Kind)
            {
                case ModifierEffectKind.Poison:
                case ModifierEffectKind.Burn:
                    c.Hp = Mathf.Max(0f, c.Hp - a.Magnitude);
                    result.Log.Add($"    [{a.Kind}] {c.Name} takes {a.Magnitude} {a.Kind} damage → {c.Hp:F1} ({left}t left)");
                    r.Record(a.Kind, c, a.Magnitude);
                    break;
                case ModifierEffectKind.Regen:
                case ModifierEffectKind.Pulse:
                    c.Hp = Mathf.Min(c.MaxHp, c.Hp + a.Magnitude);
                    result.Log.Add($"    [{a.Kind}] {c.Name} regenerates {a.Magnitude} HP → {c.Hp:F1} ({left}t left)");
                    r.Record(a.Kind, c, a.Magnitude);
                    break;
            }
            if (--a.RemainingTurns <= 0) c.Active.RemoveAt(i);
        }
    }

    private static void FireProcs(Combatant owner, Combatant opponent, TriggerType trigger, CombatResult result, CombatResolver r, bool roll, CombatRng rng)
    {
        foreach (var p in owner.Procs)
        {
            if (p.Trigger != trigger) continue;
            if (roll && !RollProc(p, owner, result, rng)) continue;
            r.Self     = owner;
            r.Opponent = opponent;
            p.Apply(r);
        }
    }

    private static bool RollProc(CombatProcEffect p, Combatant owner, CombatResult result, CombatRng rng)
    {
        float roll = rng.NextFloat();
        bool  pass = roll < p.ProcChance / 100f;
        result.Log.Add($"    [roll] {owner.Name} {p.Kind} ({TriggerLabel(p.Trigger)}, {p.ProcChance}%) → {roll * 100f:F0}: {(pass ? "PROC" : "no proc")}");
        return pass;
    }

    private static string TriggerLabel(TriggerType t) => t switch
    {
        TriggerType.Offensive => "on hit",
        TriggerType.Defensive => "when hit",
        TriggerType.Passive   => "passive",
        _                     => "",
    };

    private static Combatant BuildCombatant(CreatureDNA dna, CreatureDatabaseSO db, EquipmentDatabaseSO equipDb, bool isA)
    {
        var baseStats = CombatStats.GetEffectiveStats(dna, db);
        var eff       = EquipmentStats.Apply(baseStats, dna, equipDb);

        var c = new Combatant
        {
            Dna     = dna,
            Name    = dna.CustomName,
            IsA     = isA,
            MaxHp   = eff.Constitution * CombatStats.BaseHpCombatMultiplier,
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
        foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
        {
            if (!dna.Equipped.TryGetValue(slot, out var id)) continue;
            var item = equipDb.GetByID(id);
            if (item?.Effects == null) continue;
            foreach (var e in item.Effects)
                if (e is CombatProcEffect proc) list.Add(proc);
        }
        return list;
    }

    private static string Clip(string id) => id[..Mathf.Min(14, id.Length)];

    private static CombatFighterSnapshot Snapshot(Combatant c) => new CombatFighterSnapshot
    {
        MaxHp     = c.MaxHp,
        Attack    = c.Attack,
        Speed     = c.Speed,
        Defense   = c.Defense,
        Luck      = c.Luck,
        Evasion   = c.Evasion,
        BodyTier  = (int)c.Dna.BodyTier,
        ArmTier   = (int)c.Dna.ArmTier,
        EyeTier   = (int)c.Dna.EyeTier,
        MouthTier = (int)c.Dna.MouthTier,
        ColorHex  = ColorUtility.ToHtmlStringRGB(c.Dna.BaseColor),
    };

}
}
