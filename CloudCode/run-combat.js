// Cloud Code Script: run-combat
// Matchmaking + server-side combat simulation for MoriMonchis async battles.

const { DataApi } = require("@unity-services/cloud-save-1.4");

// Instant mode is a DEBUG path (skip the hourly wait): only ever one creature A and
// one creature B meet here, so it gets its OWN pool, fully isolated from the
// scheduled matchmaking_pool. No cross-contamination with timer-queued creatures.
const POOL_KEY    = "instant_pool";
const RESULTS_KEY = "combat_results";
const POOL_TTL_MS = 86400000;  // 24 h

const CRIT_CHANCE              = 0.10;
const CRIT_MULT                = 3;
const LUCK_CRIT_PER_POINT      = 0.03;
const DEF_REDUCTION_PER_POINT  = 0.08;
const EVASION_PER_POINT        = 0.10;

module.exports = async ({ params, context, logger }) => {
    const { creatureId, customName, creatureJson, playerName } = params;

    if (!creatureId || !customName || !creatureJson)
        throw new Error("Missing required params: creatureId, customName, creatureJson");

    const api = new DataApi({ accessToken: context.serviceToken });

    // ── Load pool from Custom Data (Game Data tab) ────────────────
    // Note: pool is wrapped in { entries: [...] } because Custom Data values
    // are typed as `object` and arrays at the top level get rejected (404).
    let pool = [];
    try {
        const res  = await api.getCustomItems(context.projectId, context.environmentId, [POOL_KEY]);
        const item = res.data?.results?.find(i => i.key === POOL_KEY);
        pool = Array.isArray(item?.value?.entries) ? item.value.entries : [];
    } catch (e) {
        logger.info("Pool not found, starting empty: " + (e.message || e));
    }

    const now = Date.now();
    pool = pool.filter(e => now - e.ts < POOL_TTL_MS);

    const opponentIdx = pool.findIndex(e => e.playerId !== context.playerId);

    if (opponentIdx === -1) {
        // No opponent yet — enqueue
        pool.push({ playerId: context.playerId, playerName: playerName ?? "Anonymous", creatureId, customName, creatureJson, ts: now });
        await api.setCustomItem(context.projectId, context.environmentId, {
            key:   POOL_KEY,
            value: { entries: pool },
        });
        return JSON.stringify({ status: "waiting" });
    }

    // ── Match found ───────────────────────────────────────────────
    const [opponent] = pool.splice(opponentIdx, 1);
    await api.setCustomItem(context.projectId, context.environmentId, {
        key:   POOL_KEY,
        value: { entries: pool },
    });

    const dnaA   = JSON.parse(creatureJson);
    const dnaB   = JSON.parse(opponent.creatureJson);
    const battle = simulateCombat(dnaA, customName, dnaB, opponent.customName);

    logger.info("Battle complete — winner is A: " + battle.winnerIsA);

    await appendResult(api, context.projectId, context.playerId,  buildResult(battle, true,  opponent.customName, opponent.playerId, opponent.playerName ?? "Anonymous"));
    await appendResult(api, context.projectId, opponent.playerId, buildResult(battle, false, customName,          context.playerId,  playerName          ?? "Anonymous"));

    return JSON.stringify({ status: "matched" });
};

// ─────────────────────────────────────────────────────────────────
// I/O Helpers
// ─────────────────────────────────────────────────────────────────

async function appendResult(api, projectId, playerId, result) {
    let existing = [];
    try {
        const res  = await api.getItems(projectId, playerId, [RESULTS_KEY]);
        const item = res.data?.results?.find(i => i.key === RESULTS_KEY);
        if (item?.value) existing = JSON.parse(item.value);
    } catch (_) {}

    existing.push(result);
    await api.setItemBatch(projectId, playerId, {
        data: [{ key: RESULTS_KEY, value: JSON.stringify(existing) }],
    });
}

function buildResult(battle, callerIsA, opponentName, opponentPlayerId, opponentPlayerName) {
    const won  = callerIsA ? battle.winnerIsA : !battle.winnerIsA;
    const died = !won && battle.loserDied;
    return {
        CreatureId:         callerIsA ? battle.idA : battle.idB,
        Won:                won,
        Died:               died,
        EvolvedSlot:        won ? battle.evolvedSlot : null,
        Date:               new Date().toISOString(),   // when the fight actually ran (UTC)
        OpponentName:       opponentName,
        OpponentPlayerId:   opponentPlayerId,
        OpponentPlayerName: opponentPlayerName,
        SelfWasA:           callerIsA,        // which side this player's creature was (for the replay)
        Log:                battle.log,
        Turns:              battle.turns,     // structured turn-by-turn replay
    };
}

// ─────────────────────────────────────────────────────────────────
// Combat Simulation
// ─────────────────────────────────────────────────────────────────

const tierInt = t => {
    if (typeof t === "number") return t;
    if (t === "Tier3") return 3;
    if (t === "Tier2") return 2;
    return 1;
};

