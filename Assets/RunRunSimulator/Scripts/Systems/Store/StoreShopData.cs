using System;
using Sirenix.OdinInspector;
using UnityEngine;

// The COMMERCIAL data of one shop listing: price, per-item discount percentage, stock
// counts and tags. Kept apart from the item/furniture definition so the same def can
// appear in multiple catalogs at different prices.
//
// WHEN a discount applies (which days / which months) is catalog-wide and lives on
// ShopCatalogSO — not here. A listing only carries HOW MUCH off (DiscountBase).
// The caller passes the catalog's `discountActive` flag when computing the final price.
//
// Class (not struct) so TryConsume() / Restock() mutate the actual instance in the
// catalog listing rather than a value copy.
[Serializable]
public class StoreShopData
{
    [MinValue(0)] public int BasePrice;

    [Title("Discount")]
    [Tooltip("Fraction off the base price when the catalog's discount window is active (0.2 = 20% off). 0 = this item never goes on sale.")]
    [Range(0f, 1f)] public float DiscountBase;

    [Title("Stock")]
    [Tooltip("Units currently available. Decremented on purchase, refilled on restock. Edit MaxStock instead.")]
    [ReadOnly, MinValue(-1)] public int CurrentStock;

    [Tooltip("Units refilled on restock. -1 = unlimited (CurrentStock never matters).")]
    [MinValue(-1)] public int MaxStock;

    [Title("Meta")]
    [Tooltip("Which kind(s) this listing counts as, for the shop's type filter.")]
    public StoreItemTypeFilter TypeFilter;

    [Tooltip("Free-form labels for the UI (\"nuevo\", \"oferta\", \"limitado\"…).")]
    public string[] Tags;

    // ── Price ─────────────────────────────────────────────────────

    // The price the player pays. Pass the catalog's IsDiscountActive result as the flag.
    public int FinalPrice(bool discountActive) =>
        discountActive && DiscountBase > 0f
            ? Mathf.RoundToInt(BasePrice * (1f - DiscountBase))
            : BasePrice;

    // ── Stock helpers ─────────────────────────────────────────────

    public bool IsUnlimited => MaxStock < 0;
    public bool InStock     => IsUnlimited || CurrentStock > 0;

    // Replenishes stock to MaxStock. Called by ShopCatalogSO.RestockAll().
    public void Restock() { if (!IsUnlimited) CurrentStock = MaxStock; }

    // Decrements stock by one. Returns false if out of stock.
    public bool TryConsume()
    {
        if (IsUnlimited)      return true;
        if (CurrentStock <= 0) return false;
        CurrentStock--;
        return true;
    }
}
