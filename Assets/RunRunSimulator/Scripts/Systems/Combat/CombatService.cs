using System;
using System.Collections.Generic;
using UnityEngine;
namespace MoriMonchiSimulator
{

// Deterministic team-based combat simulator (3v3, tolerates 1..3 per side):
// same seed + same DNA snapshots + same rows always produce the same
// CombatResult (and therefore the same CombatRecord) on any client, local or
// async. All randomness is drawn from an injected CombatRng — never
// UnityEngine.Random.
//
// Fixed roll order, per round: one speed tiebreak roll PER ALIVE UNIT (both
// teams, in fixed A0..An,B0..Bn order) BEFORE sorting the turn order — this
// keeps rng consumption constant regardless of who is faster. Units then act
// in (EffSpeed desc, tiebreak desc, side A-before-B, Index asc) order. Per
// turn: tick active statuses → (Agresivo role: roll backline chance → pick
// backline target if it lands, else pick frontline) / plain pick frontline →
// fire passive procs → stun check (skip turn if stunned, grant wake-up
// immunity when the stun just expired) → Protector role: pick an ally and
// grant it Shield (no roll cost when ShieldPerTurn is 0) → roll offensive
// procs → evasion roll → crit roll → on-connect damage, Shield absorbs first
// → Empático role: heal the lowest-HP ally for a % of the damage dealt (no
// roll) → lifesteal → on-connect resolves the rolled offensive procs plus
// the defender's defensive procs. Every alive unit takes its turn once per
// round until a team is fully wiped, then: pick one alive winner (uniform)
// → CombatEvolution.TryEvolveRandomSlot roll → pick one loser unit (KO or
// not, uniform) → DeathChance roll. Combat procs come from equipment
// (CombatProcEffect) and act through ICombatContext / CombatResolver, which
// also owns the anti-permastun guard (no re-stun while already stunned,
// wake-up immunity window), status stacking and synergy detonation.
// SimulateCore is the pure deterministic core: no registry, no validation, no
// CombatRecord writes — it only mutates the CreatureDNA it's given, so async
// combat can run it against ephemeral DNA snapshots on both clients and land
// on an identical record. Simulate is the local wrapper: registry
// validation + row-lineup validation, then SimulateCore, then BuildRecord
// appends a CombatRecord to every live DNA's CombatHistory. BuildRecord is
// also used by the async flow to build each side's record from the shared
// seeded result.
public static class CombatService
{
    private static readonly int[] DefaultLineup = { (int)CombatRow.Front, (int)CombatRow.Front, (int)CombatRow.Mid };

    public static CombatResult Simulate(
        List<string>        idsA,
        List<string>        idsB,
        List<int>           rowsA,
        List<int>           rowsB,
        CreatureRegistrySO  registry,
        CreatureDatabaseSO  db,
        CombatManagerSO     config,
        EquipmentDatabaseSO equipDb,
        int                 seed)
    {
        if (idsA == null || idsB == null)
        {
            Debug.LogError("[CombatService] Team lists cannot be null.");
            return null;
        }
        if (idsA.Count != idsB.Count)
        {
            Debug.LogError($"[CombatService] Both teams must have the same size (A:{idsA.Count} B:{idsB.Count}).");
            return null;
        }
        if (idsA.Count < 1 || idsA.Count > 3)
        {
            Debug.LogError($"[CombatService] Team size must be 1-3, got {idsA.Count}.");
            return null;
        }

        var allIds = new List<string>(idsA);
        allIds.AddRange(idsB);
        var seenIds = new HashSet<string>();
        foreach (var id in allIds)
        {
            if (!seenIds.Add(id))
            {
                Debug.LogError($"[CombatService] Duplicate creature ID '{id}' across the two teams.");
                return null;
            }
        }

        var dnasA = new List<CreatureDNA>();
        var dnasB = new List<CreatureDNA>();
        if (!ValidateTeam(idsA, registry, config, dnasA)) return null;
        if (!ValidateTeam(idsB, registry, config, dnasB)) return null;

        if (!ResolveRows(idsA.Count, rowsA, out var resolvedRowsA)) return null;
        if (!ResolveRows(idsB.Count, rowsB, out var resolvedRowsB)) return null;

        var rng    = new CombatRng(seed);
        var result = SimulateCore(dnasA, dnasB, resolvedRowsA, resolvedRowsB, db, config, equipDb, rng);
        if (result == null) return null;

        var date = DateTime.UtcNow;
        foreach (var dna in dnasA)
        {
            dna.CombatHistory ??= new List<CombatRecord>();
            dna.CombatHistory.Add(BuildRecord(result, dnasA, dnasB, dna, true, "", "", seed, date));
        }
        foreach (var dna in dnasB)
        {
            dna.CombatHistory ??= new List<CombatRecord>();
            dna.CombatHistory.Add(BuildRecord(result, dnasB, dnasA, dna, false, "", "", seed, date));
        }
        return result;
    }

