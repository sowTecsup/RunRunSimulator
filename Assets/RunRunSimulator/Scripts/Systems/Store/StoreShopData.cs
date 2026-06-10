using System;
using Sirenix.OdinInspector;
using UnityEngine;

// The COMMERCIAL data of one shop listing — price, discount window, tags — kept apart
// from the item/furniture definition on purpose: a definition is neutral catalog data,
// while what it costs and when it's on sale belongs to the shop selling it. The same
// FurnitureDefinitionSO/ItemDefinitionSO can sit in several ShopCatalogSOs at different
// prices without any of this leaking into the definition. See ShopCatalogSO.
[Serializable]
public struct StoreShopData
{
    [MinValue(0)] public int BasePrice;

    [Title("Discount")]
    [Tooltip("Fraction off the base price while the discount is active (0.2 = 20% off).")]
    [Range(0f, 1f)] public float DiscountBase;

    [Tooltip("Weekdays the discount runs. None = every day (gated only by month).")]
    public DiscountDay DiscountDays;

    [Tooltip("Months the discount runs. None = every month (gated only by day).")]
    public DiscountMonth DiscountMonths;

    [Title("Stock")]
    [Tooltip("Units currently available. Decremented on purchase. -1 = unlimited.")]
    [MinValue(-1)] public int CurrentStock;

    [Tooltip("Units refilled on restock. -1 = unlimited (CurrentStock never matters).")]
    [MinValue(-1)] public int MaxStock;

    [Tooltip("Months in which the restock happens. None = every month.")]
    public DiscountMonth RestockMonths;

    [Tooltip("When during the restock month the stock refills (days 1-10 / 11-20 / 21+).")]
    public RestockPeriod RestockPeriod;

    [Title("Meta")]
    [Tooltip("Which kind(s) this listing counts as, for the shop's type filter.")]
    public StoreItemTypeFilter TypeFilter;

    [Tooltip("Free-form labels for the UI (\"nuevo\", \"oferta\", \"limitado\"…).")]
    public string[] Tags;

    // Active when a non-zero discount is set AND today falls inside both windows.
    // An empty (None) day/month flag means "no constraint on that axis", so a
    // permanent sale is DiscountBase > 0 with both flags left at None.
    public bool IsDiscountActive(DateTime now)
    {
        if (DiscountBase <= 0f) return false;

        var today = (DiscountDay)(1 << DayIndex(now.DayOfWeek));
        var month = (DiscountMonth)(1 << (now.Month - 1));

        bool dayOk   = DiscountDays   == DiscountDay.None   || (DiscountDays   & today) != 0;
        bool monthOk = DiscountMonths == DiscountMonth.None || (DiscountMonths & month) != 0;
        return dayOk && monthOk;
    }

    // The price the player actually pays right now (discount applied if active).
    public int FinalPrice(DateTime now) =>
        IsDiscountActive(now) ? Mathf.RoundToInt(BasePrice * (1f - DiscountBase)) : BasePrice;

    public int FinalPrice() => FinalPrice(DateTime.Now);

    // DayOfWeek is Sunday=0..Saturday=6; our flag order starts at Monday. Remap so
    // Monday→bit 0 … Sunday→bit 6, matching the DiscountDay layout.
    private static int DayIndex(DayOfWeek d) => ((int)d + 6) % 7;

    // ── Stock helpers ─────────────────────────────────────────────

    public bool IsUnlimited => MaxStock < 0;
    public bool InStock     => IsUnlimited || CurrentStock > 0;

    // True when today falls inside the restock window for this listing.
    // RestockMonths None = every month; the period maps day ranges:
    //   EarlyMonth 1-10 · MidMonth 11-20 · EndOfMonth 21+
    public bool IsRestockDay(DateTime now)
    {
        var month = (DiscountMonth)(1 << (now.Month - 1));
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

    // Replenishes stock to MaxStock. Call this when the restock date fires.
    // CurrentStock is runtime state on the SO — persistence to Cloud Save is a
    // later step (same PlayerData bucket as inventory + wallet).
    public void Restock() { if (!IsUnlimited) CurrentStock = MaxStock; }

    // Decrements stock by one. Returns false if out of stock (caller should block purchase).
    public bool TryConsume()
    {
        if (IsUnlimited) return true;
        if (CurrentStock <= 0) return false;
        CurrentStock--;
        return true;
    }
}
