using System;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class HotbarController : MonoBehaviour
{
    public static HotbarController Instance { get; private set; }

    [Required, AssetsOnly] [SerializeField] private ItemDatabaseSO database;
    [Tooltip("Where the active item shows in the player's hand. Child of the camera so it tracks the look.")]
    [SerializeField] private Transform handAnchor;

    public static event Action OnHotbarChanged;
    public static event Action<string> OnItemUsed;

    private int activeSlot;
    private GameObject heldVisual;

    private PlayerInventorySO Inventory => GameManager.CurrentInventory;

    private void Awake() => Instance = this;
    private void OnDestroy() { if (Instance == this) Instance = null; }

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

    public int ActiveSlot => activeSlot;
    public string ActiveItemId => Inventory != null ? Inventory.GetHotbarSlot(activeSlot) : null;
    public bool HasActiveItem => !string.IsNullOrEmpty(ActiveItemId);

    public bool IsOfferingFood
    {
        get
        {
            string id = ActiveItemId;
            if (string.IsNullOrEmpty(id) || database == null) return false;
            var def = database.GetByID(id);
            return def != null && def.Category == WorldPropCategory.Food;
        }
    }

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

    private int FirstFreeSlot(PlayerInventorySO inv)
    {
        if (string.IsNullOrEmpty(inv.GetHotbarSlot(activeSlot))) return activeSlot;
        for (int i = 0; i < PlayerInventorySO.HotbarSize; i++)
            if (string.IsNullOrEmpty(inv.GetHotbarSlot(i))) return i;
        return -1;
    }

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

    public bool DropActive()
    {
        var obj = ReleaseActiveIntoWorld();
        if (obj == null) return false;
        if (obj.TryGetComponent<IThrowable>(out var throwable)) throwable.OnRelease();
        return true;
    }

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

    private void EquipActive()
    {
        if (heldVisual != null) { Destroy(heldVisual); heldVisual = null; }

        string id = ActiveItemId;
        if (string.IsNullOrEmpty(id) || handAnchor == null) return;

        var def = database != null ? database.GetByID(id) : null;
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

    private static void HoldInHand(GameObject go)
    {
        if (go.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = true;
        foreach (var col in go.GetComponentsInChildren<Collider>()) col.enabled = false;
    }

    private static void ReleaseHeld(GameObject go)
    {
        go.transform.SetParent(null, true);
        foreach (var col in go.GetComponentsInChildren<Collider>()) col.enabled = true;
        if (go.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = false;
        if (go.TryGetComponent<WorldPropInstance>(out var marker)) marker.IsHeld = false;
    }
}
}