    private static bool ValidateTeam(List<string> ids, CreatureRegistrySO registry, CombatManagerSO config, List<CreatureDNA> outDnas)
    {
        foreach (var id in ids)
        {
            if (!registry.TryGet(id, out var dna))
            {
                Debug.LogError($"[CombatService] ID '{id}' not found in registry.");
                return false;
            }
            if (dna.IsDead)
            {
                Debug.LogError($"[CombatService] Cannot simulate combat: '{id}' is dead.");
                return false;
            }
            if (dna.IsBusy)
            {
                Debug.LogError($"[CombatService] Cannot simulate combat: '{id}' is busy (queued for async combat).");
                return false;
            }
            if (dna.FightCount >= config.MaxFightCount)
            {
                Debug.LogError($"[CombatService] '{id}' has no fights remaining ({dna.FightCount}/{config.MaxFightCount}).");
                return false;
            }
            outDnas.Add(dna);
        }
        return true;
    }

    private static bool ResolveRows(int count, List<int> rows, out List<int> resolved)
    {
        resolved = new List<int>();
        if (rows == null)
        {
            for (int i = 0; i < count; i++) resolved.Add(DefaultLineup[i]);
            return true;
        }
        if (rows.Count != count)
        {
            Debug.LogError($"[CombatService] Row list size ({rows.Count}) must match team size ({count}).");
            return false;
        }

        int front = 0, mid = 0, back = 0;
        foreach (var r in rows)
        {
            switch ((CombatRow)r)
            {
                case CombatRow.Front: front++; break;
                case CombatRow.Mid:   mid++;   break;
                case CombatRow.Back:  back++;  break;
            }
            resolved.Add(r);
        }
        if (front > 2 || mid > 3 || back > 2)
        {
            Debug.LogError($"[CombatService] Row lineup exceeds grid capacity (Front≤2, Mid≤3, Back≤2). Got Front:{front} Mid:{mid} Back:{back}.");
            return false;
        }
        return true;
    }

