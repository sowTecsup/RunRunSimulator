using System;
using System.Collections.Generic;

namespace MoriMonchiSimulator.DragonRps
{
    public class DragonRpsResult
    {
        public int HitsA;
        public int HitsB;
        public int Rounds;
        public int Winner;
    }

    public static class DragonRpsMatch
    {
        public static DragonRpsResult Play(DragonRpsDragon dragonA, DragonRpsDragon dragonB, DragonRpsPolicy policyA, DragonRpsPolicy policyB, int seed, List<string> log)
        {
            Random rng = new Random(seed);
            DragonRpsSide sideA = new DragonRpsSide(dragonA, rng);
            DragonRpsSide sideB = new DragonRpsSide(dragonB, rng);
            DragonRpsResult result = new DragonRpsResult();

            while (!IsOver(sideA, sideB))
            {
                DragonAction actionA = DragonRpsBrain.Choose(policyA, sideA, sideB, rng);
                DragonAction actionB = DragonRpsBrain.Choose(policyB, sideB, sideA, rng);
                string outcome = ResolveRound(sideA, sideB, actionA, actionB);
                result.Rounds++;

                if (log != null)
                {
                    log.Add("R" + result.Rounds + "  " + DragonRpsRules.Name(actionA) + " vs " + DragonRpsRules.Name(actionB) + "  ->  " + outcome + "   [" + sideA.Hits + " - " + sideB.Hits + "]");
                }
            }

            result.HitsA = sideA.Hits;
            result.HitsB = sideB.Hits;
            result.Winner = Winner(sideA, sideB);
            return result;
        }

        public static bool IsOver(DragonRpsSide sideA, DragonRpsSide sideB)
        {
            return !sideA.CanAct || !sideB.CanAct || sideA.Hits >= DragonRpsRules.HitsToWin || sideB.Hits >= DragonRpsRules.HitsToWin;
        }

        public static int Winner(DragonRpsSide sideA, DragonRpsSide sideB)
        {
            if (sideA.Hits > sideB.Hits) return 1;
            if (sideB.Hits > sideA.Hits) return 2;
            return 0;
        }

        public static string ResolveRound(DragonRpsSide sideA, DragonRpsSide sideB, DragonAction actionA, DragonAction actionB)
        {
            sideA.Play(actionA);
            sideB.Play(actionB);

            string outcome = Score(sideA, sideB, actionA, actionB);

            sideA.Draw();
            sideB.Draw();
            return outcome;
        }

        private static string Score(DragonRpsSide sideA, DragonRpsSide sideB, DragonAction actionA, DragonAction actionB)
        {
            if (DragonRpsRules.Beats(actionA, actionB))
            {
                sideA.Hits++;
                return DragonRpsRules.Name(actionA) + " rompe " + DragonRpsRules.Name(actionB);
            }
            if (DragonRpsRules.Beats(actionB, actionA))
            {
                sideB.Hits++;
                return DragonRpsRules.Name(actionB) + " rompe " + DragonRpsRules.Name(actionA);
            }

            int powerA = sideA.Dragon.Power[(int)actionA];
            int powerB = sideB.Dragon.Power[(int)actionB];
            if (powerA > powerB)
            {
                sideA.Hits++;
                return "espejo, se impone A por potencia " + powerA + " a " + powerB;
            }
            if (powerB > powerA)
            {
                sideB.Hits++;
                return "espejo, se impone B por potencia " + powerB + " a " + powerA;
            }

            sideA.Hits++;
            sideB.Hits++;
            return "espejo parejo, se lastiman los dos";
        }
    }
}
