namespace MoriMonchiSimulator
{
public enum CreatureGender
{
    Unknown = 0,
    Male    = 1,
    Female  = 2
}

public enum LifeStage
{
    Newborn = 0,
    Child   = 1,
    Teen    = 2,
    Adult   = 3,
    Elder   = 4
}

public enum MonchiMood
{
    Neutral    = 0,
    Feliz      = 1,
    Triste     = 2,
    Dolor      = 3,
    Enojado    = 4,
    Dormido    = 5,
    Enfermo    = 6,
    Mareado    = 7,
    Asustado   = 8,
    Amoroso    = 9,
    Emocionado = 10,
    KO         = 11,
}

public enum Tier
{
    Tier1 = 1,
    Tier2 = 2,
    Tier3 = 3,
}

public enum BusyReason
{
    None              = 0,
    Breeding          = 2,
    Sold              = 3,
}

public enum NeedType
{
    Health = 0,
    Energy = 1,
    Affect = 2,
}

public enum CreatureCondition
{
    Healthy = 0,
    InNeed  = 1,
    Sick    = 2,
}

public enum CreatureIntent
{
    Idle        = 0,
    Wandering   = 1,
    Following   = 2,
    Approaching = 3,
    Fleeing     = 4,
    Retreating  = 5,
    SeekingFood = 6,
    SeekingRest = 7,
    SeekingPlay = 8,
    Eating      = 9,
    Resting     = 10,
    Playing     = 11,
    Held        = 12,
    Tumbling    = 13,
    Socializing = 14,
    Chasing     = 15,
    SleepingTogether = 16,
    Fighting    = 17,
}

public enum ProximityReaction
{
    Ignore   = 0,
    Flee     = 1,
    Approach = 2,
    Follow   = 3,
    Retreat  = 4,
}

public enum EmoteKind
{
    Curioso  = 0,
    Feliz    = 1,
    Jugando  = 2,
    Molesto  = 3,
    Corazon  = 4,
    Zzz      = 5,
}

public enum SocialInteractionKind
{
    PlayChase     = 0,
    SleepTogether = 1,
    GremlinFight  = 2,
}
}
