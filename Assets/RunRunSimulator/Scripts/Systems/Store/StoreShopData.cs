using System;
using Sirenix.OdinInspector;
using UnityEngine;
namespace MoriMonchiSimulator
{

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

    public int FinalPrice(bool discountActive) =>
        discountActive && DiscountBase > 0f
            ? Mathf.RoundToInt(BasePrice * (1f - DiscountBase))
            : BasePrice;

    public bool IsUnlimited => MaxStock < 0;
    public bool InStock     => IsUnlimited || CurrentStock > 0;

    public void Restock() { if (!IsUnlimited) CurrentStock = MaxStock; }

    public bool TryConsume()
    {
        if (IsUnlimited)      return true;
        if (CurrentStock <= 0) return false;
        CurrentStock--;
        return true;
    }
}
}
