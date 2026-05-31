// Cloud Code Script: get-queue-status
// Returns which of the CALLER's creatures are actually sitting in the
// matchmaking pool right now. Read-only, single-key read (the whole pool is
// one Custom Data array), so cost is O(1) regardless of how many creatures —
// called on demand from the client to reconcile "ghost" QueuedForCombat states
// (local registry says queued, but the pool never had it / already dropped it).

const { DataApi } = require("@unity-services/cloud-save-1.4");

const POOL_KEY = "matchmaking_pool";

module.exports = async ({ context, logger }) => {
    const api = new DataApi({ accessToken: context.serviceToken });

    let pool = [];
    try {
        const res  = await api.getCustomItems(context.projectId, context.environmentId, [POOL_KEY]);
        const item = res.data?.results?.find(i => i.key === POOL_KEY);
        pool = Array.isArray(item?.value?.entries) ? item.value.entries : [];
    } catch (e) {
        logger.info("Pool not found: " + (e.message || e));
        return JSON.stringify({ inPool: [] });
    }

    // Only the caller's own creatures — never leak other players' queue.
    const inPool = pool
        .filter(e => e.playerId === context.playerId)
        .map(e => e.creatureId);

    return JSON.stringify({ inPool });
};
