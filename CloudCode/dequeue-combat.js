// Cloud Code Script: dequeue-combat
// Removes a creature from the matchmaking pool by creatureId + playerId.
// Safe to call even if the creature was already matched — returns "not_found" in that case.
// The client clears BusyState locally regardless of the response.

const { DataApi } = require("@unity-services/cloud-save-1.4");

const POOL_KEY = "matchmaking_pool";

module.exports = async ({ params, context, logger }) => {
    const { creatureId } = params;

    if (!creatureId)
        throw new Error("Missing required param: creatureId");

    const api = new DataApi({ accessToken: context.serviceToken });

    // ── Load pool ─────────────────────────────────────────────────
    let pool = [];
    try {
        const res  = await api.getCustomItems(context.projectId, context.environmentId, [POOL_KEY]);
        const item = res.data?.results?.find(i => i.key === POOL_KEY);
        pool = Array.isArray(item?.value?.entries) ? item.value.entries : [];
    } catch (e) {
        logger.info("Pool not found: " + (e.message || e));
        return JSON.stringify({ status: "not_found", poolSize: 0 });
    }

    // ── Remove matching entry (only own creatures — enforced by playerId) ──
    const before = pool.length;
    pool = pool.filter(e => !(e.creatureId === creatureId && e.playerId === context.playerId));
    const removed = before - pool.length;

    if (removed === 0) {
        logger.info(`Dequeue: "${creatureId}" not found in pool for player ${context.playerId}`);
        return JSON.stringify({ status: "not_found", poolSize: pool.length });
    }

    // ── Persist updated pool ──────────────────────────────────────
    await api.setCustomItem(context.projectId, context.environmentId, {
        key:   POOL_KEY,
        value: { entries: pool },
    });

    logger.info(`Dequeued "${creatureId}" for player ${context.playerId}. Pool size: ${pool.length}`);
    return JSON.stringify({ status: "dequeued", poolSize: pool.length });
};

module.exports.params = {
    creatureId: { type: "String", required: true },
};
