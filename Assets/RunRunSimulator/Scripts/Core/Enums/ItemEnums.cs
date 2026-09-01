namespace MoriMonchiSimulator
{
public enum StatType
{
    Constitution = 0,
    Attack       = 1,
    Speed        = 2,
    Defense      = 3,
    Luck         = 4,
    Evasion      = 5,
}

public enum ModifierType
{
    Flat        = 0,
    PercentAdd  = 1,
    PercentMult = 2,
}

public enum EquipmentSlot
{
    Weapon = 0,
    Armor  = 1,
    Amulet = 2,
}
}
