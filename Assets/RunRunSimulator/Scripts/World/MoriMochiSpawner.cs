using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

// Bridges DATA → PRESENCE: turns registered creatures into live agents by FIRING THEM OUT OF A
// CANNON. There's one spawn shape — a ballistic pop-out — never a quiet placement.
//
// STARTUP — Pre-warm, then gate on the world:
//   On Start(), PrewarmAndStart assembles one creature per frame (model + parts) while inactive,
//   concurrently with startDelay. It then waits for WorldReady — the first NavMesh bake after the
//   furniture loads (OnNavMeshRebaked) — before the pump fires a single shot. So nothing launches
//   into a world without floors or a baked mesh.
//
// THE SHOT — every creature is launched as a RAGDOLL (Rigidbody + collider, NO agent). The spawner
//   picks a random point inside spawnRadius and solves the exact ballistic velocity to land there.
//   The agent only takes over once it settles (MoriMochiAgent's throw pipeline: land → get up →
//   roam). The agent is NEVER enabled mid-air.
//
// RUNTIME — creatures born after prewarm fall through to GetController() (pool or fresh Instantiate).
// DESPAWN — ReturnToPool: deactivate + enqueue to the generic pool.
public class MoriMochiSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Prefab root with MoriMonchiController + MoriMochiAgent + MoriMonchiVisualizer.")]
    [Required, SerializeField] private MoriMonchiController creaturePrefab;

    // ── Cannon ────────────────────────────────────────────────────
    [Title("Cannon")]
    [Tooltip("Muzzle the creatures pop out of. If null, this object's position is used. Place it above the floor so they arc down.")]
    [SerializeField] private Transform launchPoint;

    [Tooltip("Center of the landing zone. Creatures land at a random point within 'Spawn Radius' of it. If null, this object's position is used.")]
    [SerializeField] private Transform spawnArea;

    [Tooltip("Radius of the landing zone (m). The ONLY real aiming knob — each shot's velocity is solved so it lands at a random point inside this ring.")]
    [Min(0.5f), SerializeField] private float spawnRadius = 5f;

    [Tooltip("Firing ELEVATION above the horizontal, in degrees — random per shot in [min, max]. Only shapes the ARC (high lob vs flat liner); the speed auto-adjusts so it still lands inside the radius.")]
    [MinMaxSlider(20f, 80f, true)]
    [SerializeField] private Vector2 launchAngle = new Vector2(45f, 65f);

    // ── Pooling / cadence ──────────────────────────────────────────
    [Title("Pooling / cadence")]
    [Tooltip("Startup window in seconds. The pre-warm runs 1 creature/frame during this window. The pump ALSO waits for the world to be ready (first NavMesh bake). Minimum 2 s guarantees the pre-warm completes before the first shot.")]
    [PropertyRange(2f, 15f), SerializeField] private float startDelay = 2f;

    [Tooltip("Seconds between shots. Each tick the cannon fires 'spawnPerTick' creatures — controls the visual cadence. 0 = instant flood.")]
    [Min(0f), SerializeField] private float spawnInterval = 0.2f;

    [Tooltip("How many MoriMonchis to fire per tick.")]
    [Min(1), SerializeField] private int spawnPerTick = 1;

    [Tooltip("Fallback only: if this scene has no NavMeshSurface (so OnNavMeshRebaked never fires), the pump fires anyway this many seconds after prewarm. 0 = wait forever for the bake.")]
    [Min(0f), SerializeField] private float navMeshWaitTimeout = 10f;

    // ── World gate ─────────────────────────────────────────────────
    // False until the furniture has loaded AND the NavMesh has baked (first OnNavMeshRebaked).
    // The pump won't fire a single creature until this is true.
    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private bool WorldReady => worldReady;

    // ── State ─────────────────────────────────────────────────────
    private readonly Dictionary<string, MoriMonchiController> spawned   = new Dictionary<string, MoriMonchiController>();
    private readonly Dictionary<string, MoriMonchiController> prewarmed = new Dictionary<string, MoriMonchiController>();
    private readonly Queue<MoriMonchiController>              pool       = new Queue<MoriMonchiController>();
    private readonly Queue<CreatureDNA>                       spawnQueue = new Queue<CreatureDNA>();
    private readonly HashSet<string>                          queued     = new HashSet<string>();

    private Coroutine pump;
    private Coroutine prewarmRoutine;
    private bool      isPrewarming;
    private bool      worldReady;
    private Transform player;

    private readonly List<GameObject> debugShots = new List<GameObject>();

    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private int SpawnedCount   => spawned.Count;
    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private int PooledCount    => pool.Count;
    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private int QueuedCount    => spawnQueue.Count;
    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private int PrewarmedCount => prewarmed.Count;
    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private int DebugShotCount => debugShots.Count;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void OnEnable()
    {
        GameEvents.OnRegistryChanged  += OnRegistryChanged;
        GameEvents.OnRegistryReloaded += OnRegistryReloaded;
        GameEvents.OnNavMeshRebaked   += OnNavMeshReady;
    }

    private void OnDisable()
    {
        GameEvents.OnRegistryChanged  -= OnRegistryChanged;
        GameEvents.OnRegistryReloaded -= OnRegistryReloaded;
        GameEvents.OnNavMeshRebaked   -= OnNavMeshReady;
        pump           = null;
        prewarmRoutine = null;
        isPrewarming   = false;
    }

    private void Start()
    {
        player = ResolvePlayer();
        var registry = GameManager.Instance != null ? GameManager.Instance.Registry : null;
        if (registry != null)
            prewarmRoutine = StartCoroutine(PrewarmAndStart(registry));
    }

    // The first NavMesh bake completed — the world (furniture geometry + baked mesh) is ready.
    // Flip the gate and kick the pump in case prewarm already finished waiting on it. Runtime
    // rebakes after this are no-ops here (pump's already running; agents handle them themselves).
    private void OnNavMeshReady()
    {
        worldReady = true;
        if (!isPrewarming) EnsurePump();
    }

    private void OnRegistryChanged(CreatureRegistrySO registry) => Sync(registry);

    private void OnRegistryReloaded(CreatureRegistrySO registry)
    {
        // Cancel any running startup sequence — DNA instances are being replaced.
        if (prewarmRoutine != null)
        {
            StopCoroutine(prewarmRoutine);
            prewarmRoutine = null;
            isPrewarming   = false;
        }

        // Return all prewarmed controllers to the generic pool (their DNA refs are stale).
        foreach (var c in prewarmed.Values)
            if (c != null) ReturnToPool(c);
        prewarmed.Clear();

        var all   = registry.GetAll();
        var table = GameManager.Instance != null ? GameManager.Instance.PersonalityProfiles : null;

        var stale = spawned.Keys
            .Where(id => !all.TryGetValue(id, out var d) || d.IsDead)
            .ToList();
        foreach (var id in stale) Despawn(id);

        spawnQueue.Clear();
        queued.Clear();
        foreach (var kv in all)
        {
            var dna = kv.Value;
            if (dna.IsDead) continue;

            if (spawned.TryGetValue(kv.Key, out var controller))
                controller.Initialize(dna, table, player, GameManager.Instance?.PartVisualBank);
            else
            {
                spawnQueue.Enqueue(dna);
                queued.Add(kv.Key);
            }
        }

        EnsurePump();
    }

    // ── Pre-warm ──────────────────────────────────────────────────

    // Assembles one controller per frame while inactive, waits the rest of startDelay, then blocks
    // on WorldReady (the first NavMesh bake) before handing off to the pump — so nothing is fired
    // into a world that has no floors or no baked mesh yet.
    private IEnumerator PrewarmAndStart(CreatureRegistrySO registry)
    {
        isPrewarming = true;
        float startTime = Time.time;

        var all   = registry.GetAll();
        var table = GameManager.Instance != null ? GameManager.Instance.PersonalityProfiles : null;
        var bank  = GameManager.Instance != null ? GameManager.Instance.PartVisualBank      : null;

        // Instantiating off-mesh makes NavMeshAgent.OnEnable error. Park all prewarm instances on a
        // valid NavMesh anchor near the cannon.
        Vector3 prewarmPos = ResolveActivationPoint();

        foreach (var kv in all)
        {
            var dna = kv.Value;
            if (dna.IsDead) continue;
            if (creaturePrefab == null) break;

            var controller = Instantiate(creaturePrefab, prewarmPos, Quaternion.identity);
            controller.Initialize(dna, table, player, bank);
            controller.gameObject.SetActive(false);
            prewarmed[dna.UniqueID] = controller;

            yield return null;  // one creature per frame
        }

        // Wait the remainder of startDelay if prewarm finished early.
        float remaining = startDelay - (Time.time - startTime);
        if (remaining > 0f) yield return new WaitForSeconds(remaining);

        // Gate on the world being ready (first NavMesh bake). Fallback: if there's no NavMeshSurface
        // in this scene the event never fires, so proceed after the timeout (0 = wait forever).
        if (!worldReady && navMeshWaitTimeout > 0f)
        {
            float waited = 0f;
            while (!worldReady && waited < navMeshWaitTimeout)
            {
                waited += Time.deltaTime;
                yield return null;
            }
            if (!worldReady)
            {
                Debug.LogWarning("[MoriMochiSpawner] World not ready after timeout — firing anyway.");
                worldReady = true;   // unblock EnsurePump for the rest of the session
            }
        }
        else if (!worldReady)
        {
            while (!worldReady) yield return null;   // timeout disabled → wait for the bake
        }

        isPrewarming   = false;
        prewarmRoutine = null;

        // Sync against live registry (may have changed while prewarm ran) and fire the pump.
        var current = GameManager.Instance != null ? GameManager.Instance.Registry : null;
        if (current != null) Sync(current);
        else EnsurePump();
    }

    // ── Sync ──────────────────────────────────────────────────────

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

        var stale = spawned.Keys
            .Where(id => !all.TryGetValue(id, out var d) || d.IsDead)
            .ToList();
        foreach (var id in stale) Despawn(id);

        EnsurePump();
    }

    // ── Pump ──────────────────────────────────────────────────────

    private IEnumerator SpawnPump()
    {
        var wait = spawnInterval > 0f ? new WaitForSeconds(spawnInterval) : null;

        while (spawnQueue.Count > 0)
        {
            for (int i = 0; i < spawnPerTick && spawnQueue.Count > 0; i++)
            {
                var dna = spawnQueue.Dequeue();
                queued.Remove(dna.UniqueID);

                if (!dna.IsDead && !spawned.ContainsKey(dna.UniqueID))
                    SpawnOne(dna);
            }

            if (wait != null) yield return wait;
        }
        pump = null;
    }

    private void EnsurePump()
    {
        if (isPrewarming) return;   // startup sequence hasn't completed yet
        if (!worldReady)   return;  // world (furniture + NavMesh) not ready — hold the cannon
        if (pump == null && spawnQueue.Count > 0 && isActiveAndEnabled)
            pump = StartCoroutine(SpawnPump());
    }

    private void SpawnOne(CreatureDNA dna)
    {
        var table = GameManager.Instance != null ? GameManager.Instance.PersonalityProfiles : null;
        var bank  = GameManager.Instance != null ? GameManager.Instance.PartVisualBank      : null;

        // Activate on a valid NavMesh point so NavMeshAgent.OnEnable never fires off-mesh, then
        // PrepareForLaunch disables the agent and teleports to the muzzle — never the reverse.
        Vector3 navPoint = ResolveActivationPoint();

        MoriMonchiController controller;

        // Hot path: prewarmed controller already has its model assembled — reposition, enable,
        // re-init the agent (bank=null skips Assemble, leaving the model intact).
        if (prewarmed.TryGetValue(dna.UniqueID, out controller))
        {
            prewarmed.Remove(dna.UniqueID);
            controller.transform.SetPositionAndRotation(navPoint, Quaternion.identity);
            controller.gameObject.SetActive(true);
            controller.Initialize(dna, table, player, null);
        }
        else
        {
            // Cold path: runtime spawn (born after prewarm, or prewarm interrupted).
            controller = GetController(navPoint);
            if (controller == null) return;
            controller.Initialize(dna, table, player, bank);
        }

        controller.name = $"MoriMochi_{dna.CustomName}";

        // Fire it: solve the velocity that lands at a random point inside the radius.
        Vector3 muzzle = launchPoint != null ? launchPoint.position : transform.position;
        Vector3 target = RandomLandingPoint();
        float   angle  = Random.Range(launchAngle.x, launchAngle.y) * Mathf.Deg2Rad;
        controller.PrepareForLaunch(muzzle, SolveLaunchVelocity(muzzle, target, angle));

        spawned[dna.UniqueID] = controller;
    }

    // ── Pool ──────────────────────────────────────────────────────

    private MoriMonchiController GetController(Vector3 pos)
    {
        MoriMonchiController controller = null;
        while (controller == null && pool.Count > 0) controller = pool.Dequeue();

        if (controller == null)
        {
            if (creaturePrefab == null)
            {
                Debug.LogError("[MoriMochiSpawner] No creature prefab assigned.");
                return null;
            }
            return Instantiate(creaturePrefab, pos, Quaternion.identity);
        }

        controller.transform.SetPositionAndRotation(pos, Quaternion.identity);
        controller.gameObject.SetActive(true);
        return controller;
    }

    private void ReturnToPool(MoriMonchiController controller)
    {
        if (controller == null) return;
        controller.PrepareForPool();
        controller.gameObject.SetActive(false);
        pool.Enqueue(controller);
    }

    private void Despawn(string id)
    {
        if (spawned.TryGetValue(id, out var controller)) ReturnToPool(controller);
        spawned.Remove(id);

        // A creature that died before activation — return its prewarmed slot to the pool.
        if (prewarmed.TryGetValue(id, out var prewarmedController))
        {
            ReturnToPool(prewarmedController);
            prewarmed.Remove(id);
        }
    }

    private void ClearAll()
    {
        foreach (var controller in spawned.Values) ReturnToPool(controller);
        spawned.Clear();
        spawnQueue.Clear();
        queued.Clear();
    }

    // ── Ballistics ────────────────────────────────────────────────

    // A random landing target on the floor plane, inside spawnRadius of the spawn area.
    private Vector3 RandomLandingPoint()
    {
        Vector3 center = spawnArea != null ? spawnArea.position : transform.position;
        Vector2 disk   = Random.insideUnitCircle * spawnRadius;
        return new Vector3(center.x + disk.x, center.y, center.z + disk.y);
    }

    // Solves the launch velocity to hit 'target' from 'origin' at elevation 'angleRad', accounting
    // for the height difference between muzzle and floor. Classic projectile range equation rearranged
    // for v:  y = d·tanθ − g·d² / (2·v²·cos²θ)  ⇒  v² = g·d² / (2·cos²θ·(d·tanθ − y)).
    // Falls back to a gentle arc if the target is unreachable at this angle (too close/high).
    private Vector3 SolveLaunchVelocity(Vector3 origin, Vector3 target, float angleRad)
    {
        Vector3 flat = target - origin; flat.y = 0f;
        float   d    = flat.magnitude;
        float   y    = target.y - origin.y;          // negative when the floor is below the muzzle
        float   g    = Mathf.Abs(Physics.gravity.y); if (g < 0.01f) g = 9.81f;

        float cos = Mathf.Cos(angleRad), sin = Mathf.Sin(angleRad);
        Vector3 dir;
        if (d > 0.001f) dir = flat / d;
        else { Vector2 r = Random.insideUnitCircle.normalized; dir = new Vector3(r.x, 0f, r.y); }

        float denom = 2f * cos * cos * (d * (sin / cos) - y);
        if (denom <= 0.001f)
        {
            float vFallback = Mathf.Sqrt(g * Mathf.Max(d, 2f));   // safe lob toward the target
            return dir * (vFallback * cos) + Vector3.up * (vFallback * sin);
        }

        float v = Mathf.Sqrt(g * d * d / denom);
        return dir * (v * cos) + Vector3.up * (v * sin);
    }

    // A valid NavMesh point near the cannon, used as the safe activation spot before a launch
    // (NavMeshAgent.OnEnable must fire on-mesh). Samples around the muzzle, then the spawn area.
    private Vector3 ResolveActivationPoint()
    {
        Vector3 probe = launchPoint != null ? launchPoint.position
                      : spawnArea  != null ? spawnArea.position
                      : transform.position;
        if (NavMesh.SamplePosition(probe, out var hit, 50f, NavMesh.AllAreas)) return hit.position;
        if (spawnArea != null && NavMesh.SamplePosition(spawnArea.position, out var hit2, 50f, NavMesh.AllAreas))
            return hit2.position;
        return probe;
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

    // Fires a single plain-physics projectile from the cannon: no NavMeshAgent, no AI, no DNA.
    // Use this in Play mode to verify the arc and landing zone without NavMesh interference.
    [Button("Fire Debug Shot"), GUIColor(1f, 0.85f, 0.35f)]
    private void FireDebugShot()
    {
        if (!Application.isPlaying) { Debug.LogWarning("[MoriMochiSpawner] Enter Play mode."); return; }
        if (creaturePrefab == null) return;

        Vector3 muzzle = launchPoint != null ? launchPoint.position : transform.position;
        var go  = Instantiate(creaturePrefab.gameObject, muzzle, Quaternion.identity);
        go.name = "DebugShot";

        var nav = go.GetComponent<NavMeshAgent>();
        if (nav != null) nav.enabled = false;

        var rb = go.GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = false; rb.useGravity = true; rb.linearDamping = 1.2f; rb.angularDamping = 2f; }

        var col = go.GetComponent<Collider>();
        if (col != null) col.isTrigger = false;

        Vector3 target = RandomLandingPoint();
        float   angle  = Random.Range(launchAngle.x, launchAngle.y) * Mathf.Deg2Rad;
        if (rb != null) rb.AddForce(SolveLaunchVelocity(muzzle, target, angle), ForceMode.VelocityChange);

        debugShots.Add(go);
    }

    [Button("Clear Debug Shots"), GUIColor(1f, 0.5f, 0.5f)]
    private void ClearDebugShots()
    {
        foreach (var s in debugShots) if (s != null) Destroy(s);
        debugShots.Clear();
    }

    // ── Gizmos ───────────────────────────────────────────────────

    // Always-visible: muzzle, landing zone, and the line between them.
    private void OnDrawGizmos()
    {
        Vector3 muzzle = launchPoint != null ? launchPoint.position : transform.position;
        Vector3 center = spawnArea  != null ? spawnArea.position    : transform.position;

        Gizmos.color = new Color(1f, 0.85f, 0f);
        Gizmos.DrawWireSphere(muzzle, 0.3f);

        Gizmos.color = new Color(0.4f, 1f, 0.5f);
        DrawRing(center, spawnRadius, 48);

        Gizmos.color = new Color(1f, 0.85f, 0f, 0.4f);
        Gizmos.DrawLine(muzzle, center);
    }

    // Selected: simulated trajectories to the ring edge, at both the min (cyan) and max (orange)
    // elevation, in 8 directions — shows the real arcs the cannon produces.
    private void OnDrawGizmosSelected()
    {
        Vector3 muzzle = launchPoint != null ? launchPoint.position : transform.position;
        Vector3 center = spawnArea  != null ? spawnArea.position    : transform.position;
        float   g      = Mathf.Abs(Physics.gravity.y); if (g < 0.01f) g = 9.81f;

        for (int d = 0; d < 8; d++)
        {
            float   a    = d * (Mathf.PI / 4f);
            Vector3 edge = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * spawnRadius;

            Gizmos.color = new Color(1f, 0.55f, 0.15f, 0.6f);
            DrawSimulatedArc(muzzle, SolveLaunchVelocity(muzzle, edge, launchAngle.y * Mathf.Deg2Rad), center.y, g);

            Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.5f);
            DrawSimulatedArc(muzzle, SolveLaunchVelocity(muzzle, edge, launchAngle.x * Mathf.Deg2Rad), center.y, g);
        }
    }

    // Steps a projectile (gravity only — ignores drag) and draws it until it drops back to groundY.
    private static void DrawSimulatedArc(Vector3 origin, Vector3 vel, float groundY, float g)
    {
        const int   steps = 48;
        const float dt    = 0.04f;
        Vector3 p = origin, v = vel, prev = origin;
        for (int i = 0; i < steps; i++)
        {
            v += Vector3.down * g * dt;
            p += v * dt;
            Gizmos.DrawLine(prev, p);
            prev = p;
            if (p.y <= groundY && i > 1) break;
        }
    }

    private static void DrawRing(Vector3 center, float radius, int segments)
    {
        if (radius < 0.05f) return;
        float   step = 2f * Mathf.PI / segments;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float   a    = i * step;
            Vector3 curr = center + new Vector3(Mathf.Cos(a) * radius, 0f, Mathf.Sin(a) * radius);
            Gizmos.DrawLine(prev, curr);
            prev = curr;
        }
    }
}
