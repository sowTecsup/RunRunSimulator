using System;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

// Play-mode hotbar: the 6-slot bar of world props the player carries. Owns the
// active slot, the in-hand visual, and the pickup / use / throw API. The unified
// home for every grabbable object EXCEPT MoriMonchi agents (they keep PlayerController's
// physical grab). Slots are backed by PlayerInventorySO.hotbarSlots so the bar
// survives a reload; the active slot's item is re-spawned in hand on load.
//
// Static Instance (mirror of GameManager) so WorldPropInstance.Interact() and
// PlayerController can reach it without serialized refs. HUD events are static and
// separate from GameEvents (mirror of UIManager's UI events).
public class HotbarController : MonoBehaviour
{
    public static HotbarController Instance { get; private set; }

    [Required, AssetsOnly] [SerializeField] private ItemDatabaseSO database;
    [Tooltip("Where the active item shows in the player's hand. Child of the camera so it tracks the look.")]
    [SerializeField] private Transform handAnchor;

    // HUD refresh: slots or active index changed. Throw force/aim come from
    // PlayerController, which owns the camera.
    public static event Action OnHotbarChanged;
    // The active item was used (click). Per-type effect (eat / heal / …) is wired later.
    public static event Action<string> OnItemUsed;

    private int activeSlot;
    private GameObject heldVisual;   // the spawned in-hand object for the active slot

    private PlayerInventorySO Inventory =>
        GameManager.Instance != null ? GameManager.Instance.Inventory : null;

    // ── Lifecycle ─────────────────────────────────────────────────

    private void Awake() => Instance = this;
    private void OnDestroy() { if (Instance == this) Instance = null; }

    // The inventory loads on sign-in (after this Awake), so equip on the reload event.
    private void OnEnable()
    {
        GameEvents.OnInventoryReloaded += OnInventoryReloaded;
        PlayerInputs.HotbarScrolled    += ScrollActive;
        PlayerInputs.DropPressed       += OnDrop;
    }

    private void OnDisable()
    {
        GameEvents.OnInventoryReloaded -= OnInventoryReloaded;
        PlayerInputs.HotbarScrolled    -= ScrollActive;
        PlayerInputs.DropPressed       -= OnDrop;
    }

    private void OnDrop() => DropActive();

    private void OnInventoryReloaded(PlayerInventorySO inv)
    {
        EquipActive();
        OnHotbarChanged?.Invoke();
    }

    // ── Query ─────────────────────────────────────────────────────

    public int ActiveSlot => activeSlot;
    public string ActiveItemId => Inventory != null ? Inventory.GetHotbarSlot(activeSlot) : null;
    public bool HasActiveItem => !string.IsNullOrEmpty(ActiveItemId);

    public bool IsOfferingFood
    {
        get
        {
            string id = ActiveItemId;
            if (string.IsNullOrEmpty(id) || database == null) return false;
            var def = database.GetById(id);
            return def != null && def.Category == WorldPropCategory.Food;
        }
    }

    // ── Selection ─────────────────────────────────────────────────

    // Mouse wheel: step the active slot, wrapping around the 6 slots.
    public void ScrollActive(int dir)
    {
        if (dir == 0) return;
        int n = PlayerInventorySO.HotbarSize;
        SetActiveSlot(((activeSlot + dir) % n + n) % n);
    }

    public void SetActiveSlot(int index)
    {
        if (index < 0 || index >= PlayerInventorySO.HotbarSize || index == activeSlot) return;
        activeSlot = index;
        EquipActive();
        OnHotbarChanged?.Invoke();
    }

    // ── Pickup (tap E on a loose prop) ────────────────────────────

    // Stores a loose prop in the first free slot (preferring the active one) and
    // selects it. Fails if the bar is full. Despawns the loose object — the slot id
    // is the new source of truth, re-spawned in hand by EquipActive.
    public bool PickUp(WorldPropInstance prop)
    {
        var inv = Inventory;
        if (inv == null || prop == null || string.IsNullOrEmpty(prop.ItemId)) return false;

        int slot = FirstFreeSlot(inv);
        if (slot < 0)
        {
            Debug.Log("[HotbarController] Hotbar full — can't pick up.");
            return false;
        }

        inv.SetHotbarSlot(slot, prop.ItemId);
        activeSlot = slot;
        Destroy(prop.gameObject);
        GameEvents.InventoryChanged(inv);
        EquipActive();
        OnHotbarChanged?.Invoke();
        Debug.Log($"[HotbarController] Picked up '{prop.ItemId}' into slot {slot}.");
        return true;
    }

