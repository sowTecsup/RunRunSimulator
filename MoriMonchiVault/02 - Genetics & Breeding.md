---
tags: [memory-bank, genetics, breeding, dna]
---

# 02 — Genetics & Breeding

> Relacionados: [[07 - Persistence & Identity]] (cómo se guarda el DNA), [[03 - Combat]] (stats efectivos), [[06 - Player & World]] (personalidad).

## Identidad de Criaturas (`CreatureDNA`)

```
ToStringID() = "BS0-A3-E1-M2-FF00AA"              // genetic string — contrato de red (inmutable)
UniqueID     = "BS0-A3-E1-M2-FF00AA-{Ticks}"      // clave en el registro
BirthDate    = DateTime (UTC)
Stamp()      → setea Timestamp + BirthDate de forma atómica antes de registrar
```

**Campos clave del DNA:**

- `CustomName` — adjetivo + sustantivo, auto en Mint/Breed via `CreatureNameBank.GetRandomName()`. Editable por el usuario.
- `MotherID`, `FatherID`, `ChildrenIDs` — referencias por `UniqueID` (no genetic strings).
- `Gender` — `Unknown` hasta mintearse. Se asigna 50/50 en `Mint` y `Breed`. **NO va en el DNA string.**
- `Personality` — archetype de comportamiento (6 valores). Random al mint/hatch (`CreatureGenerator.RandomPersonality()`), **NO se hereda**, **NO va en el genetic string** (metadata como Gender). Default `Curious`. La consume `MoriMochiAgent` vía `PersonalityProfileSO` (ver [[06 - Player & World]]).
- `CombatHistory` — `List<CombatRecord>`, un registro replayable por pelea (local + async). Persiste con el DNA (local + cloud). No acotado (`MaxFightCount` puede cambiar).
- `FightCount`, `WinCount`, `BreedCount` — progresión, escritos por `CombatService` y `BreedingService`.
- `BodyTier`, `ArmTier`, `EyeTier`, `MouthTier` — Tier por slot, independiente por instancia (Tier1 al nacer).
- `BaseHP`, `BaseAttack`, `BaseSpeed` — stats base aleatorios 1–10, asignados en Mint.
- `IsDead` — muerte permanente; bloquea combate y breeding si es `true`.
- `BusyState`, `BreedReadyAt`, `BreedPartnerID` — cache de estado async (ver [[03 - Combat]] y sección Breeding Async abajo).

## Sistema de IDs de Partes

- IDs **auto-generados** por la database. **No se editan manualmente** — `BodyPart.ID` es `[ReadOnly]`.
- Formato: `BS0`, `BS1`… / `A0`, `A1`… / `E0`, `E1`… / `M0`, `M1`…
- ⚠️ **Los IDs NUNCA pueden contener el carácter `-`** (es el separador del DNA string).
- Botón **Sync All IDs** en cada database renumera TODO desde 0. Usar en setup inicial, **nunca con DNA strings ya distribuidos en red**.

## Sistema de Nombres de Partes (`PartNameBank`)

- Clase estática, pools de 5 palabras por cada `(PartSet, PartRole)`.
- Botón **Roll Name** en cada `BodyPart` SO: genera nombre individual.
- Botón **Roll All Names** en cada database SO: genera nombres en bulk.
- Nombre de criatura = `"{body} {arm} {eye} {mouth}"` → `CreatureDNA.GetDisplayName(db)`.
- Palabras temáticamente ligadas al `PartSet` (GooGang = pegajoso, ZapZone = eléctrico, etc.).

## Part Sets (`PartSet` enum)

10 sets: `GooGang`, `BogBrigade`, `FuzzFactory`, `CosmicCreeps`, `NeonNightmares`, `CrunchCrew`, `GrimGlobs`, `SpudSquad`, `MoldMob`, `ZapZone`. Colores dinámicos en inspector con `[GUIColor]`.

## Tier (enum)

`Tier1 = 1`, `Tier2 = 2`, `Tier3 = 3`. Campo en `BodyPart`. Las partes nacen en Tier1. Evolución de Tier durante combate ver [[03 - Combat]].

## Gender (`CreatureGender` enum)

`Unknown` (sin registrar), `Male`, `Female`. **NO forma parte del DNA string.**

GDD target: género basado en battle-index del padre — pendiente Etapa 2.

## Sistema de Rareza (`RarityOddsTableSO`)

