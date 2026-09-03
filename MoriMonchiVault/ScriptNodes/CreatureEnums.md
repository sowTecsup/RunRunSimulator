---
tags: [enum, creature, core]
---

# CreatureEnums.cs

**Ruta:** `Core/Enums/CreatureEnums.cs`

**Responsabilidad:** Enumeraciones para el ciclo de vida y estados de una criatura. Contiene: `CreatureGender` (Unknown/Male/Female), `LifeStage` (Newborn/Child/Teen/Adult/Elder), `MonchiMood` (11 estados: Neutral/Feliz/Triste/Dolor/Enojado/Dormido/Enfermo/Mareado/Asustado/Amoroso/Emocionado/KO), `Tier` (1/2/3 para rareza de partes), `BusyReason` (None/Breeding/Sold), `NeedType` (Health/Energy/Affect), `CreatureCondition` (Healthy/InNeed/Sick), `CreatureIntent` (**S97 NUEVO:** 19 acciones: Idle/Wandering/Following/Approaching/Fleeing/Retreating/SeekingFood/SeekingRest/SeekingPlay/Eating/Resting/Playing/Held/Tumbling/Socializing/Chasing/SleepingTogether/Fighting/**Collecting**), `ProximityReaction` (Ignore/Flee/Approach/Follow/Retreat), `EmoteKind` (Curioso/Feliz/Jugando/Molesto/Corazon/Zzz), `SocialInteractionKind` (PlayChase/SleepTogether/GremlinFight).

**S93:** Consolidación de enums en archivo dedicado. **S97:** Agregado `CreatureIntent.Collecting = 18`.

## Enumeraciones

| Enum | Valores |
|------|---------|
| `CreatureGender` | Unknown (0), Male (1), Female (2) |
| `LifeStage` | Newborn, Child, Teen, Adult, Elder |
| `MonchiMood` | Neutral, Feliz, Triste, Dolor, Enojado, Dormido, Enfermo, Mareado, Asustado, Amoroso, Emocionado, KO (0-11) |
| `Tier` | Tier1, Tier2, Tier3 |
| `BusyReason` | None (0), Breeding (2), Sold (3) |
| `NeedType` | Health (0), Energy (1), Affect (2) |
| `CreatureCondition` | Healthy (0), InNeed (1), Sick (2) |
| `CreatureIntent` | **S97:** 19 valores: Idle (0) - Fighting (17), **Collecting (18)** |
| `ProximityReaction` | Ignore, Flee, Approach, Follow, Retreat |
| `EmoteKind` | Curioso, Feliz, Jugando, Molesto, Corazon, Zzz |
| `SocialInteractionKind` | PlayChase, SleepTogether, GremlinFight |

## CreatureIntent (completa lista)

```csharp
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
    Collecting  = 18,  // S97 NUEVO
}
```

## Cambios S97

**Nuevo valor en CreatureIntent:**
- `Collecting = 18` — criatura está persiguiendo un material recolectable. Manejado por `AgentExpedition`. Se mapea a `MonchiMood.Emocionado` en `MonchiMoodDriver`.

**Uso:**
- `MoriMochiAgent.Intent` devuelve `CreatureIntent.Collecting` cuando estado == `AgentState.Expedition`
- `ArenaCueOverlay` lo usa para colorear rutas (mapa a color del SO CueStyle)
- `CueStyleSO.PopulateDefaults()` precarga color cyan para Collecting

## Uso

- `CreatureGender`, `LifeStage` — metadatos de criatura
- `MonchiMood` — animación y retroalimentación visual en MoriMonchiMoodDriver
- `Tier` — rareza de partes en CreatureDNA
- `BusyReason` — estado temporal (criatura no disponible: breeding o ya vendida)
- `NeedType`, `CreatureCondition` — sistema de necesidades
- `CreatureIntent` — intención locomotora del agente (para AI). **S97:** incluye `Collecting` para expedición
- `ProximityReaction` — reacción instintiva al jugador/NPCs
- `EmoteKind` — emotes visuales (pequeños iconos)
- `SocialInteractionKind` — interacción social entre criaturas

## Vinculado a

- [[Index/01 - Creature Genetics & System]]
- [[Index/23 - Arena Sandbox y Expedicion]] (S97)
- [[CreatureDNA]] — contiene LifeStage, CreatureGender, BusyReason
- [[MonchiMoodDriver]] — consume MonchiMood, **S97:** mapea Collecting → Emocionado
- [[NeedsState]] — consume NeedType, CreatureCondition

**Conexiones:** [[CreatureDNA]], [[MonchiMoodDriver]], [[NeedsState]], [[MoriMochiAgent]], **S97:** [[AgentExpedition]], [[CueStyleSO]], [[ArenaCueOverlay]]
