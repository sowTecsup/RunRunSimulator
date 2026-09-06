---
tags: [enum, creature, core]
---

# CreatureEnums.cs

**Ruta:** `Core/Enums/CreatureEnums.cs`

**Responsabilidad:** Enumeraciones para el ciclo de vida y estados de una criatura. Contiene: `CreatureGender`, `LifeStage`, `MonchiMood` (12 estados), `Tier`, `BusyReason`, `NeedType`, `CreatureCondition`, `CreatureIntent` (27 valores incluyendo S98-S101 nuevos), `ProximityReaction`, `EmoteKind`, `SocialInteractionKind`. **S101 NUEVO:** CreatureIntent suma cinco nuevos valores (Carrying, Securing, Guarding, Hunting, Taunting) derivados de ocupaciones de expedición. **Nota:** `Occupation` enum está en [[WorldEnums]], no aquí.

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
| `CreatureIntent` | **S101:** 27 valores: Idle (0) - Taunting (27) |
| `ProximityReaction` | Ignore, Flee, Approach, Follow, Retreat |
| `EmoteKind` | Curioso, Feliz, Jugando, Molesto, Corazon, Zzz |
| `SocialInteractionKind` | PlayChase, SleepTogether, GremlinFight |

## CreatureIntent S101 (27 valores)

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
    Collecting         = 18,  // S97: busca material
    Taking             = 19,  // S98: tomando mineral
    Losing             = 20,  // S99: rival toma su mineral
    Clashing           = 21,  // S100: combate físico
    Dazed              = 22,  // S100: post-golpe
    Carrying           = 23,  // S101: cargando material (Gather fase Returning)
    Securing           = 24,  // S101: depositando material (Gather fase Securing)
    Guarding           = 25,  // S101: vigilando (Guard ocupación)
    Hunting            = 26,  // S101: persiguiendo rival (Break ocupación)
    Taunting           = 27,  // S101: provocando rival (Decoy ocupación)
}
```

## Cambios S101: Intents de Ocupación

**Nuevos CreatureIntent (líneas 88-92):**

- `Carrying = 23` — cargando material después de minería, regresando a salida
  - Generado por: `AgentExpedition` cuando Gather en fase Returning
  - Gesto: "Walk" o idle (sin gesto especial, locomotion normal)
  - Color Cue: mismo que Taking (amarillo)
  - Duración: variable según distancia a salida

- `Securing = 24` — depositando material en salida (celebración corta)
  - Generado por: `AgentExpedition` cuando Gather en fase Securing (llegó a salida)
  - Gesto: "Yes" (celebración, S101 PopulateDefaults)
  - Color Cue: amarillo (Taking color)
  - Duración: breve (1-2 segundos)

- `Guarding = 25` — vigilando puesto de material
  - Generado por: `AgentExpedition` cuando Guard ocupación activa
  - Gesto: ninguno mapeado (locomotion normal con pausa en puesto)
  - Color Cue: púrpura o teal (guardia)
  - Duración: hasta que rival llegue o sesión termine

- `Hunting = 26` — persiguiendo rival que recolecta
  - Generado por: `AgentExpedition` cuando Break ocupación activa, rival en rango
  - Gesto: ninguno mapeado (locomotion de persecución)
  - Color Cue: rojo (conflicto)
  - Duración: hasta alcanzar o perder rival (puede transicionar a Clashing)

- `Taunting = 27` — provocando rival (Decoy ocupación)
  - Generado por: `AgentExpedition` cuando Decoy ocupación, fase Approach/Taunt
  - Gesto: "Roar" (desafío, S101 PopulateDefaults)
  - Emote: Molesto (rival molesto)
  - Color Cue: naranja (provocación)
  - Duración: breve (2-3 segundos), luego Fleeing

## Mapeo Ocupación → Fases → Intent

**Gather:**
- Noticing → Collecting
- Moving → Collecting
- Taking (minería) → Taking
- Carrying (regresando) → **Carrying**
- Securing (deposita) → **Securing**

**Guard:**
- Guarding (vigilancia) → **Guarding**

**Break:**
- Hunting (persigue rival) → **Hunting**
- (Transiciona a Clashing si entra en combate)

**Decoy:**
- Approaching → Approaching
- Taunt → **Taunting** + emota Molesto
- Fleeing → Fleeing

## Uso en Drivers Visuales S101

**MonchiGestureDriver:**
- intent = Carrying → TryEnterGesture(Carrying) → unmapped, sigue locomotion
- intent = Securing → TryEnterGesture(Securing) → "Yes" (celebración)
- intent = Taunting → TryEnterGesture(Taunting) → "Roar" (desafío)
- intent = Guarding → TryEnterGesture(Guarding) → unmapped, sigue locomotion
- intent = Hunting → TryEnterGesture(Hunting) → unmapped, sigue locomotion (o Clashing si entra combate)

**MonchiMoodDriver:**
- Carrying → neutral/feliz (cargando bien)
- Securing → feliz (celebración exitosa)
- Taunting → molesto (provocador)
- Guarding → neutral/concentrado (vigilante)
- Hunting → enojado (perseguidor agresivo)

**ArenaCueOverlay (ColorFor):**
- Carrying → ColorFor(Taking) = amarillo
- Securing → ColorFor(Taking) = amarillo
- Taunting → ColorFor(Taking) = amarillo
- Guarding → custom (guardia color, si existe en CueStyleSO)
- Hunting → FightColor o custom (rojo agresivo)

## Invariantes S101 + S100 + S98

- **Ocupación ≠ Intent:** Ocupación es estrategia a largo plazo (no cambia durante sesión, asignada al spawn); Intent es acción actual y cambia cada frame/segundo.
- **Ocupación None → Gather:** fallback seguro; agentes sin ocupación asignada usan recolección.
- **Explore es placeholder:** puede usarse para debug o futuro; actualmente traduce a Gather.
- **S101:** Cinco nuevos CreatureIntent (23-27) derivados de ocupaciones (Carry, Guard, Break, Decoy)
- **S101:** Los cinco nuevos **NO** están mapeados a Hold gestures por defecto (transitorios), solo dos tienen Enter gestures (Securing → "Yes", Taunting → "Roar")

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]] (sección 8: Ocupaciones con tiempo)
- [[CreatureDNA]] — contiene LifeStage, CreatureGender, BusyReason
- [[MonchiMoodDriver]] — consume Intent
- [[MoriMochiAgent]], [[AgentExpedition]], [[AgentContext]]

## Conexiones

[[AgentExpedition]], [[ArenaRosterSO]], [[CueStyleSO]], [[ArenaCueOverlay]], [[MonchiGestureDriver]], [[MonchiMoodDriver]], [[ArenaCameraDirector]], [[WorldEnums]] (Occupation enum)
