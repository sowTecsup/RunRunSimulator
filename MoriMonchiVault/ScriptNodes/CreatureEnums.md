---
tags: [enum, creature, core]
---

# CreatureEnums.cs

**Ruta:** `Core/Enums/CreatureEnums.cs`

**Responsabilidad:** Enumeraciones para el ciclo de vida y estados de una criatura. Contiene: `CreatureGender` (Unknown/Male/Female), `LifeStage` (Newborn/Child/Teen/Adult/Elder), `MonchiMood` (12 estados: Neutral/Feliz/Triste/Dolor/Enojado/Dormido/Enfermo/Mareado/Asustado/Amoroso/Emocionado/KO), `Tier` (1/2/3 para rareza de partes), `BusyReason` (None/Breeding/Sold), `NeedType` (Health/Energy/Affect), `CreatureCondition` (Healthy/InNeed/Sick), `CreatureIntent` (**S100 ACTUALIZADO:** 23 acciones: Idle/Wandering/Following/Approaching/Fleeing/Retreating/SeekingFood/SeekingRest/SeekingPlay/Eating/Resting/Playing/Held/Tumbling/Socializing/Chasing/SleepingTogether/Fighting/Collecting/**Taking**/**Losing**/**Clashing**/**Dazed**), `ProximityReaction` (Ignore/Flee/Approach/Follow/Retreat), `EmoteKind` (Curioso/Feliz/Jugando/Molesto/Corazon/Zzz), `SocialInteractionKind` (PlayChase/SleepTogether/GremlinFight).

**S93:** Consolidación de enums en archivo dedicado. **S97:** Agregado `CreatureIntent.Collecting = 18`. **S98-S99:** Agregados `Taking = 19` (beat de recolección: criatura toma mineral) y `Losing = 20` (beat de recolección: rival toma mineral). **S100 NUEVO:** Agregados `Clashing = 21` (combate físico, estados Anticipating/Striking/Resolving internos) y `Dazed = 22` (estado post-golpe con solo giro en lugar).

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
| `CreatureIntent` | **S100:** 23 valores: Idle (0) - Fighting (17), Collecting (18), **Taking (19)**, **Losing (20)**, **Clashing (21)**, **Dazed (22)** |
| `ProximityReaction` | Ignore, Flee, Approach, Follow, Retreat |
| `EmoteKind` | Curioso, Feliz, Jugando, Molesto, Corazon, Zzz |
| `SocialInteractionKind` | PlayChase, SleepTogether, GremlinFight |

## CreatureIntent (lista completa S100)

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
    Clashing           = 21,  // S100 NUEVO: combate físico (estados Anticipating/Striking/Resolving)
    Dazed              = 22,  // S100 NUEVO: post-golpe, mareado, solo gira en lugar
}
```

## Cambios S100: Combate Físico

**Dos nuevos valores en CreatureIntent:**
- `Clashing = 21` — estado de combate físico activo. Devuelto por `AgentClash.Intent` cuando en fases Anticipating o Striking. Mapea a mood Enojado y cue naranja. Se dibuja flecha roja pulsante en ArenaCueOverlay. No interfiere con Expedition/Social; tiene prioridad en TryEngage.
- `Dazed = 22` — estado post-golpe, mareado. Devuelto por `AgentClash.Intent` cuando en fase Dazed (durando tuning.DazedSeconds). Criatura solo gira hacia atacante pero no se mueve. Mapea a mood Mareado y cue violeta. Gesto "No". Transiciona a counter-attack (Clashing) o retrete+roaming según boldness y distancia al atacante.

**Mapeos en sistemas visuales:**
- [[MonchiMoodDriver]] (líneas 51-52): `Clashing → MonchiMood.Enojado`, `Dazed → MonchiMood.Mareado`
- [[CueStyleSO]] (líneas 114-115): `Clashing → naranja (1, 0.45, 0.15)`, `Dazed → violeta (0.75, 0.6, 0.95)`
- [[MonchiGestureSetSO]] (línea 84): `Dazed → "No"` (gesto de no entendimiento)
- [[ArenaCueOverlay]] (líneas 365-379): Dibuja `DrawClash()` si `agent.ClashTarget != null` (flecha roja pulsante aditiva)
- [[ArenaCameraDirector]] (línea 29): Enfoca si `Intent == Clashing || Intent == Dazed`

**Uso en AgentClash:**
- `clash.Intent` devuelve `Clashing` si en Anticipating/Striking, `Dazed` si en Dazed, else None
- `MoriMochiAgent.Intent` delega a `clash.Intent` cuando en estado Clashing
- Permite que drivers visuales reaccionen al combate sin tocar AgentClash internamente

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
- `CreatureIntent` — intención locomotora del agente. **S97:** incluye `Collecting` (búsqueda). **S98-S99:** incluye `Taking` (acción discreto) y `Losing` (reacción). **S100:** incluye `Clashing` (combate) y `Dazed` (post-golpe).
- `ProximityReaction` — reacción instintiva al jugador/NPCs
- `EmoteKind` — emotes visuales (pequeños iconos)
- `SocialInteractionKind` — interacción social entre criaturas

## Vinculado a

- [[Index/01 - Creature Genetics & System]]
- [[Index/23 - Arena Sandbox y Expedicion]] (S97-S98-S100)
- [[CreatureDNA]] — contiene LifeStage, CreatureGender, BusyReason
- [[MonchiMoodDriver]] — consume MonchiMood, mapea CreatureIntent
- [[NeedsState]] — consume NeedType, CreatureCondition
- **S100:** [[AgentClash]] — genera Clashing/Dazed intents

**Conexiones:** [[CreatureDNA]], [[MonchiMoodDriver]], [[NeedsState]], [[MoriMochiAgent]], [[AgentExpedition]], [[CueStyleSO]], [[ArenaCueOverlay]], [[MonchiGestureDriver]], **S100:** [[AgentClash]], [[ArenaCameraDirector]]
