namespace MoriMonchiSimulator
{
public enum WorldArea
{
    ShopFrontDesk = 0,
    ShopBackroom  = 1,
    Storage       = 2,
}

public enum PerceivableKind
{
    Player   = 0,
    Monchi   = 1,
    Customer = 2,
    Prop     = 3,
    Material = 4,
    Exit     = 5,
}

public enum ExpeditionTeam
{
    None   = 0,
    Player = 1,
    Rival  = 2,
}

public enum Occupation
{
    None    = 0,
    Gather  = 1,
    Guard   = 2,
    Break   = 3,
    Decoy   = 4,
    Explore = 5,
}

public static class ExpeditionTeams
{
    public static bool AreRivals(ExpeditionTeam a, ExpeditionTeam b) => a != ExpeditionTeam.None && b != ExpeditionTeam.None && a != b;
    public static bool AreAllies(ExpeditionTeam a, ExpeditionTeam b) => a != ExpeditionTeam.None && a == b;
}
}
