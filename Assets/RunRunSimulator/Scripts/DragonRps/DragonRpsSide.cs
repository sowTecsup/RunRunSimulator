using System;
using System.Collections.Generic;

namespace MoriMonchiSimulator.DragonRps
{
    public class DragonRpsSide
    {
        public DragonRpsDragon Dragon;
        public List<DragonAction> Deck = new List<DragonAction>();
        public List<DragonAction> Hand = new List<DragonAction>();
        public List<DragonAction> Discard = new List<DragonAction>();
        public int Hits;

        public DragonRpsSide(DragonRpsDragon dragon, Random rng)
        {
            Dragon = dragon;
            Deck = dragon.BuildDeck();
            Shuffle(rng);
            for (int i = 0; i < DragonRpsRules.HandSize; i++)
            {
                Draw();
            }
        }

        public bool CanAct
        {
            get { return Hand.Count > 0; }
        }

        public void Play(DragonAction action)
        {
            Hand.Remove(action);
            Discard.Add(action);
        }

        public void Draw()
        {
            if (Deck.Count == 0) return;
            int last = Deck.Count - 1;
            Hand.Add(Deck[last]);
            Deck.RemoveAt(last);
        }

        public int[] RemainingByType()
        {
            int[] remaining = new int[DragonRpsRules.ActionCount];
            for (int type = 0; type < DragonRpsRules.ActionCount; type++)
            {
                remaining[type] = Dragon.Counts[type];
            }
            for (int i = 0; i < Discard.Count; i++)
            {
                remaining[(int)Discard[i]]--;
            }
            return remaining;
        }

        private void Shuffle(Random rng)
        {
            for (int i = Deck.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                DragonAction swap = Deck[i];
                Deck[i] = Deck[j];
                Deck[j] = swap;
            }
        }
    }
}
