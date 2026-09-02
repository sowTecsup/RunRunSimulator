using System;
using MoriMonchiSimulator.DragonRps;
namespace MoriMonchiSimulator
{

public static class DragonRpsService
{
    public static int Seed(CreatureDNA player, DateTime now) =>
        unchecked((int)(player.Timestamp ^ now.Ticks));

    public static DragonRpsSession Start(CreatureDNA player, CreatureDNA rival, int seed) =>
        new DragonRpsSession(DragonRpsGenes.ToDragon(player), DragonRpsGenes.ToDragon(rival), seed);

    public static CombatOutcome Resolve(
        DragonRpsSession   session,
        CreatureDNA        player,
        CreatureRegistrySO registry,
        PlayerInventorySO  inventory,
        CombatTuningSO     tuning,
        DateTime           now)
    {
        if (!session.Finished) return default;

        bool won = DragonRpsMatch.Winner(session.Player, session.Foe) == 1;

        var outcome = new CombatOutcome
        {
            Won        = won,
            HitsPlayer = session.Player.Hits,
            HitsRival  = session.Foe.Hits,
            Rounds     = session.Round,
        };

        if (won)
        {
            inventory.AddAdventureMaterial(tuning.MaterialPerWin);
            outcome.MaterialGained = tuning.MaterialPerWin;
            GameEvents.InventoryChanged(inventory);
        }
        else
        {
            player.CombatCooldownUntil = now.AddMinutes(tuning.CooldownMinutes).Ticks;
            outcome.CooldownUntilTicks = player.CombatCooldownUntil;
            GameEvents.RegistryChanged(registry);
        }

        return outcome;
    }
}
}
