---
tags: [enum, creature, core]
---

# CreatureEnums.cs

**Ruta:** `Core/Enums/CreatureEnums.cs`

**Responsabilidad:** Enumeraciones para el ciclo de vida y estados de una criatura. Contiene: `CreatureGender` (Unknown/Male/Female), `LifeStage` (Newborn/Child/Teen/Adult/Elder), `MonchiMood` (11 estados: Neutral/Feliz/Triste/Dolor/Enojado/Dormido/Enfermo/Mareado/Asustado/Amoroso/Emocionado/KO), `Tier` (1/2/3 para rareza de partes), `BusyReason` (None/Breeding/Sold), `NeedType` (Health/Energy/Affect), `CreatureCondition` (Healthy/InNeed/Sick), `CreatureIntent` (18 acciones: Idle/Wandering/Following/Approaching/Fleeing/Retreating/SeekingFood/SeekingRest/SeekingPlay/Eating/Resting/Playing/Held/Tumbling/Socializing/Chasing/SleepingTogether/Fighting), `ProximityReaction` (Ignore/Flee/Approach/Follow/Retreat), `EmoteKind` (Curioso/Feliz/Jugando/Molesto/Corazon/Zzz), `SocialInteractionKind` (PlayChase/SleepTogether/GremlinFight).

**S93:** Consolidación de enums en archivo dedicado.

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
| `CreatureIntent` | 18 valores: desde Idle hasta Fighting |
| `ProximityReaction` | Ignore, Flee, Approach, Follow, Retreat |
| `EmoteKind` | Curioso, Feliz, Jugando, Molesto, Corazon, Zzz |
| `SocialInteractionKind` | PlayChase, SleepTogether, GremlinFight |

## Uso

- `CreatureGender`, `LifeStage` — metadatos de criatura
- `MonchiMood` — animación y retroalimentación visual en MoriMochiAnimationDriver
- `Tier` — rareza de partes en CreatureDNA
- `BusyReason` — estado temporal (criatura no disponible: breeding o ya vendida)
- `NeedType`, `CreatureCondition` — sistema de necesidades
- `CreatureIntent` — intención locomotora del agente (para AI)
- `ProximityReaction` — reacción instintiva al jugador/NPCs
- `EmoteKind` — emotes visuales (pequeños iconos)
- `SocialInteractionKind` — interacción social entre criaturas

## Vinculado a

- [[Index/01 - Creature Genetics & System]]
- [[CreatureDNA]] — contiene LifeStage, CreatureGender, BusyReason
- [[MonchiAnimationDriver]] — consume MonchiMood
- [[NeedsState]] — consume NeedType, CreatureCondition

**Conexiones:** [[CreatureDNA]], [[MonchiAnimationDriver]], [[NeedsState]], [[MoriMochiAgent]]