    // Active slot if empty, else the first empty slot, else -1 (full).
    private int FirstFreeSlot(PlayerInventorySO inv)
    {
        if (string.IsNullOrEmpty(inv.GetHotbarSlot(activeSlot))) return activeSlot;
        for (int i = 0; i < PlayerInventorySO.HotbarSize; i++)
            if (string.IsNullOrEmpty(inv.GetHotbarSlot(i))) return i;
        return -1;
    }

    // ── Use (click) ───────────────────────────────────────────────

    // Click with an active item → its effect (eat / heal / …). The effect itself is
    // wired later via OnItemUsed; consumption (clearing the slot) belongs to that
    // per-type logic, not here.
    public void UseActive()
    {
        string id = ActiveItemId;
        if (string.IsNullOrEmpty(id)) return;
        Debug.Log($"[HotbarController] Use '{id}'.");
        OnItemUsed?.Invoke(id);
    }

    public bool TryConsumeActiveFood()
    {
        if (!IsOfferingFood) return false;
        var inv = Inventory;
        if (inv == null) return false;

        inv.ClearHotbarSlot(activeSlot);
        if (heldVisual != null) { Destroy(heldVisual); heldVisual = null; }
        GameEvents.InventoryChanged(inv);
        OnHotbarChanged?.Invoke();
        return true;
    }

    // ── Throw (hold E) / Drop (Q) ─────────────────────────────────

    // Throws the active item with an impulse toward the aim (force from PlayerController).
    // WorldPropInstance prefabs don't carry ThrowableObject (IThrowable), so if no
    // throwable is found we apply the same linearVelocity trick directly — AddForce is
    // swallowed on the frame isKinematic flips to false (done by ReleaseHeld above).
    public bool ThrowActive(Vector3 force)
    {
        var obj = ReleaseActiveIntoWorld();
        if (obj == null) return false;

        if (obj.TryGetComponent<IThrowable>(out var throwable))
        {
            throwable.OnThrow(force);
        }
        else if (obj.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.isKinematic     = false;
            rb.angularVelocity = Vector3.zero;
            rb.linearVelocity  = force / Mathf.Max(rb.mass, 0.0001f);
        }
        return true;
    }

    // Drops the active item with no impulse — it just falls from the hand to your feet.
    public bool DropActive()
    {
        var obj = ReleaseActiveIntoWorld();
        if (obj == null) return false;
        if (obj.TryGetComponent<IThrowable>(out var throwable)) throwable.OnRelease();
        return true;
    }

    // Shared by throw and drop: clears the active slot and detaches the in-hand object
    // back into the world as a loose, re-pickable WorldProp. Returns it (or null).
    private GameObject ReleaseActiveIntoWorld()
    {
        var inv = Inventory;
        if (inv == null || heldVisual == null || string.IsNullOrEmpty(ActiveItemId)) return null;

        var obj = heldVisual;
        heldVisual = null;
        inv.ClearHotbarSlot(activeSlot);
        ReleaseHeld(obj);

        GameEvents.InventoryChanged(inv);
        OnHotbarChanged?.Invoke();
        return obj;
    }

    // ── In-hand visual ────────────────────────────────────────────

    // Re-spawns the active slot's item at the hand anchor (kinematic, non-colliding,
    // marked held). Called on every selection change and on inventory reload.
    private void EquipActive()
    {
        if (heldVisual != null) { Destroy(heldVisual); heldVisual = null; }

        string id = ActiveItemId;
        if (string.IsNullOrEmpty(id) || handAnchor == null) return;

        var def = database != null ? database.GetById(id) : null;
        if (def == null || def.Prefab == null) return;

        heldVisual = Instantiate(def.Prefab, handAnchor);
        heldVisual.transform.localPosition = Vector3.zero;
        heldVisual.transform.localRotation = Quaternion.identity;
        HoldInHand(heldVisual);

        if (heldVisual.TryGetComponent<WorldPropInstance>(out var marker))
        {
            marker.Configure(id);
            marker.IsHeld = true;
        }
    }

    // While held: kinematic + colliders off so it sits in the hand instead of falling
    // or shoving things. (Throw reverses this.)
    private static void HoldInHand(GameObject go)
    {
        if (go.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
        foreach (var col in go.GetComponentsInChildren<Collider>()) col.enabled = false;
    }

    // Back to a free physics body in the world.
    private static void ReleaseHeld(GameObject go)
    {
        go.transform.SetParent(null, true);
        foreach (var col in go.GetComponentsInChildren<Collider>()) col.enabled = true;
        if (go.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = false;
        if (go.TryGetComponent<WorldPropInstance>(out var marker)) marker.IsHeld = false;
    }
}
}
