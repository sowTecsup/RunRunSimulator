using System.Collections;
using System.Text;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class StorageContainer : MonoBehaviour, IInteractable
{
    public static StorageContainer Instance { get; private set; }

    [Required, AssetsOnly] [SerializeField] private ItemDatabaseSO database;
    [Tooltip("Where ejected props re-appear. Defaults to this transform.")]
    [SerializeField] private Transform ejectPoint;

    private int justEjectedId = -1;

    private void Awake() => Instance = this;
    private void OnDestroy() { if (Instance == this) Instance = null; }

    private PlayerInventorySO Inventory => GameManager.CurrentInventory;

    private Transform EjectAt => ejectPoint != null ? ejectPoint : transform;

    private void OnTriggerEnter(Collider other)
    {
        var prop = other.GetComponentInParent<WorldPropInstance>();
        if (prop == null || prop.IsHeld || string.IsNullOrEmpty(prop.ItemId)) return;
        if (prop.gameObject.GetInstanceID() == justEjectedId) return;

        var inventory = Inventory;
        if (inventory == null) return;

        inventory.AddWorldProp(prop.ItemId);
        GameEvents.InventoryChanged(inventory);
        Debug.Log($"[StorageContainer] Stored '{prop.ItemId}'.");
        Destroy(prop.gameObject);
    }

    public void Interact()
    {
        Debug.Log($"[StorageContainer] Interact — {DescribeContents()}");
        UIManager.RequestPanelSet(UIPanelType.Storage, true);
    }

    public bool Eject(string id)
    {
        var inventory = Inventory;
        if (inventory == null) return false;

        var def = database != null ? database.GetByID(id) : null;
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