- Pesos relativos configurables por `Rarity`. Por defecto: Common 60 / Uncommon 25 / Rare 10 / Epic 4 / Legendary 1.
- `CreatureGenerator.GenerateRandom(db, oddsTable)` → cada uno de los 4 slots hace su propio `oddsTable.Roll()`.
- Si no se asigna tabla, el generador elige sin filtro de rareza.

## Sistema de Breeding (`InheritanceOddsTableSO`)

Probabilidades por defecto — configurables, normalizadas internamente:

| Origen | Peso por defecto |
|--------|-----------------|
| Padres directos | 40 |
| Abuelos | 20 |
| Bisabuelos | 10 |
| Mutación aleatoria | 20 |
| Base / entorno | 10 |

- Cada slot (body, arm, eye, mouth) hace su **propio roll independiente**.
- Mutación y Base → parte aleatoria del pool completo (sin filtro de rarity ni set).
- Si un ancestro no existe en el registro, fallback automático a random.
- Pesos editables directo en el inspector del SO asset — serialización Unity (no JSON).
- Singleton: `InheritanceOddsTableSO.Current` (se setea en `OnEnable` del SO).
- **Validaciones en `BreedingService.Breed()`**: `IsDead`, género correcto, `BreedCount < MaxBreedCount (4)`. `BreedCount` se incrementa en ambos padres dentro del servicio.
- **Stats del hijo**: cada stat (HP, ATK, SPD) hereda 50/50 de madre o padre, luego aplica delta aleatorio de **-1, 0 o +1**. Mínimo garantizado: 1.
- `GameManager`: botón **Fill Random Breeders** — selecciona hembra + macho vivos bajo el límite. Muestra info de breeds restantes en inspector.

## Sistema de Breeding Async (timer server-side)

Breeding con timer **server-authoritative**. El cliente nunca decide cuándo termina: el timestamp se stampa y se valida server-side. La cría sí se mintea localmente y se pushea (checkpoint para mover la generación server-side en una etapa futura).

### Almacenamiento — Game Data (Custom Data)

Los huevos viven en Custom Data como un **array** por jugador, key `breeding_eggs_<playerId>`, **solo escribible vía Cloud Code** (service token). El cliente no puede falsificar el tiempo.

```js
breeding_eggs_<playerId> = { entries: [ { motherId, fatherId, startedAt, readyAt }, ... ] }
// readyAt = startedAt + BREED_DURATION_MS, server-side
```

- **Varias parejas pueden incubar en paralelo**; una pareja (o cualquier padre) solo puede estar en un huevo a la vez — lo garantiza `BusyState.Breeding` + validación server-side.
- Un huevo se identifica por su par `(motherId, fatherId)` — único entre huevos activos.

### Flujo

1. **Breed Timer** (botón morado en `BreedingController`): valida padres localmente → `start-breeding` rechaza si alguno de los dos padres ya está en un huevo, si no stampa server-time y appendea al array → cliente marca ambos padres `BusyReason.Breeding`, cachea `BreedReadyAt`/`BreedPartnerID` (display) → push. El juego se puede cerrar.
2. **Show Eggs** (botón, sin server): lista TODOS los huevos con índice (`[0] "Mamá" x "Papá" — 12:34 left`) leyendo `BreedReadyAt` de las madres. NO es autoritativo — solo display.
3. **Hatch Egg** (botón + campo "Hatch Index"): toma el huevo en ese índice → `hatch-breeding(motherId, fatherId)` compara reloj real del server vs `readyAt`. Si `ready` → quita ese huevo del array (overwrite `{ entries }`), devuelve la pareja; el cliente limpia el Busy de ambos padres → `BreedingService.Breed()` mintea la cría local (+`BreedCount++`) → registra + push. Si `not_ready` → muestra el tiempo restante real del server.

### Anti-cheat — por qué el server stampa

Si el cliente escribiera el timestamp de inicio, podría atrasar el reloj del PC, iniciar el breed con timestamp viejo, restaurar la hora y el huevo estaría "listo" al instante. Por eso `start-breeding` (no el cliente) pone `startedAt`/`readyAt`, y viven en Custom Data (inaccesible para escritura del cliente). El hatch valida contra el reloj real del server.

- `CreatureDNA.BreedReadyAt` (long, epoch ms del server; 0 = no breeding) y `BreedPartnerID` — cache local para display, se persisten con el registry.
- `CombatService.Simulate` y `BreedingService.Breed` validan `IsBusy` → un padre incubando no puede pelear ni iniciar otro breed.
- El botón **Breed** (local, instantáneo) se conserva para testing — bypasea el timer por completo.

### Quirks Cloud Code (además de los de combate)