function computeStats(dna) {
    const b = t => tierInt(t) - 1;
    return {
        hp:  (dna.BaseConstitution || 5) * 5 + b(dna.BodyTier)  + b(dna.ArmTier),
        atk: (dna.BaseAttack       || 5) + b(dna.ArmTier)   + b(dna.MouthTier),
        spd: (dna.BaseSpeed        || 5) + b(dna.EyeTier)   + b(dna.MouthTier),
        def: dna.BaseDefense  || 0,
        lck: dna.BaseLuck     || 0,
        eva: dna.BaseEvasion  || 0,
    };
}

function simulateCombat(dnaA, nameA, dnaB, nameB) {
    const sA = computeStats(dnaA);
    const sB = computeStats(dnaB);
    let hpA = sA.hp, hpB = sB.hp;
    const log = [];
    const turns = [];   // structured replay (A = dnaA), mirrors C# CombatTurn

    log.push(`COMBAT — "${nameA}" HP:${hpA} ATK:${sA.atk} SPD:${sA.spd} vs "${nameB}" HP:${hpB} ATK:${sB.atk} SPD:${sB.spd}`);

    for (let round = 1; round <= 50; round++) {
        const aFirst = sA.spd !== sB.spd ? sA.spd > sB.spd : Math.random() < 0.5;

        if (aFirst) {
            hpB -= strike(sA.atk, nameA, nameB, true,  hpB, round, log, turns, sA.lck, sB.def, sB.eva);
            if (hpB <= 0) break;
            hpA -= strike(sB.atk, nameB, nameA, false, hpA, round, log, turns, sB.lck, sA.def, sA.eva);
        } else {
            hpA -= strike(sB.atk, nameB, nameA, false, hpA, round, log, turns, sB.lck, sA.def, sA.eva);
            if (hpA <= 0) break;
            hpB -= strike(sA.atk, nameA, nameB, true,  hpB, round, log, turns, sA.lck, sB.def, sB.eva);
        }

        if (hpA <= 0 || hpB <= 0) break;
    }

    const winnerIsA   = hpA >= hpB;
    const evolvedSlot = evolveRandom(winnerIsA ? dnaA : dnaB);
    const loserDied   = Math.random() < 0.15;

    log.push(`=== END === Winner: "${winnerIsA ? nameA : nameB}" | Evolved: ${evolvedSlot ?? "none"}`);

    return {
        idA: dnaA.UniqueID || "",
        idB: dnaB.UniqueID || "",
        winnerIsA,
        loserDied,
        evolvedSlot,
        log,
        turns,
    };
}

// Field names (PascalCase) MUST match C# CombatTurn so it deserializes with no remap.
function strike(atk, attackerName, defenderName, attackerIsA, defenderHp, round, log, turns, attackerLck, defenderDef, defenderEva) {
    if (Math.random() < defenderEva * EVASION_PER_POINT) {
        log.push(`${attackerName} → ${defenderName}: DODGE`);
        turns.push({
            TurnNumber:      round,
            AttackerName:    attackerName,
            DefenderName:    defenderName,
            AttackerIsA:     attackerIsA,
            Damage:          0,
            WasCrit:         false,
            DefenderHpAfter: defenderHp,
        });
        return 0;
    }
    const critChance = CRIT_CHANCE + attackerLck * LUCK_CRIT_PER_POINT;
    const crit       = Math.random() < critChance;
    const raw        = crit ? atk * CRIT_MULT : atk;
    const reduction  = Math.min(1, defenderDef * DEF_REDUCTION_PER_POINT);
    const dmg        = raw * (1 - reduction);
    const hpAfter    = Math.max(0, defenderHp - dmg);
    log.push(`${attackerName} → ${defenderName}: ${dmg}${crit ? " [CRIT]" : ""}`);
    turns.push({
        TurnNumber:      round,
        AttackerName:    attackerName,
        DefenderName:    defenderName,
        AttackerIsA:     attackerIsA,
        Damage:          dmg,
        WasCrit:         crit,
        DefenderHpAfter: hpAfter,
    });
    return dmg;
}

function evolveRandom(dna) {
    const slots = [
        { name: "Body",  t: tierInt(dna.BodyTier)  },
        { name: "Arm",   t: tierInt(dna.ArmTier)   },
        { name: "Eye",   t: tierInt(dna.EyeTier)   },
        { name: "Mouth", t: tierInt(dna.MouthTier) },
    ];

    const eligible = slots.filter(s => s.t < 3);
    if (eligible.length === 0) return null;

    // Weighted: Tier1 → weight 7, Tier2 → weight 2  (70/20 split)
    const weighted = [];
    for (const s of eligible) {
        const w = s.t === 1 ? 7 : 2;
        for (let i = 0; i < w; i++) weighted.push(s.name);
    }

    const chosen = weighted[Math.floor(Math.random() * weighted.length)];
    dna[chosen + "Tier"] = tierInt(dna[chosen + "Tier"]) + 1;
    return chosen;
}

module.exports.params = {
    creatureId:   { type: "String", required: true },
    customName:   { type: "String", required: true },
    creatureJson: { type: "String", required: true },
    playerName:   { type: "String", required: false },
};
