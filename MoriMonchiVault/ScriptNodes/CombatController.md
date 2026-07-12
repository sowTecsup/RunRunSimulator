---
tags: [combat, controller, singleton]
---

# CombatController

**Ruta:** `Systems/Combat/CombatController.cs`

**Responsabilidad:** Apex orquestador de combate (domain owner). Singleton: `static Instance`. Posee `CombatManagerSO` (config). Public API: `SimulateLocal(idsA, idsB, rowsA, rowsB)` genera seed, llama `CombatService.Simulate()`, dispara eventos, persiste. Wrappers async para `AsyncCombatService` (enqueue, dequeue, poll). Resuelve registry y database de `GameManager.Instance` en Awake. **S37/S39:** Única firma de `SimulateLocal()` es la 3v3 con equipos + rows.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `SimulateLocal(List<string> idsA, List<string> idsB, List<int> rowsA, List<int> rowsB)` | `CombatResult` | **S37+** Única sobrecarga: Genera `seed = Guid.NewGuid().GetHashCode()`, valida equipos 3v3, llama `CombatService.Simulate(idsA, idsB, rowsA, rowsB, ..., seed)`, dispara `GameEvents.CombatCompleted()` y `GameEvents.RegistryChanged()` |
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
| `config` | `CombatManagerSO` | Referencia al SO de config (roles, max rounds, etc.) |
| `asyncCombatService` | `AsyncCombatService` | Referencia al servicio async (mismo GameObject) |

## Ciclo de Simulación Local (S32 + S37 + S39)

1. `SimulateLocal(idsA, idsB, rowsA, rowsB)` es llamado por UI (CombatLineupUITK) / CombatDevConsole
2. Valida `config != null`
3. Genera `seed = System.Guid.NewGuid().GetHashCode()` (pseudo-aleatorio local)
4. **S37+:** Valida equipos (size match, no duplicados, alive, not busy, fights restantes)
5. Llama `CombatService.Simulate(idsA, idsB, rowsA, rowsB, registry, database, config, equipDb, seed)`
6. Si result != null:
   - Dispara `GameEvents.CombatCompleted(result)`
   - Dispara `GameEvents.RegistryChanged(registry)` → persistencia automática
7. Retorna result

## Cambios S37 + S39

**Firma única:** `SimulateLocal(List<string> idsA, List<string> idsB, List<int> rowsA, List<int> rowsB)` es la única sobrecarga pública. La anterior `SimulateLocal(string aID, string bID)` fue eliminada.

**Equipo validation:** Las validaciones de `CombatService.ValidateTeam()` se ejecutan antes de `SimulateCore`. Si validation falla, retorna null.

**Rows handling:** rowsA/rowsB pueden ser null (default 2-3-2 lineup) o custom vía params. Callers responsables de validación de rows.

**S39:** CombatService ahora resuelve Element/Affinity/Energy de cada Combatant en BuildCombatant. No hay cambios en firma de Controller.

## Vinculado a

- [[Index/03 - Combat]]
- [[Index/13 - Combat Design Direction]]
- [[CombatService]] — orquesta simulación 3v3
- [[CombatManagerSO]] — config immutable (roles, rules, synergies, elemental)
- [[AsyncCombatService]] — orchestrates async combat
- [[GameManager]] — proporciona registry/database
- [[GameEvents]] — dispara eventos
- [[CombatLineupUITK]] — consumer de `SimulateLocal()` (S37 lineup UI)
- [[CombatDevConsole]] — consumer de `SimulateLocal()`

## Conexiones

**Entrada:**
- Serialized refs en inspector (config, asyncService)
- `GameManager.Instance` resuelve en Awake

**Salida:**
- `GameEvents.CombatCompleted()` — notifica visualizador
- `GameEvents.RegistryChanged()` — trigger persistencia
- Enqueue/poll tasks hacia async combate

## Notas (S32 + S37 + S39)

- **Seed local:** `Guid.NewGuid().GetHashCode()` es no-determinista pero suficiente para combate local (sí es determinista para async, que viene del servidor).
- **Determinismo:** Local no necesita reproducibilidad (sin replay); async sí (del servidor + seed compartido).
- **S37:** Lineup (rows) puede ser null (default 2-3-2) o custom vía params. Callers responsables de validación de rows.
- **S39:** Sistema elemental integrado; Element/Affinity/Energy resueltos en BuildCombatant de cada Combatant.
- **No crea records:** `CombatService.Simulate()` crea y almacena records; Controller solo orquesta.
