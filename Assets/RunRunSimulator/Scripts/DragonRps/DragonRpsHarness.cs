using System;
using System.Collections.Generic;
using System.Text;

namespace MoriMonchiSimulator.DragonRps
{
    public static class DragonRpsHarness
    {
        public static string PlayVerbose(int seed, int powerPlayer, int powerFoe)
        {
            DragonRpsDragon dragonA = DragonRpsDragon.Standard("A", powerPlayer);
            DragonRpsDragon dragonB = DragonRpsDragon.Standard("B", powerFoe);
            List<string> log = new List<string>();
            DragonRpsResult result = DragonRpsMatch.Play(dragonA, dragonB, DragonRpsPolicy.Counting, DragonRpsPolicy.Counting, seed, log);

            StringBuilder text = new StringBuilder();
            text.AppendLine("A potencia " + powerPlayer + "   vs   B potencia " + powerFoe + "   (seed " + seed + ")");
            for (int i = 0; i < log.Count; i++)
            {
                text.AppendLine(log[i]);
            }
            text.AppendLine("Ganador: " + (result.Winner == 0 ? "empate" : (result.Winner == 1 ? "A" : "B")) + "   en " + result.Rounds + " rondas");
            return text.ToString();
        }

        public static string RunBalance(int matches, int powerA, int powerB)
        {
            DragonRpsDragon dragonA = DragonRpsDragon.Standard("A", powerA);
            DragonRpsDragon dragonB = DragonRpsDragon.Standard("B", powerB);
            int skilled = 0;
            int blind = 0;
            int draws = 0;
            int rounds = 0;

            for (int m = 0; m < matches; m++)
            {
                DragonRpsResult result = DragonRpsMatch.Play(dragonA, dragonB, DragonRpsPolicy.Counting, DragonRpsPolicy.Random, m * 104729 + 13, null);
                rounds += result.Rounds;
                if (result.Winner == 1) skilled++;
                else if (result.Winner == 2) blind++;
                else draws++;
            }

            StringBuilder text = new StringBuilder();
            text.AppendLine("potencia " + powerA + " vs " + powerB + "   sobre " + matches + " combates");
            text.AppendLine("  el que cuenta gana " + (100.0 * skilled / (skilled + blind)).ToString("0.0") + "% de las decididas");
            text.AppendLine("  empates " + (100.0 * draws / matches).ToString("0.0") + "%   duracion media " + ((double)rounds / matches).ToString("0.00") + " rondas");
            return text.ToString();
        }
    }
}
