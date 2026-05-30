// Cloud Code Script: start-breeding
// Stamps a server-authoritative breeding timer in Custom Data (Game Data).
// Eggs are stored as an ARRAY per player — multiple pairs can incubate in
// parallel. A given parent can only be in one egg at a time (a busy parent
// can't start another breed). Only writable via Cloud Code (service token) —
// the client cannot forge the start time.

const { DataApi } = require("@unity-services/cloud-save-1.4");

// Duration lives ONLY here (server-side) for anti-cheat. The client receives
// readyAt and uses it just for display — it never decides when breeding ends.
const BREED_DURATION_MS = 30 * 60 * 1000;  // 30 minutes

const eggsKey = playerId => `breeding_eggs_${playerId}`;

module.exports = async ({ params, context, logger }) => {
    const { motherId, fatherId } = params;

    if (!motherId || !fatherId)
        throw new Error("Missing required params: motherId, fatherId");
    if (motherId === fatherId)
        throw new Error("A MoriMochi cannot breed with itself.");

    const api = new DataApi({ accessToken: context.serviceToken });
    const key = eggsKey(context.playerId);

    // ── Load existing eggs ────────────────────────────────────────
    let eggs = [];
    try {
        const res  = await api.getCustomItems(context.projectId, context.environmentId, [key]);
        const item = res.data?.results?.find(i => i.key === key);
        eggs = Array.isArray(item?.value?.entries) ? item.value.entries : [];
    } catch (e) {
        logger.info("No existing eggs, starting empty: " + (e.message || e));
    }

    // ── Reject if either parent is already in an egg ──────────────
    const busy = eggs.some(e =>
        e.motherId === motherId || e.fatherId === motherId ||
        e.motherId === fatherId || e.fatherId === fatherId);
    if (busy) {
        return JSON.stringify({ status: "already_breeding" });
    }

    // ── Stamp server time + append ────────────────────────────────
    const startedAt = Date.now();
    const readyAt   = startedAt + BREED_DURATION_MS;
    eggs.push({ motherId, fatherId, startedAt, readyAt });

    await api.setCustomItem(context.projectId, context.environmentId, {
        key,
        value: { entries: eggs },
    });

    logger.info(`Breeding started for player ${context.playerId}: ${motherId} x ${fatherId}. Eggs now: ${eggs.length}. Ready at ${new Date(readyAt).toISOString()}`);
    return JSON.stringify({ status: "breeding", startedAt, readyAt });
};

module.exports.params = {
    motherId: { type: "String", required: true },
    fatherId: { type: "String", required: true },
};
