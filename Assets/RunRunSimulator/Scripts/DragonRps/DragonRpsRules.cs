namespace MoriMonchiSimulator.DragonRps
{
    public enum DragonAction
    {
        Horns = 0,
        Wings = 1,
        Back = 2
    }

    public static class DragonRpsRules
    {
        public const int ActionCount = 3;
        public const int DeckSize = 6;
        public const int HandSize = 3;
        public const int HitsToWin = 3;

        public static bool Beats(DragonAction attacker, DragonAction defender)
        {
            return (attacker == DragonAction.Horns && defender == DragonAction.Wings)
                || (attacker == DragonAction.Wings && defender == DragonAction.Back)
                || (attacker == DragonAction.Back && defender == DragonAction.Horns);
        }

        public static string Name(DragonAction action)
        {
            if (action == DragonAction.Horns) return "Cuernos";
            if (action == DragonAction.Wings) return "Alas";
            return "Espalda";
        }
    }
}
