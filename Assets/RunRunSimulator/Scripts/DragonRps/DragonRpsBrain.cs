using System;
using System.Collections.Generic;

namespace MoriMonchiSimulator.DragonRps
{
    public enum DragonRpsPolicy
    {
        Random = 0,
        Counting = 1
    }

    public static class DragonRpsBrain
    {
        public static DragonAction Choose(DragonRpsPolicy policy, DragonRpsSide me, DragonRpsSide foe, Random rng)
        {
            if (policy == DragonRpsPolicy.Random)
            {
                return me.Hand[rng.Next(me.Hand.Count)];
            }
            return ChooseCounting(me, foe, rng);
        }

        private static DragonAction ChooseCounting(DragonRpsSide me, DragonRpsSide foe, Random rng)
        {
            int[] foeRemaining = foe.RemainingByType();
            DragonAction best = me.Hand[0];
            int bestScore = int.MinValue;
            List<DragonAction> ties = new List<DragonAction>();

            for (int i = 0; i < me.Hand.Count; i++)
            {
                DragonAction candidate = me.Hand[i];
                int score = ScoreAgainst(candidate, me, foe, foeRemaining);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                    ties.Clear();
                    ties.Add(candidate);
                }
                else if (score == bestScore)
                {
                    ties.Add(candidate);
                }
            }

            return ties[rng.Next(ties.Count)];
        }

        private static int ScoreAgainst(DragonAction candidate, DragonRpsSide me, DragonRpsSide foe, int[] foeRemaining)
        {
            int score = 0;
            for (int type = 0; type < DragonRpsRules.ActionCount; type++)
            {
                int weight = foeRemaining[type];
                if (weight <= 0) continue;
                DragonAction foeAction = (DragonAction)type;

                if (DragonRpsRules.Beats(candidate, foeAction))
                {
                    score += weight;
                }
                else if (DragonRpsRules.Beats(foeAction, candidate))
                {
                    score -= weight;
                }
                else
                {
                    int mine = me.Dragon.Power[(int)candidate];
                    int theirs = foe.Dragon.Power[type];
                    if (mine > theirs) score += weight;
                    else if (mine < theirs) score -= weight;
                }
            }
            return score;
        }
    }
}