    public static CombatResult SimulateCore(
        List<CreatureDNA>   teamA,
        List<CreatureDNA>   teamB,
        List<int>           rowsA,
        List<int>           rowsB,
        CreatureDatabaseSO  db,
        CombatManagerSO     config,
        EquipmentDatabaseSO equipDb,
        CombatRng           rng)
    {
        var result = new CombatResult();

        var A = new List<Combatant>();
        for (int i = 0; i < teamA.Count; i++)
            A.Add(BuildCombatant(teamA[i], db, equipDb, true, i, (CombatRow)rowsA[i], config.Roles));
        var B = new List<Combatant>();
        for (int i = 0; i < teamB.Count; i++)
            B.Add(BuildCombatant(teamB[i], db, equipDb, false, i, (CombatRow)rowsB[i], config.Roles));

        foreach (var c in A) result.TeamA.Add(Snapshot(c));
        foreach (var c in B) result.TeamB.Add(Snapshot(c));

        result.Log.Add("=== COMBAT START ===");
        foreach (var c in A)
            result.Log.Add($"[A{c.Index}] \"{c.Name}\" ({c.Role}, {c.Row})  HP:{c.MaxHp:F1}  ATK:{c.Attack:F1}  SPD:{c.Speed:F1}  DEF:{c.Defense:F0}  LCK:{c.Luck:F0}  EVA:{c.Evasion:F0}");
        foreach (var c in B)
            result.Log.Add($"[B{c.Index}] \"{c.Name}\" ({c.Role}, {c.Row})  HP:{c.MaxHp:F1}  ATK:{c.Attack:F1}  SPD:{c.Speed:F1}  DEF:{c.Defense:F0}  LCK:{c.Luck:F0}  EVA:{c.Evasion:F0}");

        var resolver = new CombatResolver { Result = result, Synergies = config.Synergies };
        bool wiped = false;

        for (int round = 1; round <= config.MaxRounds; round++)
        {
            var order = BuildTurnOrder(A, B, rng);

            var orderLabels = new List<string>();
            foreach (var c in order) orderLabels.Add($"{(c.IsA ? "A" : "B")}{c.Index}");
            result.Log.Add($"--- Round {round} ---");
            result.Log.Add($"    order: {string.Join(" ", orderLabels)}");

            foreach (var actor in order)
            {
                if (!actor.IsAlive) continue;
                if (AllDead(A) || AllDead(B)) { wiped = true; break; }

                var allies  = actor.IsA ? A : B;
                var enemies = actor.IsA ? B : A;
                TakeTurn(actor, allies, enemies, config, result, round, resolver, rng, A, B);

                if (AllDead(A) || AllDead(B)) { wiped = true; break; }
            }

            if (wiped) break;
        }

        if (!wiped)
        {
            result.IsDraw = true;
            foreach (var dna in teamA) dna.FightCount++;
            foreach (var dna in teamB) dna.FightCount++;
            result.Log.Add($"=== DRAW — {config.MaxRounds} rounds reached. {TeamHpSummary(A)} | {TeamHpSummary(B)} ===");
            result.Log.Add("[DRAW] No consequences for either team.");
            result.Log.Add("=== COMBAT END === DRAW");
            return result;
        }

        result.TeamAWon = AllDead(B);
        var winnerCombatants = result.TeamAWon ? A : B;
        var loserCombatants  = result.TeamAWon ? B : A;

        result.Log.Add($"=== WIPE === Team {(result.TeamAWon ? "A" : "B")} wins | {TeamHpSummary(A)} | {TeamHpSummary(B)} ===");

        foreach (var dna in teamA) dna.FightCount++;
        foreach (var dna in teamB) dna.FightCount++;
        foreach (var dna in (result.TeamAWon ? teamA : teamB)) dna.WinCount++;

        string evolvedLine = "";
        var evoCandidates = new List<Combatant>();
        foreach (var c in winnerCombatants) if (c.IsAlive) evoCandidates.Add(c);
        if (evoCandidates.Count > 0)
        {
            var unit = evoCandidates[rng.Range(0, evoCandidates.Count)];
            result.EvolvedSlot = CombatEvolution.TryEvolveRandomSlot(unit.Dna, rng);
            if (result.EvolvedSlot != null)
            {
                result.EvolvedUnitId   = unit.Dna.UniqueID;
                result.EvolvedUnitName = unit.Dna.CustomName;
                evolvedLine = $" | Evolved: \"{unit.Name}\" {result.EvolvedSlot} → Tier{CombatEvolution.GetSlotTier(unit.Dna, result.EvolvedSlot)}";
                result.Log.Add($"[EVOLUTION] \"{unit.Name}\" — {result.EvolvedSlot} evolved to Tier{CombatEvolution.GetSlotTier(unit.Dna, result.EvolvedSlot)}!");
            }
            else
            {
                result.Log.Add($"[EVOLUTION] \"{unit.Name}\" — all parts already at max Tier.");
            }
        }

        string diedLine = "";
        if (loserCombatants.Count > 0)
        {
            var unit = loserCombatants[rng.Range(0, loserCombatants.Count)];
            if (rng.NextFloat() < config.DeathChance)
            {
                unit.Dna.IsDead     = true;
                result.DiedUnitId   = unit.Dna.UniqueID;
                result.DiedUnitName = unit.Dna.CustomName;
                diedLine = $" | Died: \"{unit.Name}\"";
                result.Log.Add($"[DEATH] \"{unit.Name}\" ha muerto permanentemente.");
            }
            else
            {
                result.Log.Add($"[DEATH] \"{unit.Name}\" survives the death roll.");
            }
        }

        result.Log.Add($"=== COMBAT END === Winner: Team {(result.TeamAWon ? "A" : "B")}{evolvedLine}{diedLine}");
        return result;
    }

