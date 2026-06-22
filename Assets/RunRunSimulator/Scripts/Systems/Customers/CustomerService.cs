using UnityEngine;
using Sirenix.OdinInspector;

namespace MoriMonchiSimulator
{
    public class CustomerService : MonoBehaviour
    {
        public static CustomerService Instance { get; private set; }

        [Title("Data")]
        [Required, AssetsOnly] [SerializeField] private CustomerPricingSO pricing;
        [Required, AssetsOnly] [SerializeField] private CustomerArchetypeDatabaseSO archetypes;

        private ValuationHandler valuation = new ValuationHandler();
        private NegotiationFlow negotiation = new NegotiationFlow();

        public CustomerPricingSO Pricing => pricing;
        public CustomerArchetypeDatabaseSO Archetypes => archetypes;
        public ValuationHandler Valuation => valuation;
        public NegotiationFlow Negotiation => negotiation;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public int EstimateAverage(CreatureDNA dna) => valuation.Estimate(dna, null, pricing);
    }
}
