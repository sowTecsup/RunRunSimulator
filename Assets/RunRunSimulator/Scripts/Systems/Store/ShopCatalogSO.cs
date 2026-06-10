using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

// One shop's catalog: the things THIS store sells and what each costs. A store in the
// world points its StoreManager at one of these — three stores = three assets, each
// with its own prices and offers. The catalog only PAIRS an existing definition with
// commercial data (StoreShopData); it never owns the item/furniture data itself, which
// stays in FurnitureDatabaseSO / ItemDatabaseSO. That keeps pricing decoupled from the
// catalog so the same item can be sold elsewhere at another price.
//
// Two typed lists (not one polymorphic collection): furniture and world props are
// genuinely different kinds with different ids ("F#" vs "I#") and different purchase
// flows, so each listing knows exactly what it holds. The StorePanel derives its tabs
// from these (Furniture / WorldProps / Consumables-by-WorldPropCategory).
[CreateAssetMenu(fileName = "ShopCatalog", menuName = "RunRunSimulator/Shop Catalog")]
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

    [Title("Furniture for sale")]
    [TableList(AlwaysExpanded = true)]
    [SerializeField] private List<FurnitureListing> furnitureListings = new List<FurnitureListing>();

    [Title("World props for sale")]
    [TableList(AlwaysExpanded = true)]
    [SerializeField] private List<ItemListing> itemListings = new List<ItemListing>();

    public IReadOnlyList<FurnitureListing> FurnitureListings => furnitureListings;
    public IReadOnlyList<ItemListing> ItemListings => itemListings;
}