    public static CombatRecord BuildRecord(
        CombatResult       result,
        List<CreatureDNA>  selfTeam,
        List<CreatureDNA>  opponentTeam,
        CreatureDNA        self,
        bool               selfWasA,
        string             opponentPlayerName,
        string             opponentPlayerId,
        int                seed,
        DateTime           date)
    {
        bool won = !result.IsDraw && (selfWasA == result.TeamAWon);

        var opponentNames = new List<string>();
        foreach (var dna in opponentTeam) opponentNames.Add(dna.CustomName);

        var selfTeamIds = new List<string>();
        foreach (var dna in selfTeam) selfTeamIds.Add(dna.UniqueID);
        var opponentTeamIds = new List<string>();
        foreach (var dna in opponentTeam) opponentTeamIds.Add(dna.UniqueID);

        return new CombatRecord
        {
            OpponentName       = string.Join(" · ", opponentNames),
            OpponentPlayerName = opponentPlayerName ?? "",
            OpponentPlayerId   = opponentPlayerId ?? "",
            OpponentDnaId      = "",
            Seed               = seed,
            Date               = date,
            Outcome            = result.IsDraw ? CombatOutcome.Draw : (won ? CombatOutcome.Won : CombatOutcome.Lost),
            Died               = result.DiedUnitId == self.UniqueID,
            EvolvedSlot        = result.EvolvedUnitId == self.UniqueID ? result.EvolvedSlot : null,
            SelfStats          = null,
            OpponentStats      = null,
            SelfWasA           = selfWasA,
            Turns              = result.Turns,
            SelfTeam           = selfWasA ? result.TeamA : result.TeamB,
            OpponentTeam       = selfWasA ? result.TeamB : result.TeamA,
            SelfTeamIds        = selfTeamIds,
            OpponentTeamIds    = opponentTeamIds,
        };
    }

    // ── Turn order ─────────────────────────────────────────────────

    private static List<Combatant> BuildTurnOrder(List<Combatant> A, List<Combatant> B, CombatRng rng)
    {
        var order = new List<Combatant>();
        foreach (var c in A) if (c.IsAlive) order.Add(c);
        foreach (var c in B) if (c.IsAlive) order.Add(c);

        var tiebreak = new Dictionary<Combatant, float>();
        foreach (var c in order) tiebreak[c] = rng.NextFloat();

        order.Sort((x, y) =>
        {
            int cmp = y.EffSpeed.CompareTo(x.EffSpeed);
            if (cmp != 0) return cmp;
            cmp = tiebreak[y].CompareTo(tiebreak[x]);
            if (cmp != 0) return cmp;
            cmp = (x.IsA ? 0 : 1).CompareTo(y.IsA ? 0 : 1);
            if (cmp != 0) return cmp;
            return x.Index.CompareTo(y.Index);
        });

        return order;
    }

    private static bool AllDead(List<Combatant> team)
    {
        foreach (var c in team) if (c.IsAlive) return false;
        return true;
    }

    private static Combatant FirstAlive(List<Combatant> team)
    {
        foreach (var c in team) if (c.IsAlive) return c;
        return null;
    }

    private static string TeamHpSummary(List<Combatant> team)
    {
        var parts = new List<string>();
        foreach (var c in team) parts.Add($"{(c.IsA ? "A" : "B")}{c.Index}:{Mathf.Max(0f, c.Hp):F1}HP");
        return string.Join(" ", parts);
    }

    // ── Turn ───────────────────────────────────────────────────────

