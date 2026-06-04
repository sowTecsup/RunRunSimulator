using UnityEngine;

// A world object a MoriMochi walks to and uses to refill ONE need (Feeder/RestZone/PlayZone derive
// from this). Self-registers with the NeedStationRegistry so agents can find the closest available
// one. Single-user: an agent reserves it while using and releases it when full or interrupted
// (grabbed). Same World domain as the agent → direct calls, no GameEvents.
//
// Goes on a furniture prefab (spawned by FurnitureSpawner); OnEnable/OnDisable handle (un)registering
// automatically when the piece is placed/removed. (Future: stations deplete a resource the player
// must replenish — for now they refill to full.)
public abstract class NeedStation : MonoBehaviour
{
    [Tooltip("Where the agent stands to use it. Defaults to this transform if left empty.")]
    [SerializeField] private Transform usePoint;
    [Tooltip("Stat points restored per second while an agent is using it (continuous, fills to 100).")]
    [SerializeField, Min(0f)] private float fillPerSecond = 25f;

    public abstract NeedType Need { get; }

    public Vector3 UsePosition => (usePoint != null ? usePoint : transform).position;

    private MoriMochiAgent currentUser;
    public bool IsAvailable => currentUser == null;

    private void Reset()     => usePoint = transform;
    private void OnEnable()  => NeedStationRegistry.Register(this);
    private void OnDisable() => NeedStationRegistry.Unregister(this);

    // Reserve for one agent. Succeeds if free (or already reserved by the same agent).
    public bool TryReserve(MoriMochiAgent agent)
    {
        if (currentUser != null && currentUser != agent) return false;
        currentUser = agent;
        return true;
    }

    public void Release(MoriMochiAgent agent)
    {
        if (currentUser == agent) currentUser = null;
    }

    // Refills this station's need on the agent. Returns true once the stat is full (≥ 100).
    public bool Refill(NeedsState needs, float dt)
    {
        needs.Restore(Need, fillPerSecond * dt);
        return needs.Get(Need) >= 100f;
    }
}
