---
tags: [combat, controller, singleton]
---

# CombatController

**Ruta:** `Systems/Combat/CombatController.cs`

**Responsabilidad:** Apex orquestador de combate (domain owner). Singleton: `static Instance`. Posee `CombatManagerSO` (config). Public API: `SimulateLocal()` genera seed, llama `CombatService.Simulate()`, dispara eventos, persiste. Wrappers async para `AsyncCombatService` (enqueue, dequeue, poll). Resuelve registry y database de `GameManager.Instance` en Awake.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `SimulateLocal(string aID, string bID)` | `CombatResult` | **S32:** Genera `seed = Guid.NewGuid().GetHashCode()`, valida, llama `CombatService.Simulate(..., seed)`, dispara `GameEvents.CombatCompleted()` y `GameEvents.RegistryChanged()` |
| `EnqueueForAsyncCombat(string uniqueID, bool scheduled)` | `Task` | Encolador wrapper → `AsyncCombatService.EnqueueInstantAsync()` o `EnqueueScheduledAsync()` |
| `DequeueAsync(CreatureDNA dna)` | `Task` | Wrapper → `AsyncCombatService.DequeueAsync()` |
| `PollResultsAsync()` | `Task` | Wrapper → `AsyncCombatService.PollResultsAsync()` |
| `FetchQueuedIdsAsync()` | `Task<HashSet<string>>` | Wrapper → `AsyncCombatService.FetchQueuedIdsAsync()` |
| `FetchPendingResultIdsAsync()` | `Task<HashSet<string>>` | Wrapper → `AsyncCombatService.FetchPendingResultIdsAsync()` |

## Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Instance` | `static CombatController` | Singleton lazily initialized en Awake |
| `Config` | `CombatManagerSO` | Getter de SO (encapsulación) |

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `config` | `CombatManagerSO` | Referencia al SO de config |
| `asyncCombatService` | `AsyncCombatService` | Referencia al servicio async (mismo GameObject) |

## Ciclo de Simulación Local (S32)

1. `SimulateLocal(aID, bID)` es llamado por UI / CombatDevConsole
2. Valida `config != null`
3. Genera `seed = System.Guid.NewGuid().GetHashCode()` (pseudo-aleatorio local)
4. Llama `CombatService.Simulate(aID, bID, registry, database, config, equipDb, seed)`
5. Si result != null:
   - Dispara `GameEvents.CombatCompleted(result)`
   - Dispara `GameEvents.RegistryChanged(registry)` → persistencia automática
6. Retorna result

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatService]] — orquesta simulación
- [[CombatManagerSO]] — config immutable
- [[AsyncCombatService]] — orchestrates async combat
- [[GameManager]] — proporciona registry/database
- [[GameEvents]] — dispara eventos
- [[CombatDevConsole]] — consumer de `SimulateLocal()`, `EnqueueForAsyncCombat()`

## Conexiones

**Entrada:**
- Serialized refs en inspector (config, asyncService)
- `GameManager.Instance` resuelve en Awake

**Salida:**
- `GameEvents.CombatCompleted()` — notifica visualizador
- `GameEvents.RegistryChanged()` — trigger persistencia
- Enqueue/poll tasks hacia async combate

## Notas (S32)

- **Seed local:** `Guid.NewGuid().GetHashCode()` es no-determinista pero suficiente para combate local (sí es determinista para async, que viene del servidor).
- **Determinismo:** Local no necesita reproducibilidad (sin replay); async sí (del servidor + seed compartido).
- **No crea records:** `CombatService.Simulate()` crea y almacena records; Controller solo orquesta.
