using System.Collections;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

// The physical storage box in the shop: an access point to the player's global
// world-prop inventory (worldPropsStored), not a container with its own data. Two
// roles:
//   • Catch  — a WorldProp thrown/dropped into its trigger zone is swept into the
//              inventory and despawned (the "guardar tirando objetos" flow).
//   • Eject  — re-spawns a stored prop into the world (real flow = open UI + Q;
//              here exposed as Odin test buttons until the storage UI lands).
//
// Resolves the inventory via GameManager.Instance (project convention); the item
// database is a serialized asset ref (like FurnitureService's database) so it can
// turn a stored "I#" id back into a prefab.
//
// Scene setup: a SOLID collider on the grab layer (so tapping E hits it) PLUS a
// trigger collider as the catch zone. Both can live on this object/children.
public class StorageContainer : MonoBehaviour, IInteractable
{
    // Single storage box in the shop (only one is sold) — the storage panel reaches it
    // here to eject without a serialized reference.
    public static StorageContainer Instance { get; private set; }

    [Required, AssetsOnly] [SerializeField] private ItemDatabaseSO database;
    [Tooltip("Where ejected props re-appear. Defaults to this transform.")]
    [SerializeField] private Transform ejectPoint;

    // Instance ID of the prop most recently ejected — exempt from OnTriggerEnter re-catch
    // for two physics frames, preventing re-storage when the eject point is inside the zone.
    private int justEjectedId = -1;

    private void Awake() => Instance = this;
    private void OnDestroy() { if (Instance == this) Instance = null; }

    private PlayerInventorySO Inventory =>
        GameManager.Instance != null ? GameManager.Instance.Inventory : null;

    private Transform EjectAt => ejectPoint != null ? ejectPoint : transform;

    // ── Catch: a world prop entered the storage zone ──────────────

    private void OnTriggerEnter(Collider other)
    {
        // Resolve the marker on the collider or its rigidbody root (props are
        // Rigidbody bodies; the collider may be a child).
        var prop = other.GetComponentInParent<WorldPropInstance>();
        if (prop == null || prop.IsHeld || string.IsNullOrEmpty(prop.ItemId)) return;
        if (prop.gameObject.GetInstanceID() == justEjectedId) return;  // just ejected — skip re-catch

        var inventory = Inventory;
        if (inventory == null) return;

        inventory.AddWorldProp(prop.ItemId);
        GameEvents.InventoryChanged(inventory);
        Debug.Log($"[StorageContainer] Stored '{prop.ItemId}'.");
        Destroy(prop.gameObject);
    }

    // ── Interact: open the storage UI (Fase 3 / step 17) ──────────

    public void Interact()
    {
        Debug.Log($"[StorageContainer] Interact — {DescribeContents()}");
        UIManager.RequestPanelSet(UIPanelType.Storage, true);
    }

    // ── Eject (shared by the UI later and the test buttons now) ────

    // Re-spawns one stored instance of 'id' into the world. Mirrors
    // DeliveryBox.OpenWorldProp (kept separate by design — two short call sites
    // beat a premature shared spawner).
    public bool Eject(string id)
    {
        var inventory = Inventory;
        if (inventory == null) return false;

        var def = database != null ? database.GetById(id) : null;
        if (def == null || def.Prefab == null)
        {
            Debug.LogWarning($"[StorageContainer] Can't eject '{id}' — no def/prefab.");
            return false;
        }
        if (!inventory.RemoveWorldProp(id)) return false;

        var go     = Instantiate(def.Prefab, EjectAt.position, EjectAt.rotation);
        justEjectedId = go.GetInstanceID();
        StartCoroutine(ClearJustEjected());

        var marker = go.GetComponent<WorldPropInstance>();
        if (marker != null) marker.Configure(id);

        GameEvents.InventoryChanged(inventory);
        Debug.Log($"[StorageContainer] Ejected '{id}'.");
        return true;
    }

    private IEnumerator ClearJustEjected()
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        justEjectedId = -1;
    }

    private string DescribeContents()
    {
        var inv = Inventory;
        if (inv == null) return "no inventory";
        var stored = inv.WorldPropsStored;
        if (stored.Count == 0) return "empty";

        var sb = new StringBuilder($"{stored.Count} stored: ");
        for (int i = 0; i < stored.Count; i++) sb.Append(stored[i]).Append(i < stored.Count - 1 ? ", " : "");
        return sb.ToString();
    }

    // ── Odin test buttons (until the storage UI exists) ───────────

    [Button("List Contents")]
    private void ListContentsTest() => Debug.Log($"[StorageContainer] {DescribeContents()}");

    [Button("Eject by ID"), GUIColor(0.5f, 0.85f, 1f)]
    private void EjectByIdTest(string id)
    {
        if (!Application.isPlaying) { Debug.LogWarning("[StorageContainer] Enter Play mode to eject."); return; }
        Eject(id);
    }
}
}
