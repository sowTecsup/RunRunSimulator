using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MoriMonchiSimulator
{
    public class NpcController : MonoBehaviour
    {
        public static NpcController Instance { get; private set; }

        [Title("Spawn")]
        [Required] [SerializeField] private Transform spawnPoint;
        [Required] [SerializeField] private Transform exitPoint;

        [Title("Targets")]
        [SerializeField] private List<StoreContainer> displays = new();
        [Required] [SerializeField] private CashRegister register;

        [Title("Cadence")]
        [MinValue(0.1f)] [SerializeField] private float minSpawnInterval = 20f;
        [MinValue(0.1f)] [SerializeField] private float maxSpawnInterval = 60f;
        [MinValue(1)]    [SerializeField] private int maxSimultaneous = 3;

        [Title("Default Prefab (fallback if archetype.AgentPrefab is null)")]
        [AssetsOnly] [SerializeField] private GameObject defaultAgentPrefab;

        private List<NpcAgent> active = new();
        private float nextSpawnAt;

        public IReadOnlyList<NpcAgent> Active => active;
        public Transform ExitPoint => exitPoint;

        private void Awake()
        {
            Instance = this;
            nextSpawnAt = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Time.time < nextSpawnAt) return;
            if (active.Count >= maxSimultaneous)
            {
                nextSpawnAt = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
                return;
            }
            TrySpawnOne();
            nextSpawnAt = Time.time + Random.Range(minSpawnInterval, maxSpawnInterval);
        }

        private void TrySpawnOne()
        {
            if (CustomerService.Instance == null)
            {
                Debug.LogWarning("[NpcController] CustomerService.Instance is null.");
                return;
            }

            var archetype = CustomerService.Instance.Archetypes?.RandomArchetype();
            if (archetype == null)
            {
                Debug.LogWarning("[NpcController] No archetype available.");
                return;
            }

            var prefab = archetype.AgentPrefab != null ? archetype.AgentPrefab : defaultAgentPrefab;
            if (prefab == null)
            {
                Debug.LogWarning("[NpcController] No prefab available for archetype or default.");
                return;
            }

            var go = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            var agent = go.GetComponent<NpcAgent>();
            if (agent == null)
            {
                Debug.LogWarning("[NpcController] Prefab has no NpcAgent component.");
                Destroy(go);
                return;
            }

            agent.Initialize(archetype, displays, register, this);
            active.Add(agent);
            GameEvents.CustomerSpawned(agent);
        }

        public void Despawn(NpcAgent agent)
        {
            active.Remove(agent);
            if (agent != null) Destroy(agent.gameObject);
        }
    }
}
