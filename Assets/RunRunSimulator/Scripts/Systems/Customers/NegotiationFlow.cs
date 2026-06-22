using System;
using UnityEngine;

namespace MoriMonchiSimulator
{
    public enum NegotiationResult { Accept, Reject }

    [Serializable]
    public class NegotiationFlow
    {
        public int ComputeCounter(int currentOffer, CustomerPricingSO pricing)
        {
            if (pricing == null) return currentOffer;
            return Mathf.RoundToInt(currentOffer * (1f + pricing.RenegotiationStep));
        }

        public NegotiationResult EvaluateCounter(CustomerArchetypeSO archetype)
        {
            if (archetype == null) return NegotiationResult.Accept;
            return UnityEngine.Random.value <= archetype.RenegotiationTolerance
                ? NegotiationResult.Accept
                : NegotiationResult.Reject;
        }
    }
}
