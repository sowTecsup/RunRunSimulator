---
tags: [script, genetics, breeding]
---

# BreedingContainer

**Ruta:** `World/Containers/BreedingContainer.cs`

## Responsabilidad

Corral de cría extendido de `MoriMochiContainer`. Hereda ancla vía `IAnchorPlace` (sin su propia clave estática). Implementa `IInteractable` (tap E para eclosionar). Auto-pair timer con dice roll (`affinity × diceChance`), restaura pasivamente necesidades de ocupantes. Gestiona courtship visual (posa pareja ante frente en puntos fijos `breedingSlots`). Al retirar un Morimonchi cancela emparejamiento + huevo en servidor. En `Start()`/`OnDestroy()` llama `base.Start()/base.OnDestroy()` para auto-registro en `AnchorRegistry`. Estático `All` mantiene lista de todos los pens activos. **S39 cambio:** Afinidad ahora usa `dna.Role` en lugar de `dna.Personality` (deprecated).

## Cambios S39

**TryRollPair actualizado:**
- Antes: consultaba afinidad vía `dna.Personality` (Personality enum, 6 valores)
- Ahora: consulta afinidad vía `dna.Role` (Role enum, 3 valores: Protector/Agresivo/Empático)

```csharp
private bool TryRollPair(bool ...)
{
    // Busca madre y padre eligibles
    var mother = ...;  // Female, Breeding, LocationSlot libre, no cooldown
    var father = ...;  // Male, Breeding, LocationSlot libre, no cooldown
    
    if (mother == null || father == null) return false;
    
    // S39: usar Role
    float affinity = BreedingController.Instance?.GetAffinity(mother.DNA.Role, father.DNA.Role) ?? 0.5f;
    float pairChance = affinity * diceChance;
    
    if (rng.NextFloat() >= pairChance) return false;
    
    // Proceder con breeding...
}
```

**Logs y diagnósticos actualizados:**
- Diagnostico de pareja (BuildDiagnostics) ahora muestra `dna.Role` en lugar de `dna.Personality`
- LastRollInfo log muestra afinidad resuelta vía Role

## Campos Principales

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

## API Pública

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

## Ciclo de TryRollPair (S39 cambio)

1. `Update()` cuenta `rollTimer`, cada `rollInterval` segundos llama `TryRollPair(false, false)`
2. Busca madre+padre elegibles (Female+Breeding, sin cooldown, LocationSlot disponible)
3. **S39:** Consulta `BreedingController.Instance.GetAffinity(mother.Role, father.Role)` (Role, no Personality)
4. Calcula `pairChance = affinity × diceChance`
5. Si `rng.NextFloat() < pairChance`:
   - Stamps LocationSlot a ambos DNAs
   - Llama `BreedingController.StartBreedingAsync(motherID, fatherID)` → server-side egg incubation
   - Inicia courtship visual (orbita/tienda)
6. Si falla roll, intenta siguiente par

## Cambios en S21

- Borrado: `penKey`, diccionario `byKey`, `TryGet()`, `ReclaimDirect()`, corrutina `ReclaimBreedingOccupants()` → todo absorbido por la base + `AnchorRegistry`.
- Renombrado: `HomePenKey`/`HomePenSlot` → `LocationKey`/`LocationSlot` en `CreatureDNA` (genérico, no sólo cría).
- Campo `all` ahora es lista estática local (antes parte de `byKey`).
- Usa `base.Start()`/`base.OnDestroy()` para auto-registrar/desregistrar en `AnchorRegistry`.

## Conexiones

- **`MoriMochiContainer` (base)**: Hereda `Claim()`, `DetachOccupant()`, ocupantes, confinement. Llama `base.Start()/OnDestroy()` para auto-ancla.
- **`AnchorRegistry`**: Registrado automáticamente via `base.Start()` (del padre).
- **`CreatureDNA`**: Estampa `LocationKey`/`LocationSlot` (antes `HomePenKey`/`HomePenSlot`) en padres al emparejar. Campo `Role` (S39) usado para afinidad.
- **`BreedingController`**: Consulta `All` para conocer dónde crían, obtiene afinidad via `GetAffinity(Role, Role)` (S39), inicia breeding async.
- **`GameEvents`**: Suscrito a `OnBreedingCompleted` para lanzar crías.
- **`MoriMochiSpawner`**: Consulta `AnchorRegistry` (no más `TryGet()` del corral directo).
- **`MoriMochiAgent`**: Confinado via `EnterConfinement()`. Participa en courtship (female Tend en anchor, male Orbit).
- **`BreedingAffinityTableSO`**: Tabla de afinidad Role → Role (S39, antes Personality → Personality).

## Notas de Implementación

- **S21:** La "reclamación" en carga ahora es responsabilidad de `MoriMochiSpawner` + `AnchorRegistry`, no del corral.
- **S39:** Afinidad resuelta vía Role (enum 3 valores), no Personality. Impacta TryRollPair directamente.
- `LocationSlot` es el índice en `breedingSlots[]` si una pareja está emparejada (resolvería el anchor de courtship).
- `Release()` cancela breeding del servidor si la criatura se retira mientras incuba.
- Courtship usa los puntos `breedingSlots[LocationSlot].spotA/spotB` o cae al centro si el slot no existe (fallback gracioso).

**Vinculado a:** [[Index/02 - Genetics & Breeding]], [[Index/06 - Player & World]]
