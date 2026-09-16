---
tags: [script, core, handoff, expedition]
---

# ExpeditionHandoff.cs

**Ruta:** `Core/ExpeditionHandoff.cs`

**Responsabilidad:** Puente estático singleton entre escenas (tienda/arena) que encapsula el traspaso de datos de expedición. Mantiene estado de navegación (CameFromStore), resultado de ronda (HasResult, Result) y expone flujo de viaje: GoToArena() → arena → ReturnToStore(result). S119: introducido para desacoplar GameScene y ArenaSandbox. S120: agrega SelectedIds para el equipo elegido. S121: introduce struct ExpeditionReturn. **S124:** Expande ExpeditionResult para soportar bajada por pisos: agrega Floors (pisos completados), Lost (bool), HealthById (delta vida por criatura), Fallen (caídas).

**Estado Estático:**
- `CameFromStore` — bool; true si se inició desde tienda, reset en SubsystemRegistration
- `HasResult` — bool; true si hay un ExpeditionResult pendiente de consumir
- `Result` — ExpeditionResult; struct con Seed, Winner, PlayerSecured, RivalSecured, Floors, Lost, HealthById, Fallen
- `RunSeed` — int (S124); semilla raíz de la bajada actual
- `SelectedIds` — List<string> (S120); IDs únicos del equipo elegido antes de bajar

## Struct ExpeditionResult (S124)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Seed` | `int` | Semilla raíz de la bajada |
| `Winner` | `ExpeditionTeam` | Equipo ganador (Player si no Lost, Rival si Lost) |
| `PlayerSecured` | `int` | Material asegurado por el jugador (0 si Lost) |
| `RivalSecured` | `int` | Material rival (siempre 0 en v1) |
| `Floors` | `int` | Pisos totales completados |
| `Lost` | `bool` | True si rival ganó un piso de Enemies |
| `HealthById` | `Dictionary<string, int>` | Delta vida por criatura (actual - inicio); negativo si dañada |
| `Fallen` | `int` | Cantidad de criaturas con health <= 0 |

## Struct ExpeditionReturn (S121)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Seed` | `int` | Semilla de la sala |
| `Winner` | `ExpeditionTeam` | Equipo ganador |
| `PlayerSecured` | `int` | Material asegurado |
| `RivalSecured` | `int` | Material rival |
| `MaterialGained` | `int` | Material ingresado al inventario |
| `HealthLost` | `int` | Salud total perdida del equipo |
| `Fallen` | `int` | Criaturas caídas |
| `Creatures` | `int` | Cantidad de criaturas en equipo |
| `Floors` | `int` | Pisos completados |
| `Lost` | `bool` | Derrota |

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `void GoToArena(IReadOnlyList<string> ids)` | Fija CameFromStore=true, RunSeed=TickCount, copia ids a SelectedIds, carga ArenaSandbox |
| `void ReturnToStore(ExpeditionResult? result)` | Fija resultado si lo hay, timeScale=1, carga GameScene; si no hay resultado desactiva CameFromStore |
| `bool TryConsumeResult(out ExpeditionResult result)` | Lee resultado, limpia HasResult/CameFromStore/SelectedIds, retorna si había pendiente |

## Flujo S119-S124

1. **Tienda (GameScene):**
   - Panel expedición: elegir hasta 3 criaturas → IDs en SelectedIds
   - `ExpeditionBridge.RequestDeparture(ids)` → `ExpeditionHandoff.GoToArena(ids)` + RunSeed generado
   - Transición a ArenaSandbox

2. **Arena (ArenaSandbox):**
   - ArenaRunDirector crea `new ArenaRun(RunSeed, SelectedIds)`
   - Ciclo: EnterFloor → juega piso → RecordFloor → decide Continuar/Retirarse
   - Piso Buff cada 3 (recupera 30 vida)
   - Piso Enemies: si Rival gana → Lost=true, Material=0
   - ArenaRunDirector.Retreat() → llama `ReturnToStore(run.ToResult())`

3. **Retorno (GameScene):**
   - ExpeditionBridge.Start() → si HasResult, ApplyResult()
   - Suma PlayerSecured a inventario
   - Gasta energía en criaturas: base + caídas
   - Dispara GameEvents con HealthById/Fallen

## Invariantes S124

- RunSeed generado en GoToArena (TickCount & 0x7fffffff)
- SelectedIds vacío si se vuelve sin ids o al consumir resultado
- Lost anula PlayerSecured (material perdido)
- HealthById delta = (actual - inicio); refleja daño acumulado
- Fallen = criaturas con health <= 0 (no permanente, solo estado)

## Vinculado a

[[Index/24 - Puente Tienda-Arena]], [[Index/26 - Plan H0 - Bajada por pisos]] (S124)

**Conexiones:** [[ExpeditionBridge]], [[ArenaPlanPanel]], [[ArenaRunDirector]], [[ArenaRun]], [[ArenaSandbox]], [[ArenaRound]], [[GameEvents]]
