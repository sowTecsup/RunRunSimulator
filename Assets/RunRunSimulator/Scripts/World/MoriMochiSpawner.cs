using System.Collections;
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
// Spawning is POOLED and STAGGERED: instead of instantiating a whole batch in one
// frame (FPS spike) and launching them all from the same point at once (they pile
// up and knock each other mid-air), the cannon pulls one creature from the pool
// every spawnInterval seconds. Despawned creatures go back to the pool instead of
// being destroyed, so steady-state churn allocates nothing.
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
    [MinMaxSlider(1f, 60f, true)]
    [SerializeField] private Vector2 launchForce = new Vector2(8f, 14f);
    [TabGroup("Spawn", "Launched (shoot out)")]
    [Tooltip("Firing ELEVATION above the horizontal, in degrees — a random value in [min, max] each shot. 0 = flat shot, 45 = farthest, 90 = straight up. The horizontal heading stays random (360°); this controls the arc. Set min == max for a fixed angle.")]
    [MinMaxSlider(0f, 90f, true)]
    [SerializeField] private Vector2 launchAngle = new Vector2(45f, 70f);

    // ── Pooling / cadence ──
    [TabGroup("Spawn", "Pooling")]
    [Tooltip("Seconds to wait before the cannon starts firing a backlog (lets the scene/NavMesh settle, or just paces a dramatic entrance). Applied once each time the pump begins draining.")]
    [Min(0f), SerializeField] private float startDelay = 0f;
    [TabGroup("Spawn", "Pooling")]
    [Tooltip("Seconds between ticks. Each tick the cannon pulls 'spawnPerTick' MoriMonchis from the pool, so a batch arcs out in bursts instead of all in the same frame — no FPS spike and no mid-air pile-up. 0 = spawn the whole backlog in a single frame (old behavior).")]
    [Min(0f), SerializeField] private float spawnInterval = 0.2f;
    [TabGroup("Spawn", "Pooling")]
    [Tooltip("How many MoriMonchis to launch per tick. 1 = strictly one-by-one; raise it to drain big backlogs faster while still spreading the cost across frames.")]
    [Min(1), SerializeField] private int spawnPerTick = 1;

    // ── State ─────────────────────────────────────────────────────
    private readonly Dictionary<string, MoriMochiAgent> spawned    = new Dictionary<string, MoriMochiAgent>();
    private readonly Queue<MoriMochiAgent>              pool       = new Queue<MoriMochiAgent>();
    private readonly Queue<CreatureDNA>                 spawnQueue = new Queue<CreatureDNA>();   // backlog drained at spawnInterval
    private readonly HashSet<string>                    queued     = new HashSet<string>();      // ids already in spawnQueue (dedupe)
    private Coroutine pump;
    private Transform player;

    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private int SpawnedCount => spawned.Count;
    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private int PooledCount => pool.Count;
    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private int QueuedCount => spawnQueue.Count;

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
        pump = null;   // coroutines are stopped automatically when the object disables
    }

    private void Start()
    {
        player = ResolvePlayer();
        var registry = GameManager.Instance != null ? GameManager.Instance.Registry : null;
        if (registry != null) Sync(registry);
    }

    // Incremental: same dna instances are mutated in place, so live agents stay valid.
    private void OnRegistryChanged(CreatureRegistrySO registry) => Sync(registry);

    // Reload (cloud pull/reset) swaps the dna OBJECTS, so live agents now hold stale refs.
    // We RECONCILE rather than ClearAll: a creature still present is re-bound in place to its
    // new dna instance (re-tint + new needs, no despawn) so the world doesn't blink out and
    // re-stream one-by-one over the spawn interval. Only the genuinely-gone are despawned and
    // only the genuinely-new are enqueued.
    private void OnRegistryReloaded(CreatureRegistrySO registry)
    {
        var all   = registry.GetAll();
        var table = GameManager.Instance != null ? GameManager.Instance.PersonalityProfiles : null;

        var stale = spawned.Keys
            .Where(id => !all.TryGetValue(id, out var d) || d.IsDead)
            .ToList();
        foreach (var id in stale) Despawn(id);

        // Rebuild the pending backlog against the new instances: anything not already spawned
        // (genuinely new OR still-queued from this batch) is (re)enqueued with its fresh dna ref,
        // so a queued creature never spawns against an orphaned pre-reload instance.
        spawnQueue.Clear();
        queued.Clear();
        foreach (var kv in all)
        {
            var dna = kv.Value;
            if (dna.IsDead) continue;

            if (spawned.TryGetValue(kv.Key, out var agent))
                agent.Initialize(dna, table, player);          // rebind in place to the new instance
            else
            {
                spawnQueue.Enqueue(dna);
                queued.Add(kv.Key);
            }
        }

        EnsurePump();
    }

    // ── Sync ──────────────────────────────────────────────────────

    // Enqueues newly-registered living creatures (drained one-by-one by the pump) and
    // despawns the ones that died or were removed. Enqueuing — not spawning here — is what
    // staggers the batch.
    private void Sync(CreatureRegistrySO registry)
    {
        var all = registry.GetAll();

        foreach (var kv in all)
        {
            var dna = kv.Value;
            if (dna.IsDead) continue;
            if (spawned.ContainsKey(kv.Key) || queued.Contains(kv.Key)) continue;
            spawnQueue.Enqueue(dna);
            queued.Add(kv.Key);
        }

        // Despawn the ones that died or were removed from the registry.
        var stale = spawned.Keys
            .Where(id => !all.TryGetValue(id, out var d) || d.IsDead)
            .ToList();
        foreach (var id in stale) Despawn(id);

        EnsurePump();
    }

    // Drains the backlog: one spawn per spawnInterval (or the whole backlog at once if 0).
    // Self-terminating — clears 'pump' when the queue empties so EnsurePump can restart it.
    private IEnumerator SpawnPump()
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        var wait = spawnInterval > 0f ? new WaitForSeconds(spawnInterval) : null;

        while (spawnQueue.Count > 0)
        {
            for (int i = 0; i < spawnPerTick && spawnQueue.Count > 0; i++)
            {
                var dna = spawnQueue.Dequeue();
                queued.Remove(dna.UniqueID);

                // Re-validate: it may have died or already been spawned while waiting in the backlog.
                if (!dna.IsDead && !spawned.ContainsKey(dna.UniqueID))
                    SpawnOne(dna);
            }

            if (wait != null) yield return wait;
        }
        pump = null;
    }

    private void EnsurePump()
    {
        if (pump == null && spawnQueue.Count > 0 && isActiveAndEnabled)
            pump = StartCoroutine(SpawnPump());
    }

    private void SpawnOne(CreatureDNA dna)
    {
        var table = GameManager.Instance != null ? GameManager.Instance.PersonalityProfiles : null;

        // Launched starts at the launch point (off the NavMesh, in the air); Placed snaps onto it.
        Vector3 pos = spawnMode == SpawnMode.Launched
            ? (launchPoint != null ? launchPoint.position : transform.position)
            : ResolveSpawnPosition(table, dna.Personality);

        var agent = GetAgent(pos);
        if (agent == null) return;

        agent.name = $"MoriMochi_{dna.CustomName}";
        agent.Initialize(dna, table, player);

        // Pop it out in a random direction; the agent's bounce/settle/get-up pipeline lands it near
        // here, then it roams to its preferred area like any landed throw.
        if (spawnMode == SpawnMode.Launched)
            agent.Launch(RandomLaunchImpulse());

        spawned[dna.UniqueID] = agent;
    }

    // ── Pool ──────────────────────────────────────────────────────

    // Reuses an inactive instance (repositioned + reactivated) or instantiates a fresh one when
    // the pool is empty. Skips any pooled entries destroyed out from under us.
    private MoriMochiAgent GetAgent(Vector3 pos)
    {
        MoriMochiAgent agent = null;
        while (agent == null && pool.Count > 0) agent = pool.Dequeue();

        if (agent == null)
        {
            if (creaturePrefab == null)
            {
                Debug.LogError("[MoriMochiSpawner] No creature prefab assigned.");
                return null;
            }
            return Instantiate(creaturePrefab, pos, Quaternion.identity);
        }

        agent.transform.SetPositionAndRotation(pos, Quaternion.identity);
        agent.gameObject.SetActive(true);
        return agent;
    }

    // Parks an instance back in the pool: free its station/pen, deactivate, enqueue.
    private void ReturnToPool(MoriMochiAgent agent)
    {
        if (agent == null) return;
        agent.PrepareForPool();
        agent.gameObject.SetActive(false);
        pool.Enqueue(agent);
    }

    private void Despawn(string id)
    {
        if (spawned.TryGetValue(id, out var agent)) ReturnToPool(agent);
        spawned.Remove(id);
    }

    private void ClearAll()
    {
        foreach (var agent in spawned.Values) ReturnToPool(agent);
        spawned.Clear();
        spawnQueue.Clear();
        queued.Clear();
    }

    // ── Helpers ───────────────────────────────────────────────────

    // A random horizontal heading raised to a random elevation in launchAngle, scaled by a random
    // force in launchForce. Building the direction from an explicit elevation (instead of blending
    // an up-bias into a unit horizontal, which capped the arc at 45°) lets the angle reach 0–90°.
    private Vector3 RandomLaunchImpulse()
    {
        float   angle   = Random.Range(launchAngle.x, launchAngle.y) * Mathf.Deg2Rad;
        Vector2 azimuth = Random.insideUnitCircle.normalized;   // random horizontal heading
        float   cos     = Mathf.Cos(angle);
        Vector3 dir     = new Vector3(azimuth.x * cos, Mathf.Sin(angle), azimuth.y * cos);   // already unit length
        return dir * Random.Range(launchForce.x, launchForce.y);
    }

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
