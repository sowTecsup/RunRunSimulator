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
    // Placed: dropped straight onto the NavMesh near the area. Launched: popped out of a
    // GameObject as a ragdoll in a random direction, then it bounces, settles and roams home.
    private enum SpawnMode { Placed, Launched }

    [Header("Prefab")]
    [Tooltip("Cube prefab with MoriMochiAgent + NavMeshAgent + Rigidbody + Collider and a NameTag child.")]
    [Required, SerializeField] private MoriMochiAgent creaturePrefab;

    [Title("Spawn mode")]
    [EnumToggleButtons, HideLabel]
    [SerializeField] private SpawnMode spawnMode = SpawnMode.Placed;

    // ── Placed (drop onto the NavMesh) ──
    [TabGroup("Spawn", "Placed (drop)")]
    [Tooltip("Where creatures appear. If null, this object's position is used.")]
    [SerializeField] private Transform spawnArea;
    [TabGroup("Spawn", "Placed (drop)")]
    [Tooltip("Creatures are dropped within this radius of the spawn area, snapped to the NavMesh.")]
    [SerializeField] private float spawnRadius = 4f;

    // ── Launched (shoot out of a GameObject) ──
    [TabGroup("Spawn", "Launched (shoot out)")]
    [Tooltip("GameObject the creatures pop out of. If null, this object's position is used. Place it slightly above the floor so they arc down onto the NavMesh.")]
    [SerializeField] private Transform launchPoint;
    [TabGroup("Spawn", "Launched (shoot out)")]
    [Tooltip("Impulse magnitude of the pop-out — a random value in [min, max] each spawn.")]
    [MinMaxSlider(1f, 40f, true)]
    [SerializeField] private Vector2 launchForce = new Vector2(8f, 14f);
    [TabGroup("Spawn", "Launched (shoot out)")]
    [Tooltip("Upward bias blended into the random horizontal direction (0 = flat shot, 1 = mostly up). Higher = a taller arc.")]
    [Range(0f, 1f)]
    [SerializeField] private float launchUpBias = 0.5f;

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

        // Launched starts at the launch point (off the NavMesh, in the air); Placed snaps onto it.
        Vector3 pos = spawnMode == SpawnMode.Launched
            ? (launchPoint != null ? launchPoint.position : transform.position)
            : ResolveSpawnPosition(table, dna.Personality);

        var agent = Instantiate(creaturePrefab, pos, Quaternion.identity);
        agent.name = $"MoriMochi_{dna.CustomName}";
        agent.Initialize(dna, table, player);

        // Pop it out in a random direction; the agent's bounce/settle/get-up pipeline lands it near
        // here, then it roams to its preferred area like any landed throw.
        if (spawnMode == SpawnMode.Launched)
            agent.Launch(RandomLaunchImpulse());

        spawned[dna.UniqueID] = agent;
    }

    // A random horizontal direction with an upward bias, scaled by a random force in launchForce.
    private Vector3 RandomLaunchImpulse()
    {
        Vector2 flat = Random.insideUnitCircle.normalized;
        Vector3 dir  = (new Vector3(flat.x, 0f, flat.y) + Vector3.up * launchUpBias).normalized;
        return dir * Random.Range(launchForce.x, launchForce.y);
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

    // Samples a NavMesh point near the spawn area, biased toward the creature's
    // preferred area so it starts "home" — but it's free to wander off afterwards.
    private Vector3 ResolveSpawnPosition(PersonalityProfileSO table, Personality personality)
    {
        Vector3 origin  = spawnArea != null ? spawnArea.position : transform.position;
        Vector3 desired = origin + Random.insideUnitSphere * spawnRadius;
        desired.y       = origin.y;

        int mask = NavMesh.AllAreas;
        if (table != null)
        {
            int idx = NavMesh.GetAreaFromName(table.GetProfile(personality).PreferredArea.ToString());
            if (idx >= 0) mask = 1 << idx;
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
