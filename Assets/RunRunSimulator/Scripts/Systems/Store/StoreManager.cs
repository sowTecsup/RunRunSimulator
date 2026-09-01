using System;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

public class StoreManager : MonoBehaviour
{
    [Title("Catalog")]
    [Required, AssetsOnly] [SerializeField] private ShopCatalogSO catalog;

    [Title("World-prop delivery")]
    [Required, AssetsOnly] [SerializeField] private DeliveryBox deliveryBoxPrefab;
    [Required]             [SerializeField] private Transform   deliverySpawnPoint;

    public ShopCatalogSO Catalog => catalog;

    public BuyResult BuyFurniture(FurnitureDefinitionSO def, StoreShopData shop)
    {
        if (def == null || shop == null) { Debug.LogWarning("[StoreManager] BuyFurniture: null arg."); return BuyResult.OutOfStock; }

        if (!shop.InStock) return BuyResult.OutOfStock;

        var inventory = GameManager.CurrentInventory;
        if (inventory == null) { Debug.LogError("[StoreManager] No PlayerInventory available."); return BuyResult.OutOfStock; }

        if (inventory.HasFurniture(def.Id)) return BuyResult.AlreadyOwned;

        var now   = GameManager.Now;
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

        var inventory = GameManager.CurrentInventory;
        if (inventory == null) { Debug.LogError("[StoreManager] No PlayerInventory available."); return BuyResult.OutOfStock; }

        var now   = GameManager.Now;
        int price = catalog.FinalPrice(shop, now);
        if (price > 0 && !inventory.SpendDabloons(price)) return BuyResult.InsufficientFunds;

        shop.TryConsume();

        var go  = Instantiate(deliveryBoxPrefab, deliverySpawnPoint.position, deliverySpawnPoint.rotation);
        var box = go.GetComponent<DeliveryBox>();
        if (box == null)
        {
            Debug.LogError("[StoreManager] deliveryBoxPrefab has no DeliveryBox component.");
            Destroy(go);
            if (price > 0) { inventory.AddDabloons(price); shop.CurrentStock++; }
            return BuyResult.OutOfStock;
        }
        box.Configure(def);
        GameEvents.InventoryChanged(inventory);
        Debug.Log($"[StoreManager] Ordered '{def.DisplayName}' ({def.Id}) for {price} Dabloons.");
        return BuyResult.Success;
    }

    public void RestockIfNeeded()
    {
        if (catalog == null) return;
        var now = GameManager.Now;
        if (catalog.NeedsRestock(now)) catalog.RestockAll(now);
    }
}
}
