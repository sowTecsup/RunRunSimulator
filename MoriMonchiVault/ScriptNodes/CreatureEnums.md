---
tags: [enum, creature, core]
---

# CreatureEnums.cs

**Ruta:** `Core/Enums/CreatureEnums.cs`

**Responsabilidad:** Enumeraciones para el ciclo de vida y estados de una criatura. Contiene: `CreatureGender` (Unknown/Male/Female), `LifeStage` (Newborn/Child/Teen/Adult/Elder), `MonchiMood` (12 estados: Neutral/Feliz/Triste/Dolor/Enojado/Dormido/Enfermo/Mareado/Asustado/Amoroso/Emocionado/KO), `Tier` (1/2/3 para rareza de partes), `BusyReason` (None/Breeding/Sold), `NeedType` (Health/Energy/Affect), `CreatureCondition` (Healthy/InNeed/Sick), `CreatureIntent` (**S98-S99 ACTUALIZADO:** 21 acciones: Idle/Wandering/Following/Approaching/Fleeing/Retreating/SeekingFood/SeekingRest/SeekingPlay/Eating/Resting/Playing/Held/Tumbling/Socializing/Chasing/SleepingTogether/Fighting/Collecting/**Taking**/**Losing**), `ProximityReaction` (Ignore/Flee/Approach/Follow/Retreat), `EmoteKind` (Curioso/Feliz/Jugando/Molesto/Corazon/Zzz), `SocialInteractionKind` (PlayChase/SleepTogether/GremlinFight).

**S93:** Consolidación de enums en archivo dedicado. **S97:** Agregado `CreatureIntent.Collecting = 18`. **S98-S99:** Agregados `Taking = 19` (beat de recolección: criatura toma mineral) y `Losing = 20` (beat de recolección: rival toma mineral).

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
| `CreatureIntent` | **S98-S99:** 21 valores: Idle (0) - Fighting (17), Collecting (18), **Taking (19)**, **Losing (20)** |
| `ProximityReaction` | Ignore, Flee, Approach, Follow, Retreat |
| `EmoteKind` | Curioso, Feliz, Jugando, Molesto, Corazon, Zzz |
| `SocialInteractionKind` | PlayChase, SleepTogether, GremlinFight |

## CreatureIntent (completa lista S98)

```csharp
public enum CreatureIntent
{
    Idle               = 0,
    Wandering          = 1,
    Following          = 2,
    Approaching        = 3,
    Fleeing            = 4,
    Retreating         = 5,
    SeekingFood        = 6,
    SeekingRest        = 7,
    SeekingPlay        = 8,
    Eating             = 9,
    Resting            = 10,
    Playing            = 11,
    Held               = 12,
    Tumbling           = 13,
    Socializing        = 14,
    Chasing            = 15,
    SleepingTogether   = 16,
    Fighting           = 17,
    Collecting         = 18,  // S97: criatura busca mineral
    Taking             = 19,  // S98: criatura está tomando/comiendo mineral (beat discreto)
    Losing             = 20,  // S99: rival acaba de tomar mineral (criatura pierde)
}
```

## Cambios S98

**Dos nuevos valores en CreatureIntent:**
- `Taking = 19` — beat discreto: la criatura está en el acto de agarrar/consumir un mineral (animación Take de ~1.2s). Manejado por `AgentExpedition.TickExpedition()` y `ArenaCueOverlay`.
- `Losing = 20` — beat discreto: rival acaba de tomar un mineral que el agente buscaba. Disparado por `AgentExpedition` cuando otro agente consume su target. Se mapea a `MonchiMood.Asustado` o `Enojado` en `MonchiMoodDriver`.

**Uso:**
- `MoriMochiAgent.Intent` devuelve `Taking` durante la fase de consumición (beat `TakeSeconds` de `ExpeditionRulesSO`).
- `MoriMochiAgent.Intent` devuelve `Losing` cuando rival consume el target.
- `ArenaCueOverlay` y `CueStyleSO` mapean estos intents a colores de ruta/HUD.
- `MonchiGestureDriver` se sincroniza: `Taking` → gesto "Taking" (si existe en SO), `Losing` → gesto "Losing" (si existe).
- `MonchiMoodDriver` mapea: `Taking` → según contexto (Emocionado), `Losing` → Asustado/Enojado.

## Uso

- `CreatureGender`, `LifeStage` — metadatos de criatura
- `MonchiMood` — animación y retroalimentación visual en MoriMonchiMoodDriver
- `Tier` — rareza de partes en CreatureDNA
- `BusyReason` — estado temporal (criatura no disponible: breeding o ya vendida)
- `NeedType`, `CreatureCondition` — sistema de necesidades
- `CreatureIntent` — intención locomotora del agente. **S97:** incluye `Collecting` (búsqueda). **S98-S99:** incluye `Taking` (acción discreto) y `Losing` (reacción).
- `ProximityReaction` — reacción instintiva al jugador/NPCs
- `EmoteKind` — emotes visuales (pequeños iconos)
- `SocialInteractionKind` — interacción social entre criaturas

## Vinculado a

- [[Index/01 - Creature Genetics & System]]
- [[Index/23 - Arena Sandbox y Expedicion]] (S97-S98)
- [[CreatureDNA]] — contiene LifeStage, CreatureGender, BusyReason
- [[MonchiMoodDriver]] — consume MonchiMood, mapea CreatureIntent
- [[NeedsState]] — consume NeedType, CreatureCondition

**Conexiones:** [[CreatureDNA]], [[MonchiMoodDriver]], [[NeedsState]], [[MoriMochiAgent]], [[AgentExpedition]], [[CueStyleSO]], [[ArenaCueOverlay]], [[MonchiGestureDriver]]
