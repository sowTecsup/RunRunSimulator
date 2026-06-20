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
public partial class MoriMochiSpawner : MonoBehaviour
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
    // Breeders drain first: placed directly inside their pen before any free creature is fired.
    private readonly Queue<CreatureDNA>                       breederQueue = new Queue<CreatureDNA>();
    private readonly HashSet<string>                          queued     = new HashSet<string>();

    // childUniqueID → world point to fire a newborn from. Registered by the BreedingContainer that bred
    // it (the pen owns the request), so the cría arcs out of its pen instead of the global muzzle.
    private readonly Dictionary<string, Vector3>             birthLaunchPoints = new Dictionary<string, Vector3>();

    private const float WorldReadyDebounce = 0.75f;

    private Coroutine pump;
    private Coroutine prewarmRoutine;
    private Coroutine worldReadyDebounce;
    private bool      isPrewarming;
    private bool      worldReady;
    private Transform player;

    private readonly List<GameObject> debugShots = new List<GameObject>();

    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private int SpawnedCount   => spawned.Count;
    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private int PooledCount    => pool.Count;
    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private int QueuedCount    => spawnQueue.Count + breederQueue.Count;
    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private int PrewarmedCount => prewarmed.Count;
    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private int DebugShotCount => debugShots.Count;

    // ── Lifecycle ─────────────────────────────────────────────────

    public static MoriMochiSpawner Instance { get; private set; }

    private void Awake() => Instance = this;

    private void OnDestroy() { if (Instance == this) Instance = null; }

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
        pump               = null;
        prewarmRoutine     = null;
        worldReadyDebounce = null;
        isPrewarming       = false;
    }

    private void Start()
    {
        player = ResolvePlayer();
        var registry = GameManager.Instance != null ? GameManager.Instance.Registry : null;
        if (registry != null)
            prewarmRoutine = StartCoroutine(PrewarmAndStart(registry));
    }

    private void OnNavMeshReady()
    {
        if (worldReady) return;
        if (worldReadyDebounce != null) StopCoroutine(worldReadyDebounce);
        worldReadyDebounce = StartCoroutine(WorldReadyAfterDebounce());
    }

    private IEnumerator WorldReadyAfterDebounce()
    {
        yield return new WaitForSeconds(WorldReadyDebounce);
        worldReadyDebounce = null;
        worldReady = true;
        if (!isPrewarming) EnsurePump();
    }

    private void OnRegistryChanged(CreatureRegistrySO registry) => Sync(registry);

    // Called by the BreedingContainer that just bred a child: the next time the spawner fires this
    // creature, it leaves from 'worldPoint' (the pen) instead of the global muzzle.
    public void RegisterBirthLaunch(string childId, Vector3 worldPoint)
    {
        if (!string.IsNullOrEmpty(childId)) birthLaunchPoints[childId] = worldPoint;
    }

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
        breederQueue.Clear();
        queued.Clear();
        foreach (var kv in all)
        {
            var dna = kv.Value;
            if (dna.IsDead) continue;

            if (spawned.TryGetValue(kv.Key, out var controller))
                controller.Initialize(dna, table, player, GameManager.Instance?.PartVisualBank);
            else
                Enqueue(dna);
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
            Enqueue(dna);
        }

        var stale = spawned.Keys
            .Where(id => !all.TryGetValue(id, out var d) || d.IsDead)
            .ToList();
        foreach (var id in stale) Despawn(id);

        EnsurePump();
    }

    // Breeders go to the priority queue (placed directly in their pen); everyone else is fired.
    private void Enqueue(CreatureDNA dna)
    {
        if (dna.BusyState == BusyReason.Breeding) breederQueue.Enqueue(dna);
        else                                      spawnQueue.Enqueue(dna);
        queued.Add(dna.UniqueID);
    }

    // ── Pump ──────────────────────────────────────────────────────

    private IEnumerator SpawnPump()
    {
        var wait = spawnInterval > 0f ? new WaitForSeconds(spawnInterval) : null;

        while (breederQueue.Count > 0 || spawnQueue.Count > 0)
        {
            for (int i = 0; i < spawnPerTick && (breederQueue.Count > 0 || spawnQueue.Count > 0); i++)
            {
                var dna = breederQueue.Count > 0 ? breederQueue.Dequeue() : spawnQueue.Dequeue();
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
        if (pump == null && (breederQueue.Count > 0 || spawnQueue.Count > 0) && isActiveAndEnabled)
            pump = StartCoroutine(SpawnPump());
    }

    private void SpawnOne(CreatureDNA dna)
    {
        // Breeders are placed directly inside their pen — no cannon shot. Free creatures fall through.
        if (dna.BusyState == BusyReason.Breeding && BreedingContainer.TryGet(dna.HomePenKey, out var pen))
        {
            if (TryPlaceInPen(dna, pen)) return;
        }

        // Activate on a valid NavMesh point so NavMeshAgent.OnEnable never fires off-mesh, then
        // Launch disables the agent and teleports to the muzzle — never the reverse.
        var controller = Acquire(dna, ResolveActivationPoint());
        if (controller == null) return;

        controller.name = $"MoriMochi_{dna.CustomName}";

        // Newborns fire from the point their breeding pen registered; everyone else from the global muzzle.
        Vector3 muzzle;
        if (birthLaunchPoints.TryGetValue(dna.UniqueID, out var bornPoint))
        {
            muzzle = bornPoint;
            birthLaunchPoints.Remove(dna.UniqueID);
        }
        else muzzle = launchPoint != null ? launchPoint.position : transform.position;

        // Fire it: solve the velocity that lands at a random point inside the radius.
        Vector3 target = RandomLandingPoint();
        float   angle  = Random.Range(launchAngle.x, launchAngle.y) * Mathf.Deg2Rad;
        controller.Launch(muzzle, SolveLaunchVelocity(muzzle, target, angle));

        spawned[dna.UniqueID] = controller;
    }

    // Acquires a controller at 'navPoint' — prewarmed (model already assembled, bank=null skips
    // Assemble) or cold (pool/fresh, full bank). Returns null only if a cold spawn can't be made.
    private MoriMonchiController Acquire(CreatureDNA dna, Vector3 navPoint)
    {
        var table = GameManager.Instance != null ? GameManager.Instance.PersonalityProfiles : null;
        var bank  = GameManager.Instance != null ? GameManager.Instance.PartVisualBank      : null;

        if (prewarmed.TryGetValue(dna.UniqueID, out var controller))
        {
            prewarmed.Remove(dna.UniqueID);
            controller.transform.SetPositionAndRotation(navPoint, Quaternion.identity);
            controller.gameObject.SetActive(true);
            controller.Initialize(dna, table, player, null);
            return controller;
        }

        controller = GetController(navPoint);
        if (controller == null) return null;
        controller.Initialize(dna, table, player, bank);
        return controller;
    }

    // Places a breeder directly inside its pen instead of firing it. Returns false (so SpawnOne falls
    // back to the cannon) if the pen rejects it (full / breeding navmesh not ready yet).
    private bool TryPlaceInPen(CreatureDNA dna, BreedingContainer pen)
    {
        Vector3 navPoint = NavMesh.SamplePosition(pen.Center, out var hit, 5f, NavMesh.AllAreas) ? hit.position : pen.Center;

        var controller = Acquire(dna, navPoint);
        if (controller == null) return false;

        if (!pen.ReclaimDirect(controller.Agent))
        {
            // Pen rejected it — return the controller so SpawnOne's fallback fires it cleanly.
            ReturnToPool(controller);
            return false;
        }

        controller.name = $"MoriMochi_{dna.CustomName}";
        spawned[dna.UniqueID] = controller;
        return true;
    }
}
