using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

[CreateAssetMenu(fileName = "ShopCatalog", menuName = "RunRunSimulator/Store/Shop Catalog")]
public class ShopCatalogSO : SerializedScriptableObject
{
    [Serializable]
    public class FurnitureListing
    {
        [Required, AssetsOnly, HideLabel, HorizontalGroup(220)]
        public FurnitureDefinitionSO Furniture;

        [HideLabel]
        public StoreShopData Shop;
    }

    [Serializable]
    public class ItemListing
    {
        [Required, AssetsOnly, HideLabel, HorizontalGroup(220)]
        public ItemDefinitionSO Item;

        [HideLabel]
        public StoreShopData Shop;
    }

    [Serializable]
    public class CreatureBoxListing
    {
        [Required, AssetsOnly, HideLabel, HorizontalGroup(220)]
        public CreatureBoxSO Box;

        [HideLabel]
        public StoreShopData Shop;
    }

    [Title("Restock schedule (applies to all listings in this shop)")]
    [Tooltip("Game days between restocks.")]
    [Min(1)] public int RestockEveryDays = 3;

    [Title("Discount schedule (applies to all listings in this shop)")]
    [Tooltip("Game days between discount windows. 0 = never on sale.")]
    [Min(0)] public int DiscountEveryDays = 7;

    [Button("Force Restock All (DEV)", ButtonSizes.Medium), GUIColor(0.9f, 0.75f, 0.2f)]
    private void DevForceRestock()
    {
        int day = GameClock.Instance != null ? GameClock.Instance.Day : 1;
        RestockAll(day);
        Debug.Log("[ShopCatalog] Force restock fired — all listings refilled to MaxStock.");
    }

    [Title("Furniture for sale")]
    [TableList(AlwaysExpanded = true)]
    [SerializeField] private List<FurnitureListing> furnitureListings = new List<FurnitureListing>();

    [Title("World props for sale")]
    [TableList(AlwaysExpanded = true)]
    [SerializeField] private List<ItemListing> itemListings = new List<ItemListing>();

    [Title("MoriMonchi boxes for sale")]
    [TableList(AlwaysExpanded = true)]
    [SerializeField] private List<CreatureBoxListing> creatureBoxListings = new List<CreatureBoxListing>();

    public IReadOnlyList<FurnitureListing>    FurnitureListings    => furnitureListings;
    public IReadOnlyList<ItemListing>         ItemListings         => itemListings;
    public IReadOnlyList<CreatureBoxListing>  CreatureBoxListings  => creatureBoxListings;

    public bool IsDiscountActive(int day) => DiscountEveryDays > 0 && day % DiscountEveryDays == 0;

    public int FinalPrice(StoreShopData shop, int day) =>
        shop?.FinalPrice(IsDiscountActive(day)) ?? 0;

    public bool NeedsRestock(int day) => lastRestockDay <= 0 || day - lastRestockDay >= RestockEveryDays;

    public void RestockAll(int day)
    {
        lastRestockDay = day;

        foreach (var l in furnitureListings)    l?.Shop?.Restock();
        foreach (var l in itemListings)         l?.Shop?.Restock();
        foreach (var l in creatureBoxListings)  l?.Shop?.Restock();
    }

    [NonSerialized] private int lastRestockDay;
}
}
