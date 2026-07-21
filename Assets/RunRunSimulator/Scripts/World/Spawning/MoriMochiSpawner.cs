using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

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
// RUNTIME — creatures born after prewarm fall through to controllerPool.Get() (reuse or fresh Instantiate).
// DESPAWN — controllerPool.Return(): deactivate + enqueue to the ControllerPool.
public class MoriMochiSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Prefab root with MoriMonchiController + MoriMochiAgent + MonchiVisualizer.")]
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

    [Tooltip("Fallback: si no llega ninguna carga autoritativa (reload local/nube) en este tiempo, puebla desde el registro local igual — evita esperar para siempre offline. 0 = esperar siempre.")]
    [Min(0f), SerializeField] private float dataReadyTimeout = 6f;

    [Tooltip("Las criaturas ancladas esperan a que su lugar (corral / estante / corral de cría) esté listo para colocarse DIRECTO adentro (sin cañón, sin ragdoll). Si el lugar no aparece en este tiempo (s), recién ahí van por cañón como último recurso. 0 = nunca esperar (cañón siempre).")]
    [Min(0f), SerializeField] private float anchorPlaceTimeout = 5f;

    // ── World gate ─────────────────────────────────────────────────
    // False until the furniture has loaded AND the NavMesh has baked (first OnNavMeshRebaked).
    // The pump won't fire a single creature until this is true.
    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    internal bool WorldReady => worldReady;

    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private bool DataReady => dataReady;

    // ── State ─────────────────────────────────────────────────────
    private readonly Dictionary<string, MoriMonchiController> spawned   = new Dictionary<string, MoriMonchiController>();
    private readonly Dictionary<string, MoriMonchiController> prewarmed = new Dictionary<string, MoriMonchiController>();
    private ControllerPool controllerPool;
    private readonly Queue<CreatureDNA>                       spawnQueue = new Queue<CreatureDNA>();
    // Anchored creatures drain first: placed directly inside their place before any free creature is fired.
    private readonly Queue<CreatureDNA>                       anchoredQueue = new Queue<CreatureDNA>();
    private readonly HashSet<string>                          queued     = new HashSet<string>();

    // childUniqueID → world point to fire a newborn from. Registered by the BreedingContainer that bred
    // it (the pen owns the request), so the cría arcs out of its pen instead of the global muzzle.
    private readonly Dictionary<string, Vector3>             birthLaunchPoints = new Dictionary<string, Vector3>();
    // childUniqueID → world point a newborn lands at (just outside its pen, so it pops OUT of the corral).
    private readonly Dictionary<string, Vector3>             birthLandingPoints = new Dictionary<string, Vector3>();

    // anchoredUniqueID → Time.time deadline. While an anchored creature waits for its place to register we
    // re-queue it instead of cannon-firing it; past the deadline we give up and fire it (place likely gone).
    private readonly Dictionary<string, float>               anchorPlaceDeadline = new Dictionary<string, float>();

    private const float WorldReadyDebounce = 0.75f;

    private Coroutine pump;
    private Coroutine prewarmRoutine;
    private Coroutine worldReadyDebounce;
    private bool      isPrewarming;
    private bool      worldReady;
    private bool      dataReady;
    private Transform player;

    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    internal int SpawnedCount   => spawned.Count;
    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    internal int PooledCount    => controllerPool?.Count ?? 0;
    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    internal int QueuedCount    => spawnQueue.Count + anchoredQueue.Count;
    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    internal int PrewarmedCount => prewarmed.Count;

    internal MoriMonchiController CreaturePrefab => creaturePrefab;
    internal Vector3 MuzzlePosition => launchPoint != null ? launchPoint.position : transform.position;
    internal Vector2 LaunchAngleRange => launchAngle;
    internal IEnumerable<KeyValuePair<string, MoriMonchiController>> SpawnedEntries => spawned;

    // ── Lifecycle ─────────────────────────────────────────────────

    public static MoriMochiSpawner Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
        controllerPool = new ControllerPool(creaturePrefab);
    }

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
        player = SpawnBallistics.ResolvePlayer();
        var registry = GameManager.Instance != null ? GameManager.Instance.Registry : null;
        if (registry != null)
            prewarmRoutine = StartCoroutine(PrewarmAndStart(registry));
        StartCoroutine(DataReadyFallback());
    }

    private IEnumerator DataReadyFallback()
    {
        float waited = 0f;
        while (!dataReady && (dataReadyTimeout <= 0f || waited < dataReadyTimeout))
        {
            waited += Time.deltaTime;
            yield return null;
        }
        if (dataReady) yield break;
        Debug.LogWarning("[MoriMochiSpawner] Sin carga autoritativa en el timeout — poblando desde el registro local.");
        dataReady = true;
        EnsurePump();
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
    // creature, it leaves from 'muzzle' (the pen) and lands at 'landing' (just outside the pen) instead
    // of the global muzzle + random landing — so the newborn pops OUT of the corral.
    public void RegisterBirthLaunch(string childId, Vector3 muzzle, Vector3 landing)
    {
        if (string.IsNullOrEmpty(childId)) return;
        birthLaunchPoints[childId]  = muzzle;
        birthLandingPoints[childId] = landing;
    }

    private void OnRegistryReloaded(CreatureRegistrySO registry)
    {
        dataReady = true;

        // Cancel any running startup sequence — DNA instances are being replaced.
        if (prewarmRoutine != null)
        {
            StopCoroutine(prewarmRoutine);
            prewarmRoutine = null;
            isPrewarming   = false;
        }

        // Return all prewarmed controllers to the generic pool (their DNA refs are stale).
        foreach (var c in prewarmed.Values)
            if (c != null) controllerPool.Return(c);
        prewarmed.Clear();

        var all   = registry.GetAll();
        var table = GameManager.Instance != null ? GameManager.Instance.RoleWorldProfiles : null;
        var furDb = GameManager.Instance != null ? GameManager.Instance.FurTypeDatabase     : null;

        var stale = spawned.Keys
            .Where(id => !all.TryGetValue(id, out var d) || d.IsDead || d.IsSold)
            .ToList();
        foreach (var id in stale) Despawn(id);

        spawnQueue.Clear();
        anchoredQueue.Clear();
        queued.Clear();
        int rebound = 0, enqueued = 0;
        foreach (var kv in all)
        {
            var dna = kv.Value;
            if (dna.IsDead || dna.IsSold) continue;

            if (spawned.TryGetValue(kv.Key, out var controller))
            {
                // An anchored creature that came back from the pull as a free roamer (lost its place
                // on a cold load): despawn and re-route through placement instead of a stale Rebind.
                if (!string.IsNullOrEmpty(dna.LocationKey) && controller.Agent != null && !controller.Agent.IsPenned)
                {
                    Despawn(kv.Key);
                    Enqueue(dna);
                    enqueued++;
                }
                else
                {
                    controller.Rebind(dna, table, furDb);
                    rebound++;
                }
            }
            else
            {
                Enqueue(dna);
                enqueued++;
            }
        }

        Debug.Log($"[MoriMochiSpawner] Reload reconcile → rebound={rebound} despawned={stale.Count} enqueued={enqueued} (registro={all.Count})");

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
        var table = GameManager.Instance != null ? GameManager.Instance.RoleWorldProfiles : null;
        var bank  = GameManager.Instance != null ? GameManager.Instance.MonchiVisualBank    : null;
        var furDb = GameManager.Instance != null ? GameManager.Instance.FurTypeDatabase     : null;

        // Instantiating off-mesh makes NavMeshAgent.OnEnable error. Park all prewarm instances on a
        // valid NavMesh anchor near the cannon.
        Vector3 prewarmPos = ResolveActivationPoint();

        foreach (var kv in all)
        {
            var dna = kv.Value;
            if (dna.IsDead || dna.IsSold) continue;
            if (creaturePrefab == null) break;

            var controller = Instantiate(creaturePrefab, prewarmPos, Quaternion.identity);
            controller.Initialize(dna, table, player, bank, furDb);
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

    internal void Sync(CreatureRegistrySO registry)
    {
        var all = registry.GetAll();

        foreach (var kv in all)
        {
            var dna = kv.Value;
            if (dna.IsDead || dna.IsSold) continue;
            if (spawned.ContainsKey(kv.Key) || queued.Contains(kv.Key)) continue;
            Enqueue(dna);
        }

        var stale = spawned.Keys
            .Where(id => !all.TryGetValue(id, out var d) || d.IsDead || d.IsSold)
            .ToList();
        foreach (var id in stale) Despawn(id);

        EnsurePump();
    }

    // Anchored creatures go to the priority queue (placed directly in their place); everyone else is fired.
    private void Enqueue(CreatureDNA dna)
    {
        if (!string.IsNullOrEmpty(dna.LocationKey)) anchoredQueue.Enqueue(dna);
        else                                        spawnQueue.Enqueue(dna);
        queued.Add(dna.UniqueID);
    }

    // ── Pump ──────────────────────────────────────────────────────

    private IEnumerator SpawnPump()
    {
        var wait = spawnInterval > 0f ? new WaitForSeconds(spawnInterval) : null;

        while (anchoredQueue.Count > 0 || spawnQueue.Count > 0)
        {
            for (int i = 0; i < spawnPerTick && (anchoredQueue.Count > 0 || spawnQueue.Count > 0); i++)
            {
                var dna = anchoredQueue.Count > 0 ? anchoredQueue.Dequeue() : spawnQueue.Dequeue();
                queued.Remove(dna.UniqueID);

                if (!dna.IsDead && !dna.IsSold && !spawned.ContainsKey(dna.UniqueID))
                    SpawnOne(dna);
            }

            if (wait != null) yield return wait;
        }
        pump = null;
    }

    private void EnsurePump()
    {
        if (isPrewarming) return;   // startup sequence hasn't completed yet
        if (!dataReady)    return;
        if (!worldReady)   return;  // world (furniture + NavMesh) not ready — hold the cannon
        if (pump == null && (anchoredQueue.Count > 0 || spawnQueue.Count > 0) && isActiveAndEnabled)
            pump = StartCoroutine(SpawnPump());
    }

    private void SpawnOne(CreatureDNA dna)
    {
        // Anchored creatures are placed directly inside their place — never fired as a ragdoll. If the
        // place isn't ready yet, DEFER (re-queue) rather than cannon-fire them; only fall back to the
        // cannon once anchorPlaceTimeout elapses (place likely gone).
        if (!string.IsNullOrEmpty(dna.LocationKey))
        {
            if (AnchorRegistry.TryGet(dna.LocationKey, out var place) && TryPlaceAtAnchor(dna, place))
            {
                anchorPlaceDeadline.Remove(dna.UniqueID);
                return;
            }
            if (DeferAnchored(dna)) return;

            // The place never showed up (furniture removed while away) → fall through to the cannon and
            // CLEAR the orphan anchor, otherwise this creature would try to re-anchor forever.
            dna.LocationKey  = "";
            dna.LocationSlot = -1;
            if (GameManager.Instance != null && GameManager.Instance.Registry != null)
                GameEvents.RegistryChanged(GameManager.Instance.Registry);
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

        // Newborns land where their pen ejected them to (just outside it); everyone else at a random
        // point inside the spawn radius.
        Vector3 target = birthLandingPoints.TryGetValue(dna.UniqueID, out var ejectTo) ? ejectTo : RandomLandingPoint();
        birthLandingPoints.Remove(dna.UniqueID);
        float   angle  = Random.Range(launchAngle.x, launchAngle.y) * Mathf.Deg2Rad;
        controller.Launch(muzzle, SpawnBallistics.SolveLaunchVelocity(muzzle, target, angle));

        spawned[dna.UniqueID] = controller;
    }

    // Acquires a controller at 'navPoint' — prewarmed (model already assembled, bank=null skips
    // Assemble) or cold (pool/fresh, full bank). Returns null only if a cold spawn can't be made.
    private MoriMonchiController Acquire(CreatureDNA dna, Vector3 navPoint)
    {
        var table = GameManager.Instance != null ? GameManager.Instance.RoleWorldProfiles : null;
        var bank  = GameManager.Instance != null ? GameManager.Instance.MonchiVisualBank    : null;
        var furDb = GameManager.Instance != null ? GameManager.Instance.FurTypeDatabase     : null;

        if (prewarmed.TryGetValue(dna.UniqueID, out var controller))
        {
            prewarmed.Remove(dna.UniqueID);
            controller.transform.SetPositionAndRotation(navPoint, Quaternion.identity);
            controller.gameObject.SetActive(true);
            controller.Initialize(dna, table, player, null, furDb);
            return controller;
        }

        controller = controllerPool.Get(navPoint);
        if (controller == null) return null;
        controller.Initialize(dna, table, player, bank, furDb);
        return controller;
    }

    // Places a creature directly inside its anchored place instead of firing it. Returns false (so
    // SpawnOne falls back to the cannon) if the place rejects it (full / navmesh not ready yet).
    private bool TryPlaceAtAnchor(CreatureDNA dna, IAnchorPlace place)
    {
        Vector3 anchorPos = place.AnchorPosition(dna.LocationSlot);
        Vector3 navPoint  = NavMesh.SamplePosition(anchorPos, out var hit, 5f, NavMesh.AllAreas) ? hit.position : anchorPos;

        var controller = Acquire(dna, navPoint);
        if (controller == null) return false;

        if (!place.TryReclaim(controller.Agent, dna.LocationSlot))
        {
            // Place rejected it — return the controller so SpawnOne's fallback fires it cleanly.
            controllerPool.Return(controller);
            return false;
        }

        controller.name = $"MoriMochi_{dna.CustomName}";
        spawned[dna.UniqueID] = controller;
        return true;
    }

    // An anchored creature whose place isn't registered/ready yet: re-queue it for a later tick instead
    // of ragdolling it out of the cannon. Returns false once anchorPlaceTimeout elapses so SpawnOne fires
    // it as a last resort (timeout 0 disables the wait → always cannon).
    private bool DeferAnchored(CreatureDNA dna)
    {
        if (anchorPlaceTimeout <= 0f) return false;

        if (!anchorPlaceDeadline.TryGetValue(dna.UniqueID, out var deadline))
        {
            deadline = Time.time + anchorPlaceTimeout;
            anchorPlaceDeadline[dna.UniqueID] = deadline;
        }
        if (Time.time >= deadline)
        {
            anchorPlaceDeadline.Remove(dna.UniqueID);
            return false;
        }

        anchoredQueue.Enqueue(dna);
        queued.Add(dna.UniqueID);
        return true;
    }

    // ── Spawn tracking ────────────────────────────────────────────

    private void Despawn(string id)
    {
        if (spawned.TryGetValue(id, out var controller)) controllerPool.Return(controller);
        spawned.Remove(id);
        birthLaunchPoints.Remove(id);
        birthLandingPoints.Remove(id);
        anchorPlaceDeadline.Remove(id);

        // A creature that died before activation — return its prewarmed slot to the pool.
        if (prewarmed.TryGetValue(id, out var prewarmedController))
        {
            controllerPool.Return(prewarmedController);
            prewarmed.Remove(id);
        }
    }

    internal void ClearAll()
    {
        foreach (var controller in spawned.Values) controllerPool.Return(controller);
        spawned.Clear();
        spawnQueue.Clear();
        anchoredQueue.Clear();
        queued.Clear();
        anchorPlaceDeadline.Clear();
    }

    // ── Spawn helpers ─────────────────────────────────────────────

    // A random landing target on the floor plane, inside spawnRadius of the spawn area.
    internal Vector3 RandomLandingPoint()
    {
        Vector3 center = spawnArea != null ? spawnArea.position : transform.position;
        Vector2 disk   = Random.insideUnitCircle * spawnRadius;
        return new Vector3(center.x + disk.x, center.y, center.z + disk.y);
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
            DrawSimulatedArc(muzzle, SpawnBallistics.SolveLaunchVelocity(muzzle, edge, launchAngle.y * Mathf.Deg2Rad), center.y, g);

            Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.5f);
            DrawSimulatedArc(muzzle, SpawnBallistics.SolveLaunchVelocity(muzzle, edge, launchAngle.x * Mathf.Deg2Rad), center.y, g);
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
}
