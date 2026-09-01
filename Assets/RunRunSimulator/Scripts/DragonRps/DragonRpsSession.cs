using System;
using System.Collections.Generic;
using System.Text;

namespace MoriMonchiSimulator.DragonRps
{
    public class DragonRpsSession
    {
        private readonly Random rng;
        public DragonRpsSide Player;
        public DragonRpsSide Foe;
        public int Round;

        public DragonRpsSession(DragonRpsDragon player, DragonRpsDragon foe, int seed)
        {
            rng = new Random(seed);
            Player = new DragonRpsSide(player, rng);
            Foe = new DragonRpsSide(foe, rng);
        }

        public bool Finished
        {
            get { return DragonRpsMatch.IsOver(Player, Foe); }
        }

        public string Board()
        {
            StringBuilder text = new StringBuilder();
            text.AppendLine("golpes  vos " + Player.Hits + "  -  " + Foe.Hits + " " + Foe.Dragon.Name);
            text.AppendLine("tu mano:");
            for (int i = 0; i < Player.Hand.Count; i++)
            {
                text.AppendLine("  [" + i + "] " + DragonRpsRules.Name(Player.Hand[i]) + "  potencia " + Player.Dragon.Power[(int)Player.Hand[i]]);
            }
            text.AppendLine("intacto en el rival: " + Intact(Foe));
            text.AppendLine("intacto en vos:      " + Intact(Player));
            return text.ToString();
        }

        public string Play(int handIndex)
        {
            if (Finished) return "el combate ya termino";
            if (handIndex < 0 || handIndex >= Player.Hand.Count) return "esa carta no esta en tu mano";

            DragonAction playerAction = Player.Hand[handIndex];
            DragonAction foeAction = DragonRpsBrain.Choose(DragonRpsPolicy.Counting, Foe, Player, rng);
            string outcome = DragonRpsMatch.ResolveRound(Player, Foe, playerAction, foeAction);
            Round++;

            StringBuilder text = new StringBuilder();
            text.AppendLine("ronda " + Round + ":  " + DragonRpsRules.Name(playerAction) + "  vs  " + DragonRpsRules.Name(foeAction) + "   ->   " + outcome);
            text.AppendLine("golpes  vos " + Player.Hits + "  -  " + Foe.Hits);

            if (Finished)
            {
                int winner = DragonRpsMatch.Winner(Player, Foe);
                text.AppendLine(winner == 1 ? "GANASTE" : (winner == 2 ? "PERDISTE" : "EMPATE"));
            }
            return text.ToString();
        }

        private static string Intact(DragonRpsSide side)
        {
            int[] remaining = side.RemainingByType();
            StringBuilder text = new StringBuilder();
            for (int type = 0; type < DragonRpsRules.ActionCount; type++)
            {
                if (type > 0) text.Append("  ");
                text.Append(DragonRpsRules.Name((DragonAction)type) + " x" + remaining[type]);
            }
            return text.ToString();
        }
    }
}
