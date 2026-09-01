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

    [Title("Discount schedule (applies to all listings in this shop)")]
    [Tooltip("Weekdays the discount window is open. None / All = every day.")]
    public DiscountDay DiscountDays;

    [Tooltip("Months the discount window is open. None / All = every month.")]
    public DiscountMonth DiscountMonths;

    [Title("Restock schedule (applies to all listings in this shop)")]
    [Tooltip("Months in which the restock happens. None / All = every month.")]
    public DiscountMonth RestockMonths;

    [Tooltip("Which part of the month (days 1-10 / 11-20 / 21+) the restock fires.")]
    public RestockPeriod RestockPeriod;

    [Button("Force Restock All (DEV)", ButtonSizes.Medium), GUIColor(0.9f, 0.75f, 0.2f)]
    private void DevForceRestock()
    {
        lastRestockYear = 0;
        RestockAll(DateTime.Now);
        Debug.Log("[ShopCatalog] Force restock fired — all listings refilled to MaxStock.");
    }

    [Title("Furniture for sale")]
    [TableList(AlwaysExpanded = true)]
    [SerializeField] private List<FurnitureListing> furnitureListings = new List<FurnitureListing>();

    [Title("World props for sale")]
    [TableList(AlwaysExpanded = true)]
    [SerializeField] private List<ItemListing> itemListings = new List<ItemListing>();

    public IReadOnlyList<FurnitureListing> FurnitureListings => furnitureListings;
    public IReadOnlyList<ItemListing>      ItemListings      => itemListings;

    public bool IsDiscountActive(DateTime now)
    {
        var today = (DiscountDay)(1 << DayIndex(now.DayOfWeek));
        var month = (DiscountMonth)(1 << (now.Month - 1));

        bool dayOk   = DiscountDays   == DiscountDay.None   || (DiscountDays   & today) != 0;
        bool monthOk = DiscountMonths == DiscountMonth.None || (DiscountMonths & month) != 0;
        return dayOk && monthOk;
    }

    public int FinalPrice(StoreShopData shop, DateTime now) =>
        shop?.FinalPrice(IsDiscountActive(now)) ?? 0;

    private static int DayIndex(DayOfWeek d) => ((int)d + 6) % 7;

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

    [NonSerialized] private int           lastRestockYear;
    [NonSerialized] private int           lastRestockMonth;
    [NonSerialized] private RestockPeriod lastRestockPeriod;

    private static RestockPeriod PeriodOf(DateTime d) =>
        d.Day <= 10 ? RestockPeriod.EarlyMonth :
        d.Day <= 20 ? RestockPeriod.MidMonth   : RestockPeriod.EndOfMonth;
}
}
