// Cloud Code Script: run-combat
// Matchmaking only — combat now runs client-side from a shared seed.

const { DataApi } = require("@unity-services/cloud-save-1.4");

// Instant mode is a DEBUG path (skip the hourly wait): only ever one creature A and
// one creature B meet here, so it gets its OWN pool, fully isolated from the
// scheduled matchmaking_pool. No cross-contamination with timer-queued creatures.
const POOL_KEY    = "instant_pool";
const RESULTS_KEY = "combat_results";
const POOL_TTL_MS = 86400000;  // 24 h

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

    // ── Match found — the server no longer simulates. It hands both players the
    // same seed + both DNA snapshots; each client runs the same deterministic
    // C# sim (CombatService) and derives the identical record locally.
    const seed = Math.floor(Math.random() * 2147483647);
    const date = new Date().toISOString();

    logger.info(`Matched "${customName}" vs "${opponent.customName}" — seed ${seed}`);

    await appendResult(api, context.projectId, context.playerId, {
        CreatureId:         creatureId,
        Seed:               seed,
        SelfWasA:           true,
        CreatureJsonA:      creatureJson,
        CreatureJsonB:      opponent.creatureJson,
        OpponentName:       opponent.customName,
        OpponentPlayerId:   opponent.playerId,
        OpponentPlayerName: opponent.playerName ?? "Anonymous",
        Date:               date,
    });
    await appendResult(api, context.projectId, opponent.playerId, {
        CreatureId:         opponent.creatureId,
        Seed:               seed,
        SelfWasA:           false,
        CreatureJsonA:      creatureJson,
        CreatureJsonB:      opponent.creatureJson,
        OpponentName:       customName,
        OpponentPlayerId:   context.playerId,
        OpponentPlayerName: playerName ?? "Anonymous",
        Date:               date,
    });

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

module.exports.params = {
    creatureId:   { type: "String", required: true },
    customName:   { type: "String", required: true },
    creatureJson: { type: "String", required: true },
    playerName:   { type: "String", required: false },
};