    private static void TakeTurn(Combatant actor, List<Combatant> allies, List<Combatant> enemies,
        CombatManagerSO config, CombatResult result, int round, CombatResolver r, CombatRng rng,
        List<Combatant> teamA, List<Combatant> teamB)
    {
        var procs = new List<CombatProcEvent>();
        r.TurnProcs = procs;

        result.Log.Add($"  » Turno de {(actor.IsA ? "A" : "B")}{actor.Index} \"{actor.Name}\"");

        r.BeforeStrike = true;
        TickStatuses(actor, result, r);
        if (actor.Hp <= 0f)
        {
            result.Log.Add($"    [{actor.Name}] succumbs to its afflictions.");
            var fallbackTarget = FirstAlive(enemies);
            EmitTurn(result, round, actor, fallbackTarget, true, 0f, false, fallbackTarget.Hp, fallbackTarget.Shield, procs, teamA, teamB);
            return;
        }

        var profile = config.Roles != null ? config.Roles.GetProfile(actor.Role) : null;
        Combatant target;
        if (profile != null && profile.BacklineHitChance > 0f)
        {
            float aggroRoll = rng.NextFloat();
            if (aggroRoll < profile.BacklineHitChance)
            {
                var back = CombatTargeting.PickBacklineTarget(enemies, rng);
                if (back != null)
                {
                    target = back;
                    result.Log.Add($"    [Agresivo] {actor.Name} caza la backline");
                }
                else
                {
                    result.Log.Add("    [Agresivo] sin backline — comparte energía (sin efecto)");
                    target = CombatTargeting.PickFrontTarget(enemies, rng);
                }
            }
            else
            {
                target = CombatTargeting.PickFrontTarget(enemies, rng);
            }
        }
        else
        {
            target = CombatTargeting.PickFrontTarget(enemies, rng);
        }

        r.Self = actor;
        r.Opponent = target;
        FireProcs(actor, target, TriggerType.Passive, result, r, true, rng);
        if (actor.Hp <= 0f || target.Hp <= 0f)
        {
            EmitTurn(result, round, actor, target, true, 0f, false, target.Hp, target.Shield, procs, teamA, teamB);
            return;
        }

        if (actor.StunTurns > 0)
        {
            actor.StunTurns--;
            if (actor.StunTurns == 0) actor.StunImmunityTurns = config.StunImmunityTurns;
            result.Log.Add($"    [{actor.Name}] is stunned — skips turn ({actor.StunTurns} left)");
            r.Record(ModifierEffectKind.Stun, actor, actor.StunTurns + 1);
            EmitTurn(result, round, actor, target, true, 0f, false, target.Hp, target.Shield, procs, teamA, teamB);
            return;
        }
        if (actor.StunImmunityTurns > 0) actor.StunImmunityTurns--;

        if (profile != null && profile.ShieldPerTurn > 0f)
        {
            var ally = CombatTargeting.PickAlly(allies, rng);
            ally.Shield += profile.ShieldPerTurn;
            result.Log.Add($"    [Protector] {actor.Name} escuda a {ally.Name} +{profile.ShieldPerTurn:F0} (escudo {ally.Shield:F0})");
            r.Record(ModifierEffectKind.Shield, ally, profile.ShieldPerTurn);
        }

        var armed = new List<CombatProcEffect>();
        foreach (var p in actor.Procs)
            if (p.Trigger == TriggerType.Offensive && RollProc(p, actor, result, rng))
                armed.Add(p);

        float evaChance = target.EffEvasion * config.EvasionPerPoint;
        float evaRoll   = rng.NextFloat();
        bool  dodged    = evaRoll < evaChance;
        bool  crit        = false;
        float critChance  = 0f;
        float critRoll    = 1f;
        float damage      = 0f;
        float absorbed    = 0f;
        if (!dodged)
        {
            critChance      = config.CritChance + actor.Luck * config.LuckCritPerPoint;
            critRoll        = rng.NextFloat();
            crit             = critRoll < critChance;
            float raw       = actor.Attack * (crit ? config.CritMultiplier : 1f);
            float reduction = Mathf.Clamp01(target.EffDefense * config.DefenseReductionPerPoint);
            damage          = raw * (1f - reduction);
            absorbed        = Mathf.Min(target.Shield, damage);
            target.Shield  -= absorbed;
            target.Hp       = Mathf.Max(0f, target.Hp - (damage - absorbed));
        }
        float defHpAfterStrike    = target.Hp;
        float defShieldAfterHit   = target.Shield;

        string dir         = $"{(actor.IsA ? "A" : "B")}{actor.Index}→{(target.IsA ? "A" : "B")}{target.Index}";
        string shieldNote  = absorbed > 0f ? $" (escudo -{absorbed:F1}, quedan {target.Shield:F1})" : "";
        result.Log.Add(dodged
            ? $"    [{dir}] DODGE!  {target.Name} HP:{target.Hp:F1}   (eva {evaRoll * 100f:F0} vs {evaChance * 100f:F0}%)"
            : $"    [{dir}]{(crit ? " CRIT!" : "")} dmg:{damage:F1}{shieldNote}  {target.Name} HP:{target.Hp:F1}   (eva {evaRoll * 100f:F0} vs {evaChance * 100f:F0}% · crit {critRoll * 100f:F0} vs {critChance * 100f:F0}%)");

        r.BeforeStrike = false;

        if (!dodged && damage > 0f && profile != null && profile.HealPercentOfDamage > 0f)
        {
            var healTarget = CombatTargeting.LowestHpAlly(allies);
            if (healTarget != null)
            {
                float heal = damage * profile.HealPercentOfDamage;
                healTarget.Hp = Mathf.Min(healTarget.MaxHp, healTarget.Hp + heal);
                result.Log.Add($"    [Empático] {actor.Name} cura a {healTarget.Name} +{heal:F1} → {healTarget.Hp:F1}");
                r.Record(ModifierEffectKind.Heal, healTarget, heal);
            }
        }

        if (!dodged && damage > 0f && actor.LifestealPercent > 0f)
        {
            float steal = damage * actor.LifestealPercent;
            actor.Hp = Mathf.Min(actor.MaxHp, actor.Hp + steal);
            result.Log.Add($"    [Lifesteal] {actor.Name} +{steal:F1} → {actor.Hp:F1}");
            r.Record(ModifierEffectKind.Lifesteal, actor, steal);
        }

        if (!dodged && target.Hp > 0f)
        {
            foreach (var p in armed) { r.Self = actor; r.Opponent = target; p.Apply(r); }
            FireProcs(target, actor, TriggerType.Defensive, result, r, true, rng);
        }

        EmitTurn(result, round, actor, target, false, damage, crit, defHpAfterStrike, defShieldAfterHit, procs, teamA, teamB);
    }

