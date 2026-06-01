using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

// Bridges DATA → PRESENCE: turns registered creatures into live cubes in the
// scene. Event-driven and deliberately simple — it listens to the registry and,
// whenever a new living creature appears, spawns one prefab for it (and despawns
// the ones that died or were removed). For now EVERY living creature spawns;
// later, state-based zones can decide where they go.
//
// Reads the shared assets from GameManager.Instance (registry, personality table).
// Each spawned cube gets its CreatureDNA + personality wired via MoriMochiAgent.
public class MoriMochiSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Cube prefab with MoriMochiAgent + NavMeshAgent + Rigidbody + Collider and a NameTag child.")]
    [Required, SerializeField] private MoriMochiAgent creaturePrefab;

    [Header("Spawn placement")]
    [Tooltip("Where creatures appear. If null, this object's position is used.")]
    [SerializeField] private Transform spawnArea;
    [Tooltip("Creatures are dropped within this radius of the spawn area, snapped to the NavMesh.")]
    [SerializeField] private float spawnRadius = 4f;

    // ── State ─────────────────────────────────────────────────────
    private readonly Dictionary<string, MoriMochiAgent> spawned = new Dictionary<string, MoriMochiAgent>();
    private Transform player;

    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private int SpawnedCount => spawned.Count;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void OnEnable()
    {
        GameEvents.OnRegistryChanged  += OnRegistryChanged;
        GameEvents.OnRegistryReloaded += OnRegistryReloaded;
    }

    private void OnDisable()
    {
        GameEvents.OnRegistryChanged  -= OnRegistryChanged;
        GameEvents.OnRegistryReloaded -= OnRegistryReloaded;
    }

    private void Start()
    {
        player = ResolvePlayer();
        var registry = GameManager.Instance != null ? GameManager.Instance.Registry : null;
        if (registry != null) Sync(registry);
    }

    // Incremental: same dna instances are mutated in place, so live agents stay valid.
    private void OnRegistryChanged(CreatureRegistrySO registry) => Sync(registry);

    // Wholesale replace (cloud pull/reset) swaps the dna objects — rebuild from scratch.
    private void OnRegistryReloaded(CreatureRegistrySO registry)
    {
        ClearAll();
        Sync(registry);
    }

    // ── Sync ──────────────────────────────────────────────────────

    private void Sync(CreatureRegistrySO registry)
    {
        var all = registry.GetAll();

        // Spawn newly-registered living creatures.
        foreach (var kv in all)
        {
            var dna = kv.Value;
            if (dna.IsDead) continue;
            if (spawned.ContainsKey(kv.Key)) continue;
            SpawnOne(dna);
        }

        // Despawn the ones that died or were removed from the registry.
        var stale = spawned.Keys
            .Where(id => !all.TryGetValue(id, out var d) || d.IsDead)
            .ToList();
        foreach (var id in stale) Despawn(id);
    }

    private void SpawnOne(CreatureDNA dna)
    {
        if (creaturePrefab == null)
        {
            Debug.LogError("[MoriMochiSpawner] No creature prefab assigned.");
            return;
        }

        var table = GameManager.Instance != null ? GameManager.Instance.PersonalityProfiles : null;
        Vector3 pos = ResolveSpawnPosition(table, dna.Personality);

        var agent = Instantiate(creaturePrefab, pos, Quaternion.identity);
        agent.name = $"MoriMochi_{dna.CustomName}";
        agent.Initialize(dna, table, player);

        spawned[dna.UniqueID] = agent;
    }

    private void Despawn(string id)
    {
        if (spawned.TryGetValue(id, out var agent) && agent != null)
            Destroy(agent.gameObject);
        spawned.Remove(id);
    }

    private void ClearAll()
    {
        foreach (var agent in spawned.Values)
            if (agent != null) Destroy(agent.gameObject);
        spawned.Clear();
    }

    // ── Helpers ───────────────────────────────────────────────────

    // Samples a NavMesh point near the spawn area. If the creature is confined to a
    // preferred area, sample within that area's mask so it lands on its turf.
    private Vector3 ResolveSpawnPosition(PersonalityProfileSO table, Personality personality)
    {
        Vector3 origin  = spawnArea != null ? spawnArea.position : transform.position;
        Vector3 desired = origin + Random.insideUnitSphere * spawnRadius;
        desired.y       = origin.y;

        int mask = NavMesh.AllAreas;
        if (table != null)
        {
            var prof = table.GetProfile(personality);
            if (prof.ConfineToArea)
            {
                int idx = NavMesh.GetAreaFromName(prof.PreferredArea.ToString());
                if (idx >= 0) mask = 1 << idx;
            }
        }

        if (NavMesh.SamplePosition(desired, out var hit, spawnRadius * 3f, mask))
            return hit.position;
        if (NavMesh.SamplePosition(origin, out var hit2, spawnRadius * 5f, NavMesh.AllAreas))
            return hit2.position;
        return origin;
    }

    private static Transform ResolvePlayer()
    {
        var tagged = GameObject.FindGameObjectWithTag("Player");
        if (tagged != null) return tagged.transform;
        return Camera.main != null ? Camera.main.transform : null;
    }

    // ── Dev ───────────────────────────────────────────────────────

    [Button("Respawn All", ButtonSizes.Large), GUIColor(0.55f, 1f, 0.7f)]
    private void RespawnAll()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[MoriMochiSpawner] Enter Play mode to respawn."); return; }
        ClearAll();
        if (GameManager.Instance != null) Sync(GameManager.Instance.Registry);
    }
}
