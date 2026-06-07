// Cloud Code Script: dequeue-combat
// Removes a creature from the matchmaking pool by creatureId + playerId.
// Safe to call even if the creature was already matched — returns "not_found" in that case.
// The client clears BusyState locally regardless of the response.

const { DataApi } = require("@unity-services/cloud-save-1.4");

// A creature can be sitting in either pool (scheduled or the instant debug pool),
// so we look in both and remove wherever it is.
const POOL_KEYS = ["matchmaking_pool", "instant_pool"];

module.exports = async ({ params, context, logger }) => {
    const { creatureId } = params;

    if (!creatureId)
        throw new Error("Missing required param: creatureId");

    const api = new DataApi({ accessToken: context.serviceToken });

    let removed   = 0;
    let totalLeft = 0;

    for (const poolKey of POOL_KEYS) {
        // ── Load pool ─────────────────────────────────────────────
        let pool = [];
        try {
            const res  = await api.getCustomItems(context.projectId, context.environmentId, [poolKey]);
            const item = res.data?.results?.find(i => i.key === poolKey);
            pool = Array.isArray(item?.value?.entries) ? item.value.entries : [];
        } catch (e) {
            logger.info(`Pool ${poolKey} not found: ` + (e.message || e));
            continue;
        }

        // ── Remove matching entry (only own creatures — enforced by playerId) ──
        const before = pool.length;
        pool = pool.filter(e => !(e.creatureId === creatureId && e.playerId === context.playerId));
        const poolRemoved = before - pool.length;
        removed   += poolRemoved;
        totalLeft += pool.length;

        // Only rewrite a pool we actually changed.
        if (poolRemoved > 0) {
            await api.setCustomItem(context.projectId, context.environmentId, {
                key:   poolKey,
                value: { entries: pool },
            });
            logger.info(`Dequeued "${creatureId}" from ${poolKey} for player ${context.playerId}. Pool size: ${pool.length}`);
        }
    }

    if (removed === 0) {
        logger.info(`Dequeue: "${creatureId}" not found in any pool for player ${context.playerId}`);
        return JSON.stringify({ status: "not_found", poolSize: totalLeft });
    }

    return JSON.stringify({ status: "dequeued", poolSize: totalLeft });
};

module.exports.params = {
    creatureId: { type: "String", required: true },
};
