public enum Rarity
{
    Common    = 0,
    Uncommon  = 1,
    Rare      = 2,
    Epic      = 3,
    Legendary = 4
}

// Thematic sets — group parts into coherent visual/lore collections.
// Future: full-set bonuses, themed packs, set-filtered matchmaking.
public enum PartSet
{
    None           = 0,  // No set / mixed origin
    GooGang        = 1,  // Slime, goop and everything sticky
    BogBrigade     = 2,  // Swamp dwellers, mossy, damp
    FuzzFactory    = 3,  // Fluffy, soft, dangerously cute
    CosmicCreeps   = 4,  // Interdimensional weirdos from beyond
    NeonNightmares = 5,  // 80s neon, loud colors, bad attitude
    CrunchCrew     = 6,  // Chitin, shells, too many legs
    GrimGlobs      = 7,  // Dark, gooey, vaguely menacing
    SpudSquad      = 8,  // Round, chunky, deceptively tough
    MoldMob        = 9,  // Fungal, spore-based, smells awful
    ZapZone        = 10  // Electric, always slightly sparking
}

public enum CreatureGender
{
    Unknown = 0,  // Wild-caught or generated, not yet determined
    Male    = 1,
    Female  = 2
}

// Identifies the anatomical slot a part occupies — used for thematic name generation.
public enum PartRole
{
    Body  = 0,
    Arm   = 1,
    Eye   = 2,
    Mouth = 3,
}

public enum Tier
{
    Tier1 = 1,
    Tier2 = 2,
    Tier3 = 3,
}
