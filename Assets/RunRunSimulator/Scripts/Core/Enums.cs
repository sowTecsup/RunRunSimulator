namespace MoriMonchiSimulator
{
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

// Visible life phase derived from age in days (CreatureLifeStageTableSO maps day
// thresholds → stage). Display-only: shown on the NameTag, never part of the DNA string.
public enum LifeStage
{
    Newborn = 0,
    Child   = 1,
    Teen    = 2,
    Adult   = 3,
    Elder   = 4
}

// Coat type of a MoriMochi. Maps 1:1 to a CartoonShader material in FurTypeDatabaseSO.
// Inherited 50/50 from one parent at breed time. Stored in CreatureDNA, NOT part of
// the genetic string (metadata like Gender/Personality). The material defines the look
// (outline/shadow sizing, gradients); the per-creature colors ride on a MaterialPropertyBlock.
public enum FurType
{
    Smooth = 0,  // Clean cartoon, no fuzz
    Fluffy = 1,  // Soft and puffy
    Spiky  = 2,  // Sharp tufts
    Shaggy = 3,  // Long unkempt coat
    Scaly  = 4,  // Reptilian plates
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
    Sold              = 3,
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
    Storage          = 5,  // Storage box: list of stored world props + eject
    Store            = 6,  // Shop screen: catalog of furniture + world props with prices
    Transaction      = 7,
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

// The two worlds an owned thing can live in, mirroring the inventory's split: furniture
// is placed only in build mode (grid-bound, "F#" ids in furnitureOwned), a world prop is
// a tangible object the player holds, uses and drops ("I#" ids in worldPropsStored). No
// single SO carries this anymore — furniture and world props have separate definitions
// and purchase flows; the enum stays as the canonical name for that namespace split.
public enum ItemType
{
    Furniture = 0,
    WorldProp = 1,
}

// Groups world props in the inventory / storage UI — the WorldProp analogue of
// FurnitureCategory. Extend as new tangible objects are added.
public enum WorldPropCategory
{
    Tool     = 0,  // escoba, regadera, bate — utilitarios / lanzables
    Food     = 1,  // alimentos que se aplican a los MoriMonchis
    Medicine = 2,  // curas / remedios
}

// Days a shop discount applies. Flags so a catalog can run an offer on any mix of
// weekdays (e.g. weekend sale = Saturday | Sunday).
//   None = unconfigured / no constraint (discount active every day).
//   All  = explicit "every day" (same runtime behaviour as None, but intent is clear).
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

// Months a shop discount (or restock) applies. Flags so catalogs can pin seasonal
// windows (e.g. holidays = November | December).
//   None = unconfigured / no constraint (applies every month).
//   All  = explicit "every month" (same runtime behaviour as None, intent is clear).
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

// When during the restock month the stock refills. The day ranges are:
//   EarlyMonth  = days  1–10
//   MidMonth    = days 11–20
//   EndOfMonth  = days 21–end
// Used together with DiscountMonth (which months) to pin the exact restock window.
public enum RestockPeriod
{
    EarlyMonth = 0,
    MidMonth   = 1,
    EndOfMonth = 2,
}

// Result of a purchase attempt. Returned by StoreManager.Buy* so the UI can show
// the right feedback without knowing the purchase internals.
public enum BuyResult
{
    Success           = 0,
    OutOfStock        = 1,  // CurrentStock == 0
    InsufficientFunds = 2,  // not enough Dabloons
    AlreadyOwned      = 3,  // furniture the player owns already (no-op, no charge)
}

// Forward-looking filter for the shop UI: a listing tags which kind(s) it counts as,
// so a future unified "filter by type" view can mask the catalog without re-deriving
// the type from which typed list the entry lives in. Distinct from ItemType (which
// classifies the data, not a filter) — this one is [Flags] so a filter can combine
// kinds. Extend with Consumable etc. as the catalog grows.
[System.Flags]
public enum StoreItemTypeFilter
{
    None      = 0,
    Furniture = 1 << 0,
    WorldProp = 1 << 1,
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

// Player-facing read of what a MoriMochi is trying to do RIGHT NOW — the verb the
// NameTag verbalizes above its head. Derived from the agent's internal AgentState
// (+ its active proximity reaction and the need it's covering), NOT a stored field.
// Distinct from AgentState (which carries physics phases the player doesn't care
// about) and from CreatureCondition (how it IS, not what it's DOING).
public enum CreatureIntent
{
    Idle        = 0,   // momentary pause between wanders
    Wandering   = 1,   // roaming with no special goal
    Following   = 2,   // following the player (reaction)
    Approaching = 3,   // walking up to investigate the player
    Fleeing     = 4,   // running away from the player
    Retreating  = 5,   // backing off a short distance
    SeekingFood = 6,   // heading to a Feeder (Health critical)
    SeekingRest = 7,   // heading to a RestZone (Energy critical)
    SeekingPlay = 8,   // heading to a PlayZone (Affect critical)
    Eating      = 9,   // using a Feeder
    Resting     = 10,  // using a RestZone
    Playing     = 11,  // using a PlayZone
    Held        = 12,  // in the player's hand
    Tumbling    = 13,  // thrown / recovering after a landing
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

public enum MMAnimationType
{
    Idle    = 0,
    Walk    = 1,
    Attack  = 2,
    Hit     = 3,
    Death   = 4,
    Victory = 5,
}

// The six tunable stats a StatModifier can target. Mirrors the base stat fields of
// CreatureDNA so equipment modifiers can address any of them generically.
public enum StatType
{
    Constitution = 0,
    Attack       = 1,
    Speed        = 2,
    Defense      = 3,
    Luck         = 4,
    Evasion      = 5,
}

// How a StatModifier stacks. Applied in order: all Flat, then PercentAdd (summed),
// then each PercentMult (compounded). Classic RPG modifier pipeline.
public enum ModifierType
{
    Flat        = 0,
    PercentAdd  = 1,
    PercentMult = 2,
}

// Typed equipment slots. A MoriMochi holds at most one item per slot; each
// EquipmentSO declares which slot it occupies. Extend as new gear kinds appear.
public enum EquipmentSlot
{
    Weapon = 0,
    Armor  = 1,
    Amulet = 2,
}
}
