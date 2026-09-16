---
tags: [script, core, handoff, expedition]
---

# ExpeditionHandoff.cs

**Ruta:** `Core/ExpeditionHandoff.cs`

**Responsabilidad:** Puente estático singleton entre escenas (tienda/arena) que encapsula el traspaso de datos de expedición. Mantiene estado de navegación (CameFromStore), resultado de ronda (HasResult, Result) y expone flujo de viaje: GoToArena() → arena → ReturnToStore(result). S119: introducido para desacoplar GameScene y ArenaSandbox. **S120:** agrega SelectedIds para el equipo elegido. **S121:** introduce struct ExpeditionReturn para el retorno con energía gastada.

**Estado Estático:**
- `CameFromStore` — bool; true si se inició desde tienda, reset en SubsystemRegistration
- `HasResult` — bool; true si hay un ExpeditionResult pendiente de consumir
- `Result` — ExpeditionResult; struct con Seed, Winner, PlayerSecured, RivalSecured, Stats
- `SelectedIds` — List<string> (S120); IDs únicos del equipo elegido antes de bajar

## Struct ExpeditionResult

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Seed` | int | Semilla de la sala (ActiveSeed de ArenaSandbox) |
| `Winner` | ExpeditionTeam | Equipo ganador |
| `PlayerSecured` | int | Material asegurado por el jugador |
| `RivalSecured` | int | Material asegurado por rival |
| `Stats` | List<ArenaRoundStat> | Estadísticas por criatura (S119) |

## Struct ExpeditionReturn (S121)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Seed` | int | Semilla de la sala |
| `Winner` | ExpeditionTeam | Equipo ganador |
| `PlayerSecured` | int | Material asegurado |
| `RivalSecured` | int | Material rival |
| `MaterialGained` | int | Material ingresado al inventario (PlayerSecured) |
| `EnergySpent` | int | Total de energía gastada por el elenco |
| `Creatures` | int | Cantidad de criaturas que gastaron energía |

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `void GoToArena(IReadOnlyList<string> ids = null)` | **(S120)** Fija CameFromStore=true, copia ids a SelectedIds, limpia resultado, carga ArenaSandbox |
| `void ReturnToStore(ExpeditionResult? result)` | Fija resultado si lo hay, deshabilita pausa (timeScale=1), carga GameScene; si no hay resultado desactiva CameFromStore |
| `bool TryConsumeResult(out ExpeditionResult result)` | Lee resultado, limpia HasResult/CameFromStore/SelectedIds, retorna si había pendiente |

## Flujo S119-S121

1. **Tienda (GameScene):**
   - Panel expedición: elegir hasta 3 criaturas → IDs en SelectedIds
   - `ExpeditionBridge.RequestDeparture(ids)` → `ExpeditionHandoff.GoToArena(ids)`
   - Transición a ArenaSandbox

2. **Arena (ArenaSandbox):**
   - Lee SelectedIds, bloquea esas 3 criaturas en el elenco
   - Ronda corre hasta terminar
   - ArenaRound calcula Winner/PlayerSecured/RivalSecured
   - ArenaPlanPanel.ReturnToStore() llama `ExpeditionHandoff.ReturnToStore(lastResult)`

3. **Retorno (GameScene):**
   - ExpeditionBridge.Start() → si HasResult, ApplyResult()
   - Suma PlayerSecured a inventario
   - Gasta energía en criaturas del jugador: 20 + (5 × tumbadas), máximo 40
   - Dispara GameEvents.ExpeditionReturned(ExpeditionReturn)

## Invariantes

- CameFromStore solo true si se inició desde tienda (protege navegación accidental)
- SelectedIds vacío si se dispara sin ids o al volver (se limpia en GoToArena y TryConsumeResult)
- Result inerte hasta TryConsumeResult() — no puede consumirse dos veces
- Time.timeScale=1f en ReturnToStore para garantizar pausa deshabilitada tras ronda

## S120-S122

- **S120:** SelectedIds lista para guardar equipo elegido (IReadOnlyList público). GoToArena(ids) copia los IDs.
- **S121:** ExpeditionReturn struct nuevo con MaterialGained, EnergySpent, Creatures. ReturnToStore(null) apaga CameFromStore.
- **S122:** Sin cambios.

## Vinculado a

[[Index/24 - Puente Tienda-Arena]]

**Conexiones:** [[ExpeditionBridge]], [[ArenaPlanPanel]], [[ArenaSandbox]], [[ArenaRound]], [[GameEvents]]
