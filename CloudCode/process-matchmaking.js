// Cloud Code Script: process-matchmaking
// Scheduled trigger — runs every N minutes via Cloud Code Triggers.
// Drains the matchmaking pool, pairs creatures, and hands each player a seed +
// both DNA snapshots; clients simulate the fight locally. Leftover (odd one
// out, stale) stays in the pool for the next tick.

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

    // ── Hand each pair a shared seed + both snapshots ───────────────
    let matched = 0;
    for (const [a, b] of pairs) {
        try {
            const seed = Math.floor(Math.random() * 2147483647);
            const date = new Date().toISOString();

            await appendResult(api, context.projectId, a.playerId, {
                CreatureId:         a.creatureId,
                Seed:               seed,
                SelfWasA:           true,
                CreatureJsonA:      a.creatureJson,
                CreatureJsonB:      b.creatureJson,
                OpponentName:       b.customName,
                OpponentPlayerId:   b.playerId,
                OpponentPlayerName: b.playerName ?? "Anonymous",
                Date:               date,
            });
            await appendResult(api, context.projectId, b.playerId, {
                CreatureId:         b.creatureId,
                Seed:               seed,
                SelfWasA:           false,
                CreatureJsonA:      a.creatureJson,
                CreatureJsonB:      b.creatureJson,
                OpponentName:       a.customName,
                OpponentPlayerId:   a.playerId,
                OpponentPlayerName: a.playerName ?? "Anonymous",
                Date:               date,
            });

            matched++;
            logger.info(`Matched "${a.customName}" (${a.playerName}) vs "${b.customName}" (${b.playerName}) — seed ${seed}`);
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

function shuffle(arr) {
    for (let i = arr.length - 1; i > 0; i--) {
        const j = Math.floor(Math.random() * (i + 1));
        [arr[i], arr[j]] = [arr[j], arr[i]];
    }
}
