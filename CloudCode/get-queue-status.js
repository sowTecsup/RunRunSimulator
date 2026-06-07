// Cloud Code Script: get-queue-status
// Returns which of the CALLER's creatures are actually sitting in the
// matchmaking pool right now. Read-only, single-key read (the whole pool is
// one Custom Data array), so cost is O(1) regardless of how many creatures —
// called on demand from the client to reconcile "ghost" QueuedForCombat states
// (local registry says queued, but the pool never had it / already dropped it).

const { DataApi } = require("@unity-services/cloud-save-1.4");

// Both pools must be consulted: a creature waiting in the instant (debug) pool is
// still legitimately QueuedForCombat locally, so omitting it here would make the
// client's ghost-reconciliation wrongly clear it.
//
// IMPORTANT: read each pool with its OWN single-key getCustomItems call. A single
// multi-key call proved unreliable (returned incomplete results), which dropped
// timer-queued creatures from inPool and made the client wrongly dequeue them.
const POOL_KEYS = ["matchmaking_pool", "instant_pool"];

module.exports = async ({ context, logger }) => {
    const api = new DataApi({ accessToken: context.serviceToken });

    const entries = [];
    let   ok = true;   // false if ANY pool read threw → client must NOT reconcile on a partial view

    for (const key of POOL_KEYS) {
        try {
            const res  = await api.getCustomItems(context.projectId, context.environmentId, [key]);
            const item = res.data?.results?.find(i => i.key === key);
            if (Array.isArray(item?.value?.entries)) entries.push(...item.value.entries);
        } catch (e) {
            ok = false;
            logger.info(`Pool ${key} read failed: ` + (e.message || e));
        }
    }

    // Only the caller's own creatures — never leak other players' queue.
    const inPool = entries
        .filter(e => e.playerId === context.playerId)
        .map(e => e.creatureId);

    return JSON.stringify({ inPool, ok });
};
