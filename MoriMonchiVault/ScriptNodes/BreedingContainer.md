---
tags: [script, genetics]
---

# BreedingContainer.cs

**Ruta:** `World/Containers/BreedingContainer.cs`

## Responsabilidad

Corral de cría extendido de `MoriMochiContainer`. Hereda ancla vía `IAnchorPlace` (sin su propia clave estática). Implementa `IInteractable` (tap E para eclosionar). Auto-pair timer con dice roll (`affinity × diceChance`), restaura pasivamente necesidades de ocupantes. Gestiona courtship visual (posa pareja ante frente en puntos fijos `breedingSlots`). Al retirar un Morimonchi cancela emparejamiento + huevo en servidor. En `Start()`/`OnDestroy()` llama `base.Start()/base.OnDestroy()` para auto-registro en `AnchorRegistry`. Estático `All` mantiene lista de todos los pens activos.

## Cambios en S21

- Borrado: `penKey`, diccionario `byKey`, `TryGet()`, `ReclaimDirect()`, corrutina `ReclaimBreedingOccupants()` → todo absorbido por la base + `AnchorRegistry`.
- Renombrado: `HomePenKey`/`HomePenSlot` → `LocationKey`/`LocationSlot` en `CreatureDNA` (genérico, no sólo cría).
- Campо `all` ahora es lista estática local (antes parte de `byKey`).
- Usa `base.Start()`/`base.OnDestroy()` para auto-registrar/desregistrar en `AnchorRegistry`.

## Campos principales

| Campo | Tipo | Propósito |
|-------|------|----------|
| `diceChance` | float | Multiplicador de afinidad en el roll: `afinidad × diceChance = probabilidad`. |
| `rollInterval` | float | Segundos entre intentos de emparejamiento automático. |
| `pairCooldown` | float | Segundos mínimos entre emparejamientos sucesivos de la misma criatura. |
| `restoreRate` | float | Necesidades (salud/energía/afecto) restauradas por segundo a ocupantes. |
| `breedingSlots` | BreedingSlot[] | Puntos fijos de cría: `spotA`/`spotB` (posiciones donde se paran los padres mirándose). |
| `launchHeight` | float | Altura sobre el centro desde donde se lanzan crías recién nacidas. |
| `birthEjectDistance` | float | Distancia FUERA del borde donde aterrizan las crías (salen disparadas). |
| `all` | static List<BreedingContainer> | Todos los corrales activos en escena. |

## API pública

| Método | Firma | Propósito |
|--------|-------|----------|
| `All` { get; } | static IReadOnlyCollection<BreedingContainer> | Todos los corrales activos (usado por BreedingController para conocer dónde crían). |
| `ActivePairs()` | IEnumerable<(string, string, int)> | Tuplas (madre, padre, slot) de parejas activamente incubando en ESTE corral. |
| `LaunchPoint` { get; } | Vector3 | Centro + launchHeight (punto de lanzamiento de crías). |
| `Interact()` | void | IInteractable: tap E para eclosionar huevo listo de la madre hembra en este corral. |
| `Release(MoriMochiAgent)` | void (override) | Retiro por jugador: llama `base.Release()` + cancela breeding si la criatura estaba emparejada. |
| `Start()` | void (override) | Llama `base.Start()` (auto-registra en AnchorRegistry), agrega a `all`. |
| `OnDestroy()` | void (override) | Llama `base.OnDestroy()` (desregistra), remueve de `all`. |
| `OnBreedingCompleted` | event handler | Cuando una pareja de ESTE corral termina incubación, lanza la cría + retira padres de courtship. |

## Conexiones

- **`MoriMochiContainer` (base)**: Hereda `Claim()`, `DetachOccupant()`, ocupantes, confinement. Llama `base.Start()/OnDestroy()` para auto-ancla.
- **`AnchorRegistry`**: Registrado automaticamente via `base.Start()` (del padre).
- **`CreatureDNA`**: Estampa `LocationKey`/`LocationSlot` (antes `HomePenKey`/`HomePenSlot`) en padres al emparejar.
- **`BreedingController`**: Consulta `All` para conocer dónde crían, obtiene tabla de afinidad, inicia breeding async via `StartBreedingAsync()`.
- **`GameEvents`**: Suscrito a `OnBreedingCompleted` para lanzar crías.
- **`MoriMochiSpawner`**: Consulta `AnchorRegistry` (no más `TryGet()` del corral directo).
- **`MoriMochiAgent`**: Confinado via `EnterConfinement()`. Participa en courtship (female Tend en anchor, male Orbit).

## Notas de implementación

- S21: La "reclamación" en carga ahora es responsabilidad de `MoriMochiSpawner` + `AnchorRegistry`, no del corral.
- `LocationSlot` es el índice en `breedingSlots[]` si una pareja está emparejada (resolvería el anchor de courtship).
- `Release()` cancela breeding del servidor si la criatura se retira mientras incuba.
- Courtship usa los puntos `breedingSlots[LocationSlot].spotA/spotB` o cae al centro si el slot no existe (fallback gracioso).

**Vinculado a:** [[Index/02 - Genetics & Breeding]], [[Index/06 - Player & World]]
