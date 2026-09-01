using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
namespace MoriMonchiSimulator
{

public class MoriMochiSpawner : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("Prefab root with MoriMonchiController + MoriMochiAgent + MonchiVisualizer.")]
    [Required, SerializeField] private MoriMonchiController creaturePrefab;

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

    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    internal bool WorldReady => worldReady;

    [ShowInInspector, ReadOnly, BoxGroup("Status")]
    private bool DataReady => dataReady;

    private readonly Dictionary<string, MoriMonchiController> spawned   = new Dictionary<string, MoriMonchiController>();
    private readonly Dictionary<string, MoriMonchiController> prewarmed = new Dictionary<string, MoriMonchiController>();
    private ControllerPool controllerPool;
    private readonly Queue<CreatureDNA>                       spawnQueue = new Queue<CreatureDNA>();
    private readonly Queue<CreatureDNA>                       anchoredQueue = new Queue<CreatureDNA>();
    private readonly HashSet<string>                          queued     = new HashSet<string>();

    private readonly Dictionary<string, Vector3>             birthLaunchPoints = new Dictionary<string, Vector3>();
    private readonly Dictionary<string, Vector3>             birthLandingPoints = new Dictionary<string, Vector3>();

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

    private CreatureRegistrySO Registry => GameManager.Instance != null ? GameManager.Instance.Registry : null;
    private RoleWorldProfileSO Table    => GameManager.Instance != null ? GameManager.Instance.RoleWorldProfiles : null;
    private MonchiVisualBankSO Bank     => GameManager.Instance != null ? GameManager.Instance.MonchiVisualBank : null;
    private FurTypeDatabaseSO  FurDb    => GameManager.Instance != null ? GameManager.Instance.FurTypeDatabase : null;
    private Vector3 LandingCenter => spawnArea != null ? spawnArea.position : transform.position;

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
        var registry = Registry;
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

    public void RegisterBirthLaunch(string childId, Vector3 muzzle, Vector3 landing)
    {
        if (string.IsNullOrEmpty(childId)) return;
        birthLaunchPoints[childId]  = muzzle;
        birthLandingPoints[childId] = landing;
    }

    private void OnRegistryReloaded(CreatureRegistrySO registry)
    {
        dataReady = true;

        if (prewarmRoutine != null)
        {
            StopCoroutine(prewarmRoutine);
            prewarmRoutine = null;
            isPrewarming   = false;
        }

        foreach (var c in prewarmed.Values)
            if (c != null) controllerPool.Return(c);
        prewarmed.Clear();

        var all = registry.GetAll();
        int staleCount = DespawnStale(all);

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
                if (!string.IsNullOrEmpty(dna.LocationKey) && controller.Agent != null && !controller.Agent.IsPenned)
                {
                    Despawn(kv.Key);
                    Enqueue(dna);
                    enqueued++;
                }
                else
                {
                    controller.Rebind(dna, Table, FurDb);
                    rebound++;
                }
            }
            else
            {
                Enqueue(dna);
                enqueued++;
            }
        }

        Debug.Log($"[MoriMochiSpawner] Reload reconcile → rebound={rebound} despawned={staleCount} enqueued={enqueued} (registro={all.Count})");

        EnsurePump();
    }

    private IEnumerator PrewarmAndStart(CreatureRegistrySO registry)
    {
        isPrewarming = true;
        float startTime = Time.time;

        var all   = registry.GetAll();
        var table = Table;
        var bank  = Bank;
        var furDb = FurDb;

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

            yield return null;
        }

        float remaining = startDelay - (Time.time - startTime);
        if (remaining > 0f) yield return new WaitForSeconds(remaining);

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
                worldReady = true;
            }
        }
        else if (!worldReady)
        {
            while (!worldReady) yield return null;
        }

        isPrewarming   = false;
        prewarmRoutine = null;

        var current = Registry;
        if (current != null) Sync(current);
        else EnsurePump();
    }

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

        DespawnStale(all);

        EnsurePump();
    }

    private int DespawnStale(Dictionary<string, CreatureDNA> all)
    {
        var stale = spawned.Keys
            .Where(id => !all.TryGetValue(id, out var d) || d.IsDead || d.IsSold)
            .ToList();
        foreach (var id in stale) Despawn(id);
        return stale.Count;
    }

    private void Enqueue(CreatureDNA dna)
    {
        if (!string.IsNullOrEmpty(dna.LocationKey)) anchoredQueue.Enqueue(dna);
        else                                        spawnQueue.Enqueue(dna);
        queued.Add(dna.UniqueID);
    }

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
        if (isPrewarming) return;
        if (!dataReady)    return;
        if (!worldReady)   return;
        if (pump == null && (anchoredQueue.Count > 0 || spawnQueue.Count > 0) && isActiveAndEnabled)
            pump = StartCoroutine(SpawnPump());
    }

    private void SpawnOne(CreatureDNA dna)
    {
        if (!string.IsNullOrEmpty(dna.LocationKey))
        {
            if (AnchorRegistry.TryGet(dna.LocationKey, out var place) && TryPlaceAtAnchor(dna, place))
            {
                anchorPlaceDeadline.Remove(dna.UniqueID);
                return;
            }
            if (DeferAnchored(dna)) return;

            dna.LocationKey  = "";
            dna.LocationSlot = -1;
            if (Registry != null)
                GameEvents.RegistryChanged(Registry);
        }

        var controller = Acquire(dna, ResolveActivationPoint());
        if (controller == null) return;

        Track(dna, controller);

        Vector3 muzzle;
        if (birthLaunchPoints.TryGetValue(dna.UniqueID, out var bornPoint))
        {
            muzzle = bornPoint;
            birthLaunchPoints.Remove(dna.UniqueID);
        }
        else muzzle = MuzzlePosition;

        Vector3 target = birthLandingPoints.TryGetValue(dna.UniqueID, out var ejectTo) ? ejectTo : RandomLandingPoint();
        birthLandingPoints.Remove(dna.UniqueID);
        float   angle  = Random.Range(launchAngle.x, launchAngle.y) * Mathf.Deg2Rad;
        controller.Launch(muzzle, SpawnBallistics.SolveLaunchVelocity(muzzle, target, angle));
    }

    private MoriMonchiController Acquire(CreatureDNA dna, Vector3 navPoint)
    {
        if (prewarmed.TryGetValue(dna.UniqueID, out var controller))
        {
            prewarmed.Remove(dna.UniqueID);
            controller.transform.SetPositionAndRotation(navPoint, Quaternion.identity);
            controller.gameObject.SetActive(true);
            controller.Initialize(dna, Table, player, null, FurDb);
            return controller;
        }

        controller = controllerPool.Get(navPoint);
        if (controller == null) return null;
        controller.Initialize(dna, Table, player, Bank, FurDb);
        return controller;
    }

    private bool TryPlaceAtAnchor(CreatureDNA dna, MoriMochiContainer place)
    {
        Vector3 anchorPos = place.AnchorPosition(dna.LocationSlot);
        Vector3 navPoint  = NavMesh.SamplePosition(anchorPos, out var hit, 5f, NavMesh.AllAreas) ? hit.position : anchorPos;

        var controller = Acquire(dna, navPoint);
        if (controller == null) return false;

        if (!place.TryReclaim(controller.Agent, dna.LocationSlot))
        {
            controllerPool.Return(controller);
            return false;
        }

        Track(dna, controller);
        return true;
    }

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

    private void Track(CreatureDNA dna, MoriMonchiController controller)
    {
        controller.name = $"MoriMochi_{dna.CustomName}";
        spawned[dna.UniqueID] = controller;
    }

    private void Despawn(string id)
    {
        if (spawned.TryGetValue(id, out var controller)) controllerPool.Return(controller);
        spawned.Remove(id);
        birthLaunchPoints.Remove(id);
        birthLandingPoints.Remove(id);
        anchorPlaceDeadline.Remove(id);

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

    internal Vector3 RandomLandingPoint()
    {
        Vector3 center = LandingCenter;
        Vector2 disk   = Random.insideUnitCircle * spawnRadius;
        return new Vector3(center.x + disk.x, center.y, center.z + disk.y);
    }

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

    private void OnDrawGizmos()
    {
        Vector3 muzzle = MuzzlePosition;
        Vector3 center = LandingCenter;

        Gizmos.color = new Color(1f, 0.85f, 0f);
        Gizmos.DrawWireSphere(muzzle, 0.3f);

        Gizmos.color = new Color(0.4f, 1f, 0.5f);
        SpawnBallistics.DrawRing(center, spawnRadius, 48);

        Gizmos.color = new Color(1f, 0.85f, 0f, 0.4f);
        Gizmos.DrawLine(muzzle, center);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 muzzle = MuzzlePosition;
        Vector3 center = LandingCenter;
        float   g      = Mathf.Abs(Physics.gravity.y); if (g < 0.01f) g = 9.81f;

        for (int d = 0; d < 8; d++)
        {
            float   a    = d * (Mathf.PI / 4f);
            Vector3 edge = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * spawnRadius;

            Gizmos.color = new Color(1f, 0.55f, 0.15f, 0.6f);
            SpawnBallistics.DrawSimulatedArc(muzzle, SpawnBallistics.SolveLaunchVelocity(muzzle, edge, launchAngle.y * Mathf.Deg2Rad), center.y, g);

            Gizmos.color = new Color(0.3f, 0.85f, 1f, 0.5f);
            SpawnBallistics.DrawSimulatedArc(muzzle, SpawnBallistics.SolveLaunchVelocity(muzzle, edge, launchAngle.x * Mathf.Deg2Rad), center.y, g);
        }
    }
}
}
