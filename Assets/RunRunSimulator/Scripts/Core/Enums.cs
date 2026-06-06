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

// Tracks WHY a MoriMochi is currently unavailable for new actions.
// Extend with new reasons (e.g. Breeding) as new timed systems are added.
public enum BusyReason
{
    None              = 0,
    QueuedForCombat   = 1,  // Waiting for async matchmaking
    Breeding          = 2,  // Occupied in a timed breeding process (future)
}

// Identifies each Canvas panel the player can open via world interaction.
// UIManager maps these to the actual scene panel GameObjects. Add one entry
// per panel as new screens are built.
public enum UIPanelType
{
    None             = 0,  // Unconfigured — toggling it is a no-op
    CreatureGrid     = 1,  // The in-game MoriMonchis grid (CreatureGridUI)
    MorimonchiDetail = 2,  // Detailed summary window for one MoriMochi
    Breeding         = 3,  // Breeding screen: parent selection + incubating eggs
    Combat           = 4,  // Combat screen: online battle + local combat + results
}

// What the player is currently doing. Drives input/cursor/camera: while a UI
// panel is open the player switches to Menu (no movement, cursor free, camera
// frozen) and back to Exploring when it closes.
public enum PlayerStateType
{
    None      = 0,  // Uninitialized
    Exploring = 1,  // Normal first-person control
    Menu      = 2,  // A UI panel is focused — player control suspended
    Building  = 3,  // Construction mode — places/removes furniture (Building action map)
}

// Groups furniture in the shop catalog. Extend as new kinds are added.
public enum FurnitureCategory
{
    Decoration = 0,
    Display    = 1,
    Functional = 2,
}

// The three needs a MoriMochi satisfies at world NeedStations (Feeder / RestZone / PlayZone),
// mapped 1:1 to the fields in NeedsState.
public enum NeedType
{
    Health = 0,  // hunger / wellbeing — Feeder
    Energy = 1,  // rest               — RestZone
    Affect = 2,  // affection / anti-stress — PlayZone
}

// Overall wellbeing of a MoriMochi, DERIVED from its needs (never stored — see
// MoriMochiAgent.Condition). Orthogonal to AgentState (what it's DOING): this is HOW IT IS.
// Drives whether it can afford to react to the player and is a clean hook for UI/icons.
// Severity order: a creature prioritizes its needs over everything once it leaves Healthy.
public enum CreatureCondition
{
    Healthy = 0,  // No need is critical — free to roam and react to the player
    InNeed  = 1,  // Energy or Affect critical — busy meeting that need, ignores the player
    Sick    = 2,  // Health critical — the survival emergency (permadeath precursor)
}

// Behavioral archetype of a MoriMochi. Assigned RANDOMLY at mint/hatch (NOT
// inherited, NOT part of the genetic string — it's metadata like Gender) and
// stored in CreatureDNA. Drives world movement and how it reacts to the player.
// A PersonalityProfileSO maps each value to concrete tuning. Reserved hook for
// future relevance (combat bias, breeding affinity) — read the profile, don't
// scatter switch statements.
public enum Personality
{
    Skittish   = 0,  // Asustadizo — fast nervous bursts, flees, hides in Storage
    Aggressive = 1,  // Agresivo  — territorial, approaches, lives in the Front Desk bustle
    Lazy       = 2,  // Perezoso  — barely moves, idles a lot, neutral Backroom
    Curious    = 3,  // Curioso   — wanders wide, approaches the player and objects
    Social     = 4,  // Sociable  — seeks company, follows the player, Front Desk
    Grumpy     = 5,  // Gruñón    — solitary, keeps its distance, retreats to Storage
}

// How a MoriMochi reacts when the player enters its proximity radius. The reaction
// interrupts its default behavior and it returns to it once the player leaves.
public enum ProximityReaction
{
    Ignore   = 0,  // Keeps doing its thing
    Flee     = 1,  // Runs to a point away from the player
    Approach = 2,  // Walks toward the player and stops at a distance (investigates)
    Follow   = 3,  // Keeps following the player while in range
    Retreat  = 4,  // Backs off a short distance, then resumes
}

// Logical sectors of the shop, mapped to baked NavMesh Areas of the same name.
// An agent confined to an area only walks on polygons of that area type, so the
// confinement is real (pathfinding never leaves it), not a soft collider check.
// The names below MUST match the NavMesh Area names configured in Unity's
// Navigation window: "ShopFrontDesk", "ShopBackroom", "Storage".
public enum WorldArea
{
    ShopFrontDesk = 0,  // High-traffic counter — Social / Curious / Aggressive
    ShopBackroom  = 1,  // Neutral middle ground — Lazy
    Storage       = 2,  // Out-of-the-way corner — Skittish / Grumpy
}

// Result of a single combat from ONE creature's point of view. Stored per fight
// in CreatureDNA.CombatHistory.
public enum CombatOutcome
{
    Won  = 0,
    Lost = 1,
    Draw = 2,
}
