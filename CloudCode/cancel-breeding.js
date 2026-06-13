// Cloud Code Script: cancel-breeding
// Removes a specific pending egg (identified by motherId + fatherId) from the
// player's breeding queue. No time-gate — the caller just wants the egg gone.

const { DataApi } = require("@unity-services/cloud-save-1.4");

const eggsKey = playerId => `breeding_eggs_${playerId}`;

module.exports = async ({ params, context, logger }) => {
    const { motherId, fatherId } = params;

    if (!motherId || !fatherId)
        throw new Error("Missing required params: motherId, fatherId");

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

    // ── Find the specific egg ─────────────────────────────────────
    const idx = eggs.findIndex(e => e.motherId === motherId && e.fatherId === fatherId);
    if (idx === -1) {
        return JSON.stringify({ status: "no_egg" });
    }

    // ── Remove the egg and persist the rest ───────────────────────
    eggs.splice(idx, 1);
    await api.setCustomItem(context.projectId, context.environmentId, {
        key,
        value: { entries: eggs },
    });

    logger.info(`Breeding cancelled for player ${context.playerId}: ${motherId} x ${fatherId}. Eggs remaining: ${eggs.length}`);
    return JSON.stringify({ status: "cancelled", motherId, fatherId });
};

module.exports.params = {
    motherId: { type: "String", required: true },
    fatherId: { type: "String", required: true },
};
