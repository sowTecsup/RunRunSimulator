// Cloud Code Script: get-server-time
// Returns the authoritative server UTC time as an ISO-8601 string.
// Called once at login by CloudSyncService to compute a local-clock offset so
// time-sensitive checks (shop restock, discount windows) use server time instead
// of the potentially-tampered device clock.

module.exports = async ({ params, context, logger }) => {
    const utc = new Date().toISOString();
    logger.info(`get-server-time | playerId: ${context.playerId} | utc: ${utc}`);
    return JSON.stringify({ utc });
};
