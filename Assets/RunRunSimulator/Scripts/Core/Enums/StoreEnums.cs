namespace MoriMonchiSimulator
{
public enum FurnitureCategory
{
    Decoration = 0,
    Display    = 1,
    Functional = 2,
}

public enum ItemType
{
    Furniture = 0,
    WorldProp = 1,
}

public enum WorldPropCategory
{
    Tool     = 0,
    Food     = 1,
    Medicine = 2,
}

public enum BuyResult
{
    Success           = 0,
    OutOfStock        = 1,
    InsufficientFunds = 2,
    AlreadyOwned      = 3,
}

public enum Currency
{
    Dabloons = 0,
    Minerita = 1,
}

[System.Flags]
public enum StoreItemTypeFilter
{
    None      = 0,
    Furniture = 1 << 0,
    WorldProp = 1 << 1,
}

}
