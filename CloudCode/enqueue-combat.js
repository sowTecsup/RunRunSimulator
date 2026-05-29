// Cloud Code Script: enqueue-combat
// Adds a MoriMochi to the matchmaking pool. Returns immediately — actual
// matching + simulation happens later in `process-matchmaking` (scheduled).

const { DataApi } = require("@unity-services/cloud-save-1.4");

const POOL_KEY    = "matchmaking_pool";
const POOL_TTL_MS = 86400000;  // 24 h — stale entries get dropped at process time

module.exports = async ({ params, context, logger }) => {
    const { creatureId, customName, creatureJson, playerName } = params;

    if (!creatureId || !customName || !creatureJson)
        throw new Error("Missing required params: creatureId, customName, creatureJson");

    const api = new DataApi({ accessToken: context.serviceToken });

    // ── Load existing pool ────────────────────────────────────────
    let pool = [];
    try {
        const res  = await api.getCustomItems(context.projectId, context.environmentId, [POOL_KEY]);
        const item = res.data?.results?.find(i => i.key === POOL_KEY);
        pool = Array.isArray(item?.value?.entries) ? item.value.entries : [];
    } catch (e) {
        logger.info("Pool not found, starting empty: " + (e.message || e));
    }

    // ── Idempotency: skip if this creature is already queued ──────
    if (pool.some(e => e.creatureId === creatureId)) {
        return JSON.stringify({ status: "already_queued" });
    }

    // ── Append + persist ──────────────────────────────────────────
    pool.push({
        playerId:   context.playerId,
        playerName: playerName ?? "Anonymous",
        creatureId,
        customName,
        creatureJson,
        ts:         Date.now(),
    });

    await api.setCustomItem(context.projectId, context.environmentId, {
        key:   POOL_KEY,
        value: { entries: pool },
    });

    logger.info(`Enqueued "${customName}" (${creatureId}) for player ${context.playerId}. Pool size: ${pool.length}`);
    return JSON.stringify({ status: "queued", poolSize: pool.length });
};

module.exports.params = {
    creatureId:   { type: "String", required: true },
    customName:   { type: "String", required: true },
    creatureJson: { type: "String", required: true },
    playerName:   { type: "String", required: false },
};
