---
tags: [enum, creature, core, expedition]
---

# CreatureEnums.cs

**Ruta:** `Core/Enums/CreatureEnums.cs`

**Responsabilidad:** Enumeraciones centrales de comportamiento y estado. CreatureGender, LifeStage, MonchiMood, Tier, BusyReason, NeedType, CreatureCondition, CreatureIntent (S103: 30 valores), ProximityReaction, EmoteKind, SocialInteractionKind, HatchResult (S131). S131: Agregado HatchResult enum para flujo de cría local.

## Enumeraciones

| Enum | Valores |
|------|---------|
| CreatureGender | Unknown, Male, Female |
| LifeStage | Newborn, Child, Teen, Adult, Elder |
| MonchiMood | Neutral, Feliz, Triste, Dolor, Enojado, Dormido, Enfermo, Mareado, Asustado, Amoroso, Emocionado, KO |
| Tier | Tier1, Tier2, Tier3 |
| BusyReason | None, Breeding, Sold |
| NeedType | Health, Energy, Affect |
| CreatureCondition | Healthy, InNeed, Sick |
| **CreatureIntent** | **S103: 30 valores** (Idle 0 - Reporting 29) |
| ProximityReaction | Ignore, Flee, Approach, Follow, Retreat |
| EmoteKind | Curioso, Feliz, Jugando, Molesto, Corazon, Zzz |
| SocialInteractionKind | PlayChase, SleepTogether, GremlinFight |
| **HatchResult** | **(S131)** Hatched, NotReady, InsufficientMinerita, Invalid |

## CreatureIntent S103 (30 valores)

```
Idle = 0
Wandering = 1
Following = 2
Approaching = 3
Fleeing = 4
Retreating = 5
SeekingFood = 6
SeekingRest = 7
SeekingPlay = 8
Eating = 9
Resting = 10
Playing = 11
Held = 12
Tumbling = 13
Socializing = 14
Chasing = 15
SleepingTogether = 16
Fighting = 17
Collecting = 18        (S97: busca material)
Taking = 19            (S98: minando)
Losing = 20            (S99: rival toma su mineral)
Clashing = 21          (S100: combate físico)
Dazed = 22             (S100: post-golpe)
Carrying = 23          (S101: cargando material)
Securing = 24          (S101: depositando)
Guarding = 25          (S101: vigilando)
Hunting = 26           (S101: persiguiendo rival)
Taunting = 27          (S101: provocando rival)
Exploring = 28         (S103: scout navega a veta NUEVO)
Reporting = 29         (S103: scout reporta veta NUEVO)
```

## HatchResult S131 (NUEVO)

```csharp
public enum HatchResult
{
    Hatched              = 0,
    NotReady             = 1,
    InsufficientMinerita = 2,
    Invalid              = 3,
}
```

**Propósito:** Retorno de `IncubationService.TryHatch()` para indicar éxito o tipo de falla.

| Valor | Significado | Acción UI |
|-------|-------------|-----------|
| **Hatched** | Eclosión exitosa; criatura mintada y registrada | Animar hatching, mostrar criatura nueva |
| **NotReady** | Huevo aún incubando; BreedReadyAt no alcanzado | Toast "No está listo" + mostrar tiempo faltante |
| **InsufficientMinerita** | Cartera insuficiente para pagar eclosión | Toast "Insuficiente Minerita" + mostrar costo |
| **Invalid** | Padres no encontrados o estado corrupto | Toast "Error: estado inválido" + log error |

**Flujo en BreedingEggsTabPresenter:**
```csharp
HatchResult result = IncubationService.TryHatch(motherID, fatherID);
switch (result)
{
    case HatchResult.Hatched:
        // Animar eclosión
        break;
    case HatchResult.NotReady:
        // Mostrar tiempo faltante
        break;
    case HatchResult.InsufficientMinerita:
        // Pedir más Minerita
        break;
    case HatchResult.Invalid:
        // Log error
        break;
}
```

## S103 Cambios: Exploring & Reporting

**Exploring = 28** — scout viajando a veta descubierta
- Generado por: AgentScout.Step=Traveling
- Gesto: locomotion normal (no mapeado)
- Mood: Neutral (exploración tranquila)
- Color Cue: verde azulado (0.55, 0.9, 0.6)

**Reporting = 29** — scout reportando veta al pizarrón
- Generado por: AgentScout.Step=Reporting
- Gesto: "Yes" (celebración reporte, S103)
- Mood: Emocionado (descubrimiento exitoso)
- Color Cue: amarillo-verde (0.75, 1, 0.45)

## Cambios S131 (HC-4)

**Agregado:** HatchResult enum (introducido en S131 para cría local síncrona).

**Propósito:** IncubationService.TryHatch() retorna HatchResult en lugar de Task<bool> o void. Permite UI distinguir entre no listo, insuficiente fondos e inválido.

## Vinculado a

- [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]
- [[Index/02 - Genetics & Breeding]] (S131)
- [[Index/09 - Active Context]] (S131)

## Conexiones

**S103 (Exploración):**
- [[AgentScout]], [[AgentExpedition]], [[TeamBlackboard]], [[MonchiMoodDriver]], [[MonchiGestureSetSO]], [[CueStyleSO]]

**S131 (Cría local):**
- [[IncubationService]] — retorna HatchResult
- [[BreedingEggsTabPresenter]] — consume HatchResult en UI
- [[Wallet]] — costo de eclosión en Minerita

## Notas

- **HatchResult orden:** Hatched=0 (éxito), luego fallos en orden de probabilidad (NotReady > InsufficientMinerita > Invalid).
- **Integración UI:** BreedingEggsTabPresenter usa switch(result) para animar/toastear.
