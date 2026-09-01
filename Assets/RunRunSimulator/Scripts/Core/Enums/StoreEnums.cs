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

[System.Flags]
public enum DiscountDay
{
    None      = 0,
    Monday    = 1 << 0,
    Tuesday   = 1 << 1,
    Wednesday = 1 << 2,
    Thursday  = 1 << 3,
    Friday    = 1 << 4,
    Saturday  = 1 << 5,
    Sunday    = 1 << 6,
    All       = Monday | Tuesday | Wednesday | Thursday | Friday | Saturday | Sunday,
}

[System.Flags]
public enum DiscountMonth
{
    None      = 0,
    January   = 1 << 0,
    February  = 1 << 1,
    March     = 1 << 2,
    April     = 1 << 3,
    May       = 1 << 4,
    June      = 1 << 5,
    July      = 1 << 6,
    August    = 1 << 7,
    September = 1 << 8,
    October   = 1 << 9,
    November  = 1 << 10,
    December  = 1 << 11,
    All       = January | February | March | April | May | June | July |
                August  | September | October | November | December,
}

public enum RestockPeriod
{
    EarlyMonth = 0,
    MidMonth   = 1,
    EndOfMonth = 2,
}

public enum BuyResult
{
    Success           = 0,
    OutOfStock        = 1,
    InsufficientFunds = 2,
    AlreadyOwned      = 3,
}

[System.Flags]
public enum StoreItemTypeFilter
{
    None      = 0,
    Furniture = 1 << 0,
    WorldProp = 1 << 1,
}

public enum ItemTriggerKind
{
    None      = 0,
    LowHealth = 1,
    Collision = 2,
    Collected = 3,
}
}
