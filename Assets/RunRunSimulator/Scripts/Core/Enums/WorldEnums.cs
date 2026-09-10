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

public enum ArenaCastMode
{
    Roster    = 0,
    LocalSave = 1,
}

public enum ArenaSite
{
    Center   = 0,
    NearVein = 1,
    FarVein  = 2,
}

public enum LootChoice
{
    Big   = 0,
    Small = 1,
}

public enum ContactChoice
{
    Flee  = 0,
    Fight = 1,
}

public enum PostureChoice
{
    Protect    = 0,
    Aggressive = 1,
}

public enum OrderPillar
{
    Loot    = 0,
    Contact = 1,
    Posture = 2,
}

public enum ArenaPaletteSlot
{
    Ground  = 0,
    Grass   = 1,
    Foliage = 2,
    Trunk   = 3,
    Rock    = 4,
    Wall    = 5,
    Water   = 6,
}

public static class ExpeditionTeams
{
    public static bool AreRivals(ExpeditionTeam a, ExpeditionTeam b) => a != ExpeditionTeam.None && b != ExpeditionTeam.None && a != b;
    public static bool AreAllies(ExpeditionTeam a, ExpeditionTeam b) => a != ExpeditionTeam.None && a == b;
}

public enum ArenaRegionKind
{
    Rock  = 0,
    Lake  = 1,
    Pit   = 2,
    Grove = 3,
}
}
