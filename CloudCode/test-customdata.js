// Cloud Code Script: test-customdata
// Diagnostic — isolates the Custom Data write/read issue.
// Returns a verbose log so we can see EXACTLY what fails (404, 401, method,
// payload error, etc.) without the matchmaking logic clouding the picture.

const { DataApi } = require("@unity-services/cloud-save-1.4");

const TEST_KEY = "diagnostic_ping";

module.exports = async ({ params, context, logger }) => {
    const log = [];
    const api = new DataApi({ accessToken: context.serviceToken });

    log.push(`projectId=${context.projectId}`);
    log.push(`environmentId=${context.environmentId}`);
    log.push(`playerId=${context.playerId}`);

    // ── 1) READ: getCustomItems ───────────────────────────────────
    try {
        const res = await api.getCustomItems(context.projectId, context.environmentId, [TEST_KEY]);
        const item = res.data?.results?.find(i => i.key === TEST_KEY);
        log.push(`READ OK — existing value: ${JSON.stringify(item?.value ?? null)}`);
    } catch (e) {
        log.push(`READ FAIL — ${e.response?.status ?? "?"} ${e.message ?? e}`);
    }

    // ── 2) WRITE: setCustomItem ───────────────────────────────────
    const payload = { ts: Date.now(), caller: context.playerId };
    try {
        const res = await api.setCustomItem(context.projectId, context.environmentId, {
            key:   TEST_KEY,
            value: payload,
        });
        log.push(`WRITE OK — status: ${res.status}, writeLock: ${res.data?.writeLock ?? "?"}`);
    } catch (e) {
        log.push(`WRITE FAIL — ${e.response?.status ?? "?"} ${e.response?.statusText ?? ""} ${e.message ?? e}`);
        if (e.response?.data) log.push(`  body: ${JSON.stringify(e.response.data)}`);
    }

    // ── 3) READ AGAIN to confirm write ────────────────────────────
    try {
        const res = await api.getCustomItems(context.projectId, context.environmentId, [TEST_KEY]);
        const item = res.data?.results?.find(i => i.key === TEST_KEY);
        log.push(`READ-AFTER OK — value: ${JSON.stringify(item?.value ?? null)}`);
    } catch (e) {
        log.push(`READ-AFTER FAIL — ${e.response?.status ?? "?"} ${e.message ?? e}`);
    }

    log.forEach(l => logger.info(l));
    return JSON.stringify({ log });
};
