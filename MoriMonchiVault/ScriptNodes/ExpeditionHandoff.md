---
tags: [script, core, handoff, expedition]
---

# ExpeditionHandoff

**Ruta:** `Core/ExpeditionHandoff.cs`

**Responsabilidad:** Puente estático singleton entre escenas (tienda/arena) que encapsula traspaso de datos. Mantiene estado de navegación (CameFromStore), resultado de expedición (Result) y expone flujo: `GoToArena()` → arena → `ReturnToStore()`. Genera `RunSeed` por TickCount.

## Estado Estático

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `CameFromStore` | `bool` | True si se inició desde tienda; reset en SubsystemRegistration |
| `HasResult` | `bool` | True si hay ExpeditionResult pendiente |
| `Result` | `ExpeditionResult` | Struct con datos de la expedición |
| `RunSeed` | `int` | Semilla raíz de bajada (generada en GoToArena) |
| `SelectedIds` | `List<string>` | IDs del equipo elegido pre-expedición |

## Struct ExpeditionResult (S129)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Seed` | `int` | Semilla raíz de bajada |
| `Winner` | `ExpeditionTeam` | Equipo ganador (Player/Rival) |
| `PlayerSecured` | `int` | Material asegurado jugador |
| `RivalSecured` | `int` | Material rival (siempre 0 en v1) |
| `Floors` | `int` | Pisos totales completados |
| `Lost` | `bool` | True si rival ganó piso de Enemies |
| `FallenIds` | `List<string>` | **S129:** IDs de criaturas caídas (health ≤ 0) |
| `Fallen` | `int` | Criaturas con health <= 0 |
| `TeamIds` | `List<string>` | **S129:** IDs del equipo del jugador que entró en la bajada |

## Struct ExpeditionReturn (S129)

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Seed` | `int` | Semilla de la run |
| `Winner` | `ExpeditionTeam` | Equipo ganador |
| `PlayerSecured` | `int` | Material asegurado jugador |
| `RivalSecured` | `int` | Material rival |
| `MineritaGained` | `int` | Minerita ingresada al inventario |
| `Fallen` | `int` | Criaturas caídas |
| `Floors` | `int` | Pisos completados |
| `Lost` | `bool` | Derrota |

**S129:** Se removieron `HealthLost` y `Creatures`.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `GoToArena(IReadOnlyList<string> ids)` | `void` | Copia ids a SelectedIds, genera RunSeed, fija CameFromStore=true, carga ArenaSandbox |
| `ReturnToStore(ExpeditionResult?)` | `void` | Fija Result si hay valor, timeScale=1, carga GameScene; sin valor → CameFromStore=false |
| `TryConsumeResult(out ExpeditionResult)` | `bool` | Intenta consumir resultado; limpia SelectedIds, retorna true si había resultado |

## Ciclo Tienda → Arena → Tienda

1. **Tienda (GameScene):**
   - Panel expedición: elegir equipo (IDs → SelectedIds)
   - `ExpeditionBridge.RequestDeparture(ids)` → `GoToArena(ids)`
   - RunSeed generado: `Environment.TickCount & 0x7fffffff`

2. **Arena (ArenaSandbox):**
   - `ArenaRunDirector` crea `ArenaRun(RunSeed, SelectedIds)`
   - Ciclo piso: juega → decide Continuar/Retirarse
   - Piso Buff cada 3: +30 vida a vivas
   - Piso Enemies: si pierde → Lost=true, PlayerSecured=0
   - `Retreat()` → `ReturnToStore(run.ToExpeditionResult())`
   - `ToResult()` arma `FallenIds` (criaturas con health ≤ 0) y `TeamIds` (elenco original)

3. **Retorno (GameScene):**
   - `ExpeditionBridge` lee HasResult
   - `ApplyResult()`: suma Minerita, aplica cambios, persiste

## Invariantes S129

- **RunSeed:** generado en GoToArena, no modificado
- **SelectedIds:** limpiados al consumir resultado o volver sin resultado
- **Lost:** anula PlayerSecured (material perdido)
- **FallenIds:** lista de IDs con health ≤ 0 al retornar
- **TeamIds:** copia del elenco original que entró (no cambia durante la bajada)
- **Fallen:** criaturas health <= 0 (no permanencia, solo estado de piso)
- **Moneda:** `MineritaGained` refleja ganancias de expedición

## Vinculado a

[[Index/24 - Puente Tienda-Arena]]
[[Index/26 - Plan H0 - Bajada por pisos]] (S124)

**Conexiones:** [[ExpeditionBridge]], [[ArenaRunDirector]], [[ArenaRun]], [[ArenaSandbox]], [[GameEvents]], [[GameManager]]
