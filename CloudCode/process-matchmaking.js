// Cloud Code Script: process-matchmaking
// Scheduled trigger — runs every N minutes via Cloud Code Triggers.
// Drains the matchmaking pool, pairs creatures, simulates combats, writes
// results to each player's combat_results. Leftover (odd one out, stale) stays
// in the pool for the next tick.

const { DataApi } = require("@unity-services/cloud-save-1.4");

const POOL_KEY    = "matchmaking_pool";
const RESULTS_KEY = "combat_results";
const POOL_TTL_MS = 86400000;  // 24 h

module.exports = async ({ context, logger }) => {
    const api = new DataApi({ accessToken: context.serviceToken });

    // ── Load pool ─────────────────────────────────────────────────
    let pool = [];
    try {
        const res  = await api.getCustomItems(context.projectId, context.environmentId, [POOL_KEY]);
        const item = res.data?.results?.find(i => i.key === POOL_KEY);
        pool = Array.isArray(item?.value?.entries) ? item.value.entries : [];
    } catch (e) {
        logger.info("Pool empty / not found: " + (e.message || e));
        return JSON.stringify({ matched: 0, remaining: 0, dropped: 0 });
    }

    const now    = Date.now();
    const before = pool.length;
    pool         = pool.filter(e => now - e.ts < POOL_TTL_MS);
    const dropped = before - pool.length;

    if (pool.length < 2) {
        await persistPool(api, context, pool);
        logger.info(`Pool too small to match (${pool.length}). Dropped ${dropped} stale.`);
        return JSON.stringify({ matched: 0, remaining: pool.length, dropped });
    }

    // ── Shuffle + pair ────────────────────────────────────────────
    shuffle(pool);
    const pairs    = [];
    const leftover = [];

    while (pool.length >= 2) {
        const a = pool.shift();
        // Find first entry whose playerId differs from a's. If none, a waits.
        const partnerIdx = pool.findIndex(e => e.playerId !== a.playerId);
        if (partnerIdx === -1) {
            leftover.push(a);
            continue;
        }
        const [b] = pool.splice(partnerIdx, 1);
        pairs.push([a, b]);
    }
    leftover.push(...pool);

    // ── Simulate each pair, write results to both players ─────────
    let matched = 0;
    for (const [a, b] of pairs) {
        try {
            const dnaA   = JSON.parse(a.creatureJson);
            const dnaB   = JSON.parse(b.creatureJson);
            const battle = simulateCombat(dnaA, a.customName, dnaB, b.customName);

            await appendResult(api, context.projectId, a.playerId, buildResult(battle, true,  b));
            await appendResult(api, context.projectId, b.playerId, buildResult(battle, false, a));

            matched++;
            logger.info(`Matched "${a.customName}" (${a.playerName}) vs "${b.customName}" (${b.playerName}) — winner A: ${battle.winnerIsA}`);
        } catch (e) {
            logger.error(`Pair failed: ${e.message || e}`);
            leftover.push(a, b);
        }
    }

    await persistPool(api, context, leftover);

    logger.info(`Tick complete — matched: ${matched}, remaining: ${leftover.length}, dropped: ${dropped}`);
    return JSON.stringify({ matched, remaining: leftover.length, dropped });
};

// ─────────────────────────────────────────────────────────────────
// I/O Helpers
// ─────────────────────────────────────────────────────────────────

async function persistPool(api, context, pool) {
    await api.setCustomItem(context.projectId, context.environmentId, {
        key:   POOL_KEY,
        value: { entries: pool },
    });
}

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

function buildResult(battle, callerIsA, opponent) {
    const won  = callerIsA ? battle.winnerIsA : !battle.winnerIsA;
    const died = !won && battle.loserDied;
    return {
        CreatureId:         callerIsA ? battle.idA : battle.idB,
        Won:                won,
        Died:               died,
        EvolvedSlot:        won ? battle.evolvedSlot : null,
        OpponentName:       opponent.customName,
        OpponentPlayerId:   opponent.playerId,
        OpponentPlayerName: opponent.playerName ?? "Anonymous",
        Log:                battle.log,
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
        hp:  (dna.BaseHP     || 5) + b(dna.BodyTier)  + b(dna.ArmTier),
        atk: (dna.BaseAttack || 5) + b(dna.ArmTier)   + b(dna.MouthTier),
        spd: (dna.BaseSpeed  || 5) + b(dna.EyeTier)   + b(dna.MouthTier),
    };
}

function simulateCombat(dnaA, nameA, dnaB, nameB) {
    const sA = computeStats(dnaA);
    const sB = computeStats(dnaB);
    let hpA = sA.hp, hpB = sB.hp;
    const log = [];

    log.push(`COMBAT — "${nameA}" HP:${hpA} ATK:${sA.atk} SPD:${sA.spd} vs "${nameB}" HP:${hpB} ATK:${sB.atk} SPD:${sB.spd}`);

    for (let round = 1; round <= 50; round++) {
        const aFirst = sA.spd !== sB.spd ? sA.spd > sB.spd : Math.random() < 0.5;

        if (aFirst) {
            hpB -= strike(sA.atk, nameA, nameB, log);
            if (hpB <= 0) break;
            hpA -= strike(sB.atk, nameB, nameA, log);
        } else {
            hpA -= strike(sB.atk, nameB, nameA, log);
            if (hpA <= 0) break;
            hpB -= strike(sA.atk, nameA, nameB, log);
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
    };
}

function strike(atk, attacker, defender, log) {
    const crit = Math.random() < 0.2;
    const dmg  = crit ? atk * 3 : atk;
    log.push(`${attacker} → ${defender}: ${dmg}${crit ? " [CRIT]" : ""}`);
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

function shuffle(arr) {
    for (let i = arr.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [arr[i], arr[j]] = [arr[j], arr[i]];
    }
}
