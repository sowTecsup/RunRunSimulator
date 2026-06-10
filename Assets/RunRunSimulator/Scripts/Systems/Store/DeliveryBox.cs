using Sirenix.OdinInspector;
using UnityEngine;

// The physical package a world-prop order arrives in, dropped in front of the shop by
// StoreManager. Tapping E "opens" it: the prop spawns as a tangible object the player
// can grab — then the box is gone. Furniture does NOT come through here: it's bought
// straight into the inventory (placed later in build mode), so only world props need
// this delivery moment.
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
        if (item.Prefab == null)
        {
            Debug.LogError($"[DeliveryBox] '{item.Id}' has no Prefab — nothing to spawn.");
            return;
        }

        // A world prop becomes a real object at the box's spot, tagged with its id so it
        // can later be stored or swept into the inventory on shutdown. NOT added to
        // worldPropsStored — it's loose in the world now, not in the storage box, so the
        // inventory is unchanged and we fire no InventoryChanged (nothing to persist).
        var go = Instantiate(item.Prefab, transform.position, transform.rotation);
        var marker = go.GetComponent<WorldPropInstance>();
        if (marker != null) marker.Configure(item.Id);
        else Debug.LogWarning($"[DeliveryBox] Spawned '{item.Id}' prefab has no WorldPropInstance — it can't be stored or persisted.");
        Debug.Log($"[DeliveryBox] World prop '{item.Id}' spawned at delivery point.");

        Destroy(gameObject);
    }
}
