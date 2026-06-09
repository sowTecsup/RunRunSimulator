using Sirenix.OdinInspector;
using UnityEngine;

// The physical package an online order arrives in, dropped in front of the shop by
// StoreManager. Tapping E "opens" it: furniture goes straight into the inventory
// (it's placed later in build mode), a world prop spawns as a tangible object the
// player can grab — then the box is gone. The whole acquisition flow funnels through
// here so both item kinds share one delivery moment.
//
// Resolves the inventory via GameManager.Instance (project convention — gameplay
// scripts don't serialize cross-references to shared assets).
[RequireComponent(typeof(Collider))]
public class DeliveryBox : MonoBehaviour, IInteractable
{
    [Tooltip("What this package contains. Stamped by StoreManager at spawn.")]
    [SerializeField, ReadOnly] private ItemDefinitionSO item;

    // Called by StoreManager right after Instantiate to bind the order's contents.
    public void Configure(ItemDefinitionSO def) => item = def;

    public void Interact()
    {
        if (item == null)
        {
            Debug.LogWarning($"[DeliveryBox] '{name}' has no item configured — nothing to open.");
            return;
        }

        var inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
        if (inventory == null)
        {
            Debug.LogError("[DeliveryBox] No PlayerInventory available — cannot open.");
            return;
        }

        switch (item.Type)
        {
            case ItemType.Furniture:
                OpenFurniture(inventory);
                break;
            case ItemType.WorldProp:
                OpenWorldProp(inventory);
                break;
        }

        GameEvents.InventoryChanged(inventory);
        Destroy(gameObject);
    }

    // Furniture is intangible until placed: it just enters ownership (the build
    // browser surfaces it). Store the FurnitureDef's "F#" id, not this def's "I#".
    private void OpenFurniture(PlayerInventorySO inventory)
    {
        if (item.FurnitureDef == null)
        {
            Debug.LogError($"[DeliveryBox] '{item.Id}' is Furniture but has no FurnitureDef bridge.");
            return;
        }
        inventory.AddFurniture(item.FurnitureDef.Id);
        Debug.Log($"[DeliveryBox] Furniture '{item.FurnitureDef.Id}' added to inventory.");
    }

    // A world prop becomes a real object at the box's spot, tagged with its id so it
    // can later be stored or swept into the inventory on shutdown. NOT added to
    // worldPropsStored — it's loose in the world now, not in the storage box.
    private void OpenWorldProp(PlayerInventorySO inventory)
    {
        if (item.Prefab == null)
        {
            Debug.LogError($"[DeliveryBox] '{item.Id}' is WorldProp but has no Prefab.");
            return;
        }
        var go = Instantiate(item.Prefab, transform.position, transform.rotation);
        var marker = go.GetComponent<WorldPropInstance>();
        if (marker != null) marker.Configure(item.Id);
        else Debug.LogWarning($"[DeliveryBox] Spawned '{item.Id}' prefab has no WorldPropInstance — it can't be stored or persisted.");
        Debug.Log($"[DeliveryBox] World prop '{item.Id}' spawned at delivery point.");
    }
}