- `hatch-breeding` **no usa `deleteCustomItem`** (firma no verificada) — hace `splice` del huevo en el array y reescribe `{ entries }` (mismo patrón que el matchmaking pool).
- El array se envuelve en `{ entries: [...] }` — Custom Data rechaza arrays top-level (mismo quirk que `matchmaking_pool`).
- Params de ambos: `motherId`, `fatherId` (camelCase). `hatch-breeding` también los necesita para identificar qué huevo abrir.

## Estado del roadmap (Genética/Breeding)

| Feature | Estado |
|---------|--------|
| `BreedingService.Breed()` con traversal genealógico | ✅ |
| `InheritanceOddsTableSO` SO puro (pesos configurables en inspector) | ✅ |
| `CreatureRegistrySO` registry visual [ReadOnly] + JSON source of truth | ✅ |
| `SaveSystem` persistencia JSON completa | ✅ |
| `GameManager.MintRandomCreature()` y `BreedCreatures()` | ✅ |
| Validación límite máximo de crías (4) — `BreedingService.MaxBreedCount` | ✅ |
| Validación límite máximo de combates (5) — `CombatManagerSO.MaxFightCount` | ✅ |
| `IsDead` bloquea breed y combate | ✅ |
| Herencia de stats en Breed: 50/50 madre/padre + delta ±1, mínimo 1 | ✅ |
| Breeding async con timer server-side | ✅ |
| `IsBusy` bloquea breed y combate | ✅ |
| Género por battle-index del padre (actualmente 50/50) | 🔲 Pendiente (Etapa 2) |
| Bonus de rareza en la 4ª cría (última posible) | 🔲 Pendiente |
| Herencia del nivel Tier de las partes | 🔲 Pendiente |

## Anchor Points Estándar (Visualizador — Etapa 1.2)

- Estándar fijo: **2 arm anchors + 2 eye anchors + 1 mouth anchor** (formato 2-2-1).
- Partes = hijos del prefab con Transform propio (sin merge de mesh) — intercambiables en runtime.
- Se requiere **preview en editor** (editor-time assembly al seleccionar un DNA).

## CreateAssetMenu — Convenciones

```
RunRunSimulator/Parts/Arm
RunRunSimulator/Parts/Eye
RunRunSimulator/Parts/Mouth
RunRunSimulator/Parts/Body Shape
RunRunSimulator/Databases/Arm Database
RunRunSimulator/Databases/Eye Database
RunRunSimulator/Databases/Mouth Database
RunRunSimulator/Databases/Body Shape Database
RunRunSimulator/Creature Database (Orchestrator)
RunRunSimulator/Creature Registry
RunRunSimulator/Rarity Odds Table
RunRunSimulator/Inheritance Odds Table
RunRunSimulator/Combat Manager
RunRunSimulator/Personality Profile Table
```

## Archivos clave

```
Assets/RunRunSimulator/Scripts/
├── Core/
│   ├── CreatureGenerator.cs              # static: GenerateRandom(db, oddsTable?)
│   └── Enums.cs                          # Rarity, PartSet, CreatureGender, PartRole, Tier, BusyReason, Personality
├── Systems/Breeding/
│   ├── BreedingService.cs                # static: Breed() — traversal árbol genealógico
│   ├── BreedingController.cs             # MonoBehaviour: UI breeding (Fill + Breed local + Timer + Hatch)
│   └── AsyncBreedingService.cs           # MonoBehaviour: StartBreedingAsync / HatchAsync
├── Data/
│   ├── CreatureDNA.cs                    # Genética + Identidad + Linaje + Progresión + Stats + IsDead + Personality + CombatHistory
│   ├── CreatureRegistrySO.cs             # SO registry: Dictionary<string, CreatureDNA>
│   ├── CreatureDatabaseSO.cs             # SO orquestador: refs sub-DBs + validación de IDs
│   ├── CreaturePartData.cs
│   ├── PartNameBank.cs                   # static: pools de nombres por (PartSet, PartRole)
│   ├── RarityOddsTableSO.cs              # SO: pesos por Rarity
│   ├── InheritanceOddsTableSO.cs         # SO singleton: odds breeding
│   ├── Parts/                            # BodyPart, ArmPart, EyePart, MouthPart, BodyShapePart
│   └── Databases/                        # PartDatabaseSO<T> + Arm/Eye/Mouth/BodyShape DBs (IDPrefix por tipo)
└── UI/
    └── (ver [[05 - UI System]] — BreedingPanelUITK + CreatureGridUITK)
```
