// Cloud Code Script: test-random
// Sanity check — returns a random number 1-4. If this works, the Cloud Code
// endpoint plumbing is healthy and any other errors are app-logic specific.

module.exports = async ({ params, context, logger }) => {
    const n = Math.floor(Math.random() * 4) + 1;
    logger.info(`test-random rolled: ${n} | playerId: ${context.playerId}`);
    return JSON.stringify({ number: n, playerId: context.playerId });
};
