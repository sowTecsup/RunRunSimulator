using System.Collections.Generic;

namespace MoriMonchiSimulator.DragonRps
{
    public class DragonRpsDragon
    {
        public string Name;
        public int[] Counts = new int[DragonRpsRules.ActionCount];
        public int[] Power = new int[DragonRpsRules.ActionCount];

        public static DragonRpsDragon Standard(string name, int power)
        {
            DragonRpsDragon dragon = FromSpread(2, 2, 2);
            dragon.Name = name;
            for (int type = 0; type < DragonRpsRules.ActionCount; type++)
            {
                dragon.Power[type] = power;
            }
            return dragon;
        }

        public static DragonRpsDragon FromSpread(int horns, int wings, int back)
        {
            DragonRpsDragon dragon = new DragonRpsDragon();
            dragon.Counts[0] = horns;
            dragon.Counts[1] = wings;
            dragon.Counts[2] = back;
            dragon.Power[0] = 1;
            dragon.Power[1] = 1;
            dragon.Power[2] = 1;
            dragon.Name = horns + "-" + wings + "-" + back;
            return dragon;
        }

        public List<DragonAction> BuildDeck()
        {
            List<DragonAction> deck = new List<DragonAction>();
            for (int type = 0; type < DragonRpsRules.ActionCount; type++)
            {
                for (int i = 0; i < Counts[type]; i++)
                {
                    deck.Add((DragonAction)type);
                }
            }
            return deck;
        }

        public static List<DragonRpsDragon> AllSpreads()
        {
            List<DragonRpsDragon> spreads = new List<DragonRpsDragon>();
            for (int horns = 1; horns <= DragonRpsRules.DeckSize - 2; horns++)
            {
                for (int wings = 1; wings <= DragonRpsRules.DeckSize - horns - 1; wings++)
                {
                    int back = DragonRpsRules.DeckSize - horns - wings;
                    if (back < 1) continue;
                    spreads.Add(FromSpread(horns, wings, back));
                }
            }
            return spreads;
        }
    }
}
