using System;
using Sirenix.OdinInspector;
using UnityEngine;

// A storefront in the world: holds the shop's catalog (ShopCatalogSO) and runs the two
// purchase flows. The StorePanel UITK reads Catalog to list what's for sale and calls
// these Buy methods on confirm.
//
// Purchase rules (enforced here, not in the UI):
//   1. Stock check   — out of stock → BuyResult.OutOfStock
//   2. AlreadyOwned  — furniture only: player already owns it → BuyResult.AlreadyOwned
//   3. Funds check   — not enough Dabloons → BuyResult.InsufficientFunds
//   4. Deduct funds + consume stock + add to inventory → BuyResult.Success
//
// Flows differ by kind:
//   Furniture  → straight into inventory (furnitureOwned, "F#"). Placed later in build mode.
//   World prop → DeliveryBox drops at deliverySpawnPoint; opening it spawns the prop.
public class StoreManager : MonoBehaviour
{
    [Title("Catalog")]
    [Required, AssetsOnly] [SerializeField] private ShopCatalogSO catalog;

    [Title("World-prop delivery")]
    [Required, AssetsOnly] [SerializeField] private DeliveryBox deliveryBoxPrefab;
    [Required]             [SerializeField] private Transform   deliverySpawnPoint;

    public ShopCatalogSO Catalog => catalog;

    // ── Purchase API (called by StorePanelUITK) ───────────────────

    public BuyResult BuyFurniture(FurnitureDefinitionSO def, StoreShopData shop)
    {
        if (def == null || shop == null) { Debug.LogWarning("[StoreManager] BuyFurniture: null arg."); return BuyResult.OutOfStock; }

        if (!shop.InStock) return BuyResult.OutOfStock;

        var inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
        if (inventory == null) { Debug.LogError("[StoreManager] No PlayerInventory available."); return BuyResult.OutOfStock; }

        if (inventory.HasFurniture(def.Id)) return BuyResult.AlreadyOwned;

        var now   = GameManager.Instance != null ? GameManager.Instance.ServerNow : DateTime.Now;
        int price = catalog.FinalPrice(shop, now);
        if (price > 0 && !inventory.SpendDabloons(price)) return BuyResult.InsufficientFunds;

        shop.TryConsume();
        inventory.AddFurniture(def.Id);
        GameEvents.InventoryChanged(inventory);
        Debug.Log($"[StoreManager] Bought furniture '{def.DisplayName}' ({def.Id}) for {price} Dabloons.");
        return BuyResult.Success;
    }

    public BuyResult BuyWorldProp(ItemDefinitionSO def, StoreShopData shop)
    {
        if (def == null || shop == null) { Debug.LogWarning("[StoreManager] BuyWorldProp: null arg."); return BuyResult.OutOfStock; }

        if (!shop.InStock) return BuyResult.OutOfStock;

        if (deliveryBoxPrefab == null || deliverySpawnPoint == null)
        {
            Debug.LogError("[StoreManager] Assign deliveryBoxPrefab + deliverySpawnPoint first.");
            return BuyResult.OutOfStock;
        }
        if (!Application.isPlaying)
        {
            Debug.LogWarning("[StoreManager] Enter Play mode to buy (spawns a DeliveryBox).");
            return BuyResult.OutOfStock;
        }

        var inventory = GameManager.Instance != null ? GameManager.Instance.Inventory : null;
        if (inventory == null) { Debug.LogError("[StoreManager] No PlayerInventory available."); return BuyResult.OutOfStock; }

        var now   = GameManager.Instance != null ? GameManager.Instance.ServerNow : DateTime.Now;
        int price = catalog.FinalPrice(shop, now);
        if (price > 0 && !inventory.SpendDabloons(price)) return BuyResult.InsufficientFunds;

        shop.TryConsume();

        var go  = Instantiate(deliveryBoxPrefab, deliverySpawnPoint.position, deliverySpawnPoint.rotation);
        var box = go.GetComponent<DeliveryBox>();
        if (box == null)
        {
            Debug.LogError("[StoreManager] deliveryBoxPrefab has no DeliveryBox component.");
            Destroy(go);
            // Refund — box never spawned.
            if (price > 0) { inventory.AddDabloons(price); shop.CurrentStock++; }
            return BuyResult.OutOfStock;
        }
        box.Configure(def);
        GameEvents.InventoryChanged(inventory);
        Debug.Log($"[StoreManager] Ordered '{def.DisplayName}' ({def.Id}) for {price} Dabloons.");
        return BuyResult.Success;
    }

    // Called by StorePanelUITK when the panel opens. Restocks all listings if
    // today is the shop's restock day and it hasn't fired yet this period.
    public void RestockIfNeeded()
    {
        if (catalog == null) return;
        var now = GameManager.Instance != null ? GameManager.Instance.ServerNow : DateTime.Now;
        if (catalog.NeedsRestock(now)) catalog.RestockAll(now);
    }
}
