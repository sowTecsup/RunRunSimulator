using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

// One shop's catalog: the things THIS store sells and what each costs. Three stores = three
// assets, each with its own prices and schedules. The catalog owns two temporal schedules:
//
//   Discount schedule — WHEN items are on sale (which days + which months). Each listing
//     still carries its own DiscountBase (how much off). If a listing has DiscountBase = 0
//     it never goes on sale even when the shop's discount window is open.
//
//   Restock schedule — WHEN stock is refilled (same DiscountMonth + RestockPeriod pattern).
//     Every listing restocks on the same catalog-wide date.
//
// Having both schedules at the catalog level means a designer configures "this shop runs
// weekend deals in winter" in one place, not per item.
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

    // ── Discount schedule ─────────────────────────────────────────

    [Title("Discount schedule (applies to all listings in this shop)")]
    [Tooltip("Weekdays the discount window is open. None / All = every day.")]
    public DiscountDay DiscountDays;

    [Tooltip("Months the discount window is open. None / All = every month.")]
    public DiscountMonth DiscountMonths;

    // ── Restock schedule ──────────────────────────────────────────

    [Title("Restock schedule (applies to all listings in this shop)")]
    [Tooltip("Months in which the restock happens. None / All = every month.")]
    public DiscountMonth RestockMonths;

    [Tooltip("Which part of the month (days 1-10 / 11-20 / 21+) the restock fires.")]
    public RestockPeriod RestockPeriod;

    [Button("Force Restock All (DEV)", ButtonSizes.Medium), GUIColor(0.9f, 0.75f, 0.2f)]
    private void DevForceRestock()
    {
        // Reset period tracking so the guard in NeedsRestock doesn't block it,
        // then restock using the current date (server time not available from SO context).
        lastRestockYear = 0;
        RestockAll(DateTime.Now);
        Debug.Log("[ShopCatalog] Force restock fired — all listings refilled to MaxStock.");
    }

    // ── Listings ──────────────────────────────────────────────────

    [Title("Furniture for sale")]
    [TableList(AlwaysExpanded = true)]
    [SerializeField] private List<FurnitureListing> furnitureListings = new List<FurnitureListing>();

    [Title("World props for sale")]
    [TableList(AlwaysExpanded = true)]
    [SerializeField] private List<ItemListing> itemListings = new List<ItemListing>();

    public IReadOnlyList<FurnitureListing> FurnitureListings => furnitureListings;
    public IReadOnlyList<ItemListing>      ItemListings      => itemListings;

    // ── Discount helpers ──────────────────────────────────────────

    // True when today falls inside this shop's discount window.
    // None / All on either axis = no constraint on that axis (always passes).
    public bool IsDiscountActive(DateTime now)
    {
        var today = (DiscountDay)(1 << DayIndex(now.DayOfWeek));
        var month = (DiscountMonth)(1 << (now.Month - 1));

        bool dayOk   = DiscountDays   == DiscountDay.None   || (DiscountDays   & today) != 0;
        bool monthOk = DiscountMonths == DiscountMonth.None || (DiscountMonths & month) != 0;
        return dayOk && monthOk;
    }

    // Final price for a listing, combining this shop's discount window with the item's rate.
    public int FinalPrice(StoreShopData shop, DateTime now) =>
        shop?.FinalPrice(IsDiscountActive(now)) ?? 0;

    private static int DayIndex(DayOfWeek d) => ((int)d + 6) % 7;

    // ── Restock ───────────────────────────────────────────────────

    public bool IsRestockDay(DateTime now)
    {
        var month    = (DiscountMonth)(1 << (now.Month - 1));
        bool monthOk = RestockMonths == DiscountMonth.None || (RestockMonths & month) != 0;
        if (!monthOk) return false;

        return RestockPeriod switch
        {
            RestockPeriod.EarlyMonth => now.Day <= 10,
            RestockPeriod.MidMonth   => now.Day >= 11 && now.Day <= 20,
            RestockPeriod.EndOfMonth => now.Day >= 21,
            _                        => false,
        };
    }

    public bool NeedsRestock(DateTime now)
    {
        if (!IsRestockDay(now)) return false;
        return lastRestockYear   != now.Year  ||
               lastRestockMonth  != now.Month ||
               lastRestockPeriod != PeriodOf(now);
    }

    public void RestockAll(DateTime now)
    {
        lastRestockYear   = now.Year;
        lastRestockMonth  = now.Month;
        lastRestockPeriod = PeriodOf(now);

        foreach (var l in furnitureListings) l?.Shop?.Restock();
        foreach (var l in itemListings)      l?.Shop?.Restock();
    }

    // ── Private ───────────────────────────────────────────────────

    [NonSerialized] private int           lastRestockYear;
    [NonSerialized] private int           lastRestockMonth;
    [NonSerialized] private RestockPeriod lastRestockPeriod;

    private static RestockPeriod PeriodOf(DateTime d) =>
        d.Day <= 10 ? RestockPeriod.EarlyMonth :
        d.Day <= 20 ? RestockPeriod.MidMonth   : RestockPeriod.EndOfMonth;
}
