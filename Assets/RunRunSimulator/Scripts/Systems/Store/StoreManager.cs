using Sirenix.OdinInspector;
using UnityEngine;

// A storefront in the world: holds the shop's catalog (a ShopCatalogSO) and runs the
// two purchase flows. The StorePanel UITK reads Catalog to list what's for sale and
// calls these Buy methods on confirm. There's no wallet yet — buying is free; price is
// display-only for now (economy enforcement is a later stage). The flows differ by kind:
//
//  • Furniture  → straight into the inventory (furnitureOwned, "F#"). It's intangible
//                 until placed in build mode, so no physical delivery.
//  • World prop → a DeliveryBox drops at deliverySpawnPoint; opening it spawns the prop.
//
// Resolves the inventory via GameManager.Instance (project convention — gameplay
// scripts don't serialize cross-references to shared assets).
public class StoreManager : MonoBehaviour
{
    [Title("Catalog")]
    [Required, AssetsOnly] [SerializeField] private ShopCatalogSO catalog;

    [Title("World-prop delivery")]
    [Required, AssetsOnly] [SerializeField] private DeliveryBox deliveryBoxPrefab;
    [Required]             [SerializeField] private Transform deliverySpawnPoint;

    public ShopCatalogSO Catalog => catalog;

    // ── Purchase API (called by StorePanelUITK) ───────────────────

    // Furniture: ownership is a set, so buying a piece you already own is a no-op
    // (returns false). Fires InventoryChanged → GameManager persists.
    public bool BuyFurniture(FurnitureDefinitionSO def)
    {
        if (def == null) { Debug.LogWarning("[StoreManager] BuyFurniture: null definition."); return false; }

        var inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
        if (inventory == null) { Debug.LogError("[StoreManager] No PlayerInventory available."); return false; }

        bool added = inventory.AddFurniture(def.Id);
        if (added)
        {
            GameEvents.InventoryChanged(inventory);
            Debug.Log($"[StoreManager] Bought furniture '{def.DisplayName}' ({def.Id}).");
        }
        else
        {
            Debug.Log($"[StoreManager] Furniture '{def.Id}' already owned — nothing added.");
        }
        return added;
    }

    // World prop: drops a DeliveryBox the player opens to receive the physical object.
    public bool BuyWorldProp(ItemDefinitionSO def)
    {
        if (def == null) { Debug.LogWarning("[StoreManager] BuyWorldProp: null definition."); return false; }
        if (deliveryBoxPrefab == null || deliverySpawnPoint == null)
        {
            Debug.LogError("[StoreManager] Assign deliveryBoxPrefab + deliverySpawnPoint first.");
            return false;
        }
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[StoreManager] Enter Play mode to buy (spawns a DeliveryBox).");
            return false;
        }

        var go  = Instantiate(deliveryBoxPrefab, deliverySpawnPoint.position, deliverySpawnPoint.rotation);
        var box = go.GetComponent<DeliveryBox>();
        if (box == null)
        {
            Debug.LogError("[StoreManager] deliveryBoxPrefab has no DeliveryBox component.");
            Destroy(go);
            return false;
        }
        box.Configure(def);
        Debug.Log($"[StoreManager] Ordered world prop '{def.DisplayName}' ({def.Id}).");
        return true;
    }
}
