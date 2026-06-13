// Cloud Code Script: cancel-all-breeding
// Wipes the player's ENTIRE breeding queue — every pending egg, gone. Debug /
// recovery tool: clears orphaned eggs the client no longer tracks locally (e.g.
// a cancel whose endpoint failed left the server egg behind). No time-gate.

const { DataApi } = require("@unity-services/cloud-save-1.4");

const eggsKey = playerId => `breeding_eggs_${playerId}`;

module.exports = async ({ params, context, logger }) => {
    const api = new DataApi({ accessToken: context.serviceToken });
    const key = eggsKey(context.playerId);

    // ── Load eggs ─────────────────────────────────────────────────
    let eggs = [];
    try {
        const res  = await api.getCustomItems(context.projectId, context.environmentId, [key]);
        const item = res.data?.results?.find(i => i.key === key);
        eggs = Array.isArray(item?.value?.entries) ? item.value.entries : [];
    } catch (e) {
        logger.info("No eggs found: " + (e.message || e));
    }

    const cleared = eggs.length;
    if (cleared === 0) {
        return JSON.stringify({ status: "no_eggs", cleared: 0, eggs: [] });
    }

    // ── Wipe the whole queue ──────────────────────────────────────
    await api.setCustomItem(context.projectId, context.environmentId, {
        key,
        value: { entries: [] },
    });

    logger.info(`All breeding eggs cancelled for player ${context.playerId}. Cleared: ${cleared}`);
    return JSON.stringify({ status: "cancelled", cleared, eggs });
};

module.exports.params = {};
