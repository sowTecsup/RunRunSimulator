// Cloud Code Script: hatch-breeding
// Server-authoritative hatch check for a specific egg (identified by its pair).
// Compares the real server clock against the readyAt stamped at start-breeding.
// Only when enough time has elapsed does it authorize the hatch and remove that
// egg from the player's array. The client then mints the cría locally.

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

    const egg = eggs[idx];

    // ── Server-side time check (the anti-cheat gate) ──────────────
    const now = Date.now();
    if (now < egg.readyAt) {
        return JSON.stringify({ status: "not_ready", readyAt: egg.readyAt, remainingMs: egg.readyAt - now });
    }

    // ── Ready: remove this egg, persist the rest, authorize ───────
    eggs.splice(idx, 1);
    await api.setCustomItem(context.projectId, context.environmentId, {
        key,
        value: { entries: eggs },
    });

    logger.info(`Hatch authorized for player ${context.playerId}: ${egg.motherId} x ${egg.fatherId}. Eggs remaining: ${eggs.length}`);
    return JSON.stringify({ status: "ready", motherId: egg.motherId, fatherId: egg.fatherId });
};

module.exports.params = {
    motherId: { type: "String", required: true },
    fatherId: { type: "String", required: true },
};
