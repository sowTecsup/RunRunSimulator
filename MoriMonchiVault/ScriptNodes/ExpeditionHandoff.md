---
tags: [script, core, handoff, expedition]
---

# ExpeditionHandoff.cs

**Ruta:** `Core/ExpeditionHandoff.cs`

**Responsabilidad:** Puente estático singleton entre escenas (tienda/arena) que encapsula el traspaso de datos de expedición. Mantiene estado de navegación (CameFromStore), resultado de ronda (HasResult, Result) y expone flujo de viaje: GoToArena() → arena → ReturnToStore(result). S119: introducido para desacoplar GameScene y ArenaSandbox de conocimiento mutuo sobre expediciones.

**Estado Estático:**
- `CameFromStore` — bool; true si se inició desde tienda, reset en SubsystemRegistration
- `HasResult` — bool; true si hay un ExpeditionResult pendiente de consumir
- `Result` — ExpeditionResult; struct con Seed, Winner, PlayerSecured, RivalSecured, Stats

## Struct ExpeditionResult

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Seed` | int | Semilla de la sala (ActiveSeed de ArenaSandbox) |
| `Winner` | ExpeditionTeam | Equipo ganador |
| `PlayerSecured` | int | Material asegurado por el jugador |
| `RivalSecured` | int | Material asegurado por rival |
| `Stats` | List<ArenaRoundStat> | Estadísticas por criatura (S119) |

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `void GoToArena()` | Fija CameFromStore=true, limpia resultado, carga ArenaSandbox |
| `void ReturnToStore(ExpeditionResult? result)` | Fija resultado si lo hay (HasResult=true), deshabilita pausa, carga GameScene |
| `bool TryConsumeResult(out ExpeditionResult result)` | Lee resultado, limpia HasResult/CameFromStore, retorna si había pendiente |

## Inicialización

`[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` resetea estado al cargar cada sesión de Play.

## Flujo S119

1. **Tienda (GameScene):**
   - Botón "Partir" → `ExpeditionBridge.Depart()` → `ExpeditionHandoff.GoToArena()`
   - Transición a ArenaSandbox

2. **Arena (ArenaSandbox):**
   - Ronda corre hasta terminar
   - ArenaRound calcula Winner/PlayerSecured/RivalSecured
   - ArenaPlanPanel.ReturnToStore() llama `ExpeditionHandoff.ReturnToStore(lastResult)`

3. **Retorno (GameScene):**
   - ExpeditionBridge.Start() → si HasResult, ApplyResult()
   - Suma PlayerSecured a inventario, dispara InventoryChanged

## Invariantes

- CameFromStore solo true si se inició desde tienda (protege de navegación accidental)
- Result inerte hasta TryConsumeResult() — no puede consumirse dos veces
- Time.timeScale=1f en ReturnToStore para garantizar pausa deshabilitada tras ronda

## Vinculado a

[[Index/24 - Puente Tienda-Arena]]

**Conexiones:** [[ExpeditionBridge]], [[ArenaPlanPanel]], [[ArenaSandbox]], [[ArenaRound]]