    private static void EmitTurn(CombatResult result, int round, Combatant actor, Combatant target,
        bool noAttack, float damage, bool crit, float defHpAfterStrike, float defShieldAfter,
        List<CombatProcEvent> procs, List<Combatant> teamA, List<Combatant> teamB)
    {
        var turn = new CombatTurn
        {
            TurnNumber          = round,
            AttackerName        = actor.Name,
            DefenderName        = target.Name,
            AttackerIsA         = actor.IsA,
            Damage              = damage,
            WasCrit             = crit,
            DefenderHpAfter     = defHpAfterStrike,
            NoAttack            = noAttack,
            Procs               = procs,
            AttackerIndex       = actor.Index,
            DefenderIndex       = target.Index,
            DefenderShieldAfter = defShieldAfter,
        };

        foreach (var u in teamA)
            turn.TeamStateA.Add(new CombatUnitState { Hp = Mathf.Max(0f, u.Hp), Shield = u.Shield, Marks = CombatResolver.StatusMarks(u) });
        foreach (var u in teamB)
            turn.TeamStateB.Add(new CombatUnitState { Hp = Mathf.Max(0f, u.Hp), Shield = u.Shield, Marks = CombatResolver.StatusMarks(u) });

        result.Turns.Add(turn);
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

    private static Combatant BuildCombatant(CreatureDNA dna, CreatureDatabaseSO db, EquipmentDatabaseSO equipDb,
        bool isA, int index, CombatRow row, RoleTableSO roles)
    {
        var baseStats = CombatStats.GetEffectiveStats(dna, db, roles);
        var eff       = EquipmentStats.Apply(baseStats, dna, equipDb);

        var c = new Combatant
        {
            Dna     = dna,
            Name    = dna.CustomName,
            IsA     = isA,
            Role    = dna.Role,
            Row     = row,
            Index   = index,
            Shield  = 0f,
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
        Name      = c.Name,
        Role      = c.Role,
        Row       = (int)c.Row,
    };

}
}
