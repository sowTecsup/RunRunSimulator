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

    private static int Today => GameClock.Instance != null ? GameClock.Instance.Day : 1;

    private void OnEnable()  => GameEvents.OnDayStarted += OnDayStarted;
    private void OnDisable() => GameEvents.OnDayStarted -= OnDayStarted;

    private void OnDayStarted(int day) => RestockIfNeeded();

    public BuyResult BuyFurniture(FurnitureDefinitionSO def, StoreShopData shop)
    {
        if (def == null || shop == null) { Debug.LogWarning("[StoreManager] BuyFurniture: null arg."); return BuyResult.OutOfStock; }

        if (!shop.InStock) return BuyResult.OutOfStock;

        var inventory = GameManager.CurrentInventory;
        if (inventory == null) { Debug.LogError("[StoreManager] No PlayerInventory available."); return BuyResult.OutOfStock; }

        if (inventory.HasFurniture(def.Id)) return BuyResult.AlreadyOwned;

        int price = catalog.FinalPrice(shop, Today);
        if (price > 0 && Wallet.Balance(Currency.Dabloons) < price) return BuyResult.InsufficientFunds;

        shop.TryConsume();
        inventory.AddFurniture(def.Id);
        if (price > 0) Wallet.TrySpend(Currency.Dabloons, price, "store");
        else           GameEvents.InventoryChanged(inventory);
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

        int price = catalog.FinalPrice(shop, Today);
        if (price > 0 && !Wallet.TrySpend(Currency.Dabloons, price, "store")) return BuyResult.InsufficientFunds;

        shop.TryConsume();

        var box = SpawnDeliveryBox(price, shop);
        if (box == null) return BuyResult.OutOfStock;

        box.Configure(def);
        if (price <= 0) GameEvents.InventoryChanged(inventory);
        Debug.Log($"[StoreManager] Ordered '{def.DisplayName}' ({def.Id}) for {price} Dabloons.");
        return BuyResult.Success;
    }

    public BuyResult BuyCreatureBox(CreatureBoxSO box, StoreShopData shop)
    {
        if (box == null || shop == null) { Debug.LogWarning("[StoreManager] BuyCreatureBox: null arg."); return BuyResult.OutOfStock; }

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

        int price = catalog.FinalPrice(shop, Today);
        if (price > 0 && !Wallet.TrySpend(Currency.Dabloons, price, "store")) return BuyResult.InsufficientFunds;

        shop.TryConsume();

        var deliveryBox = SpawnDeliveryBox(price, shop);
        if (deliveryBox == null) return BuyResult.OutOfStock;

        deliveryBox.Configure(box);
        if (price <= 0) GameEvents.InventoryChanged(inventory);
        Debug.Log($"[StoreManager] Ordered creature box '{box.DisplayName}' ({box.Id}) for {price} Dabloons.");
        return BuyResult.Success;
    }

    private DeliveryBox SpawnDeliveryBox(int price, StoreShopData shop)
    {
        var go  = Instantiate(deliveryBoxPrefab, deliverySpawnPoint.position, deliverySpawnPoint.rotation);
        var box = go.GetComponent<DeliveryBox>();
        if (box == null)
        {
            Debug.LogError("[StoreManager] deliveryBoxPrefab has no DeliveryBox component.");
            Destroy(go);
            if (price > 0) { Wallet.Add(Currency.Dabloons, price, "store-refund"); shop.CurrentStock++; }
            return null;
        }
        return box;
    }

    public void RestockIfNeeded()
    {
        if (catalog == null) return;
        int day = Today;
        if (catalog.NeedsRestock(day)) catalog.RestockAll(day);
    }
}
}
