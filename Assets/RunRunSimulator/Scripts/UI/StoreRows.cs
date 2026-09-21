using System;
using System.Collections.Generic;
namespace MoriMonchiSimulator
{

public static class StoreRows
{
    public enum Tab { Furniture, WorldProps, Consumables, Creatures }

    public struct Row
    {
        public string          Name;
        public StoreShopData   Shop;
        public Func<BuyResult> Buy;
    }

    public static string TabLabel(Tab tab) => tab switch
    {
        Tab.Furniture   => Loc.Tr("ui.store.tab.furniture"),
        Tab.WorldProps  => Loc.Tr("ui.store.tab.worldprops"),
        Tab.Consumables => Loc.Tr("ui.store.tab.consumables"),
        Tab.Creatures   => Loc.Tr("ui.store.tab.creatures"),
        _               => "",
    };

    public static void Collect(Tab tab, ShopCatalogSO catalog, StoreManager store, List<Row> rows)
    {
        if (catalog == null) return;

        if (tab == Tab.Furniture)
        {
            foreach (var listing in catalog.FurnitureListings)
            {
                var def = listing?.Furniture;
                if (def == null) continue;
                var capturedDef  = def;
                var capturedShop = listing.Shop;
                rows.Add(new Row
                {
                    Name = NameOf(def.DisplayName, def.Id),
                    Shop = capturedShop,
                    Buy  = () => store.BuyFurniture(capturedDef, capturedShop),
                });
            }
            return;
        }

        if (tab == Tab.Creatures)
        {
            foreach (var listing in catalog.CreatureBoxListings)
            {
                var box = listing?.Box;
                if (box == null) continue;
                var capturedBox  = box;
                var capturedShop = listing.Shop;
                rows.Add(new Row
                {
                    Name = NameOf(box.DisplayName, box.Id),
                    Shop = capturedShop,
                    Buy  = () => store.BuyCreatureBox(capturedBox, capturedShop),
                });
            }
            return;
        }

        foreach (var listing in catalog.ItemListings)
        {
            var def = listing?.Item;
            if (def == null) continue;
            if (!MatchesItemTab(tab, def.Category)) continue;
            var capturedDef  = def;
            var capturedShop = listing.Shop;
            rows.Add(new Row
            {
                Name = NameOf(def.DisplayName, def.Id),
                Shop = capturedShop,
                Buy  = () => store.BuyWorldProp(capturedDef, capturedShop),
            });
        }
    }

    private static bool MatchesItemTab(Tab tab, WorldPropCategory cat) =>
        tab == Tab.WorldProps
            ? cat == WorldPropCategory.Tool
            : cat == WorldPropCategory.Food || cat == WorldPropCategory.Medicine;

    private static string NameOf(string display, string id) =>
        string.IsNullOrEmpty(display) ? id : display;
}
}
