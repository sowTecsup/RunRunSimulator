---
tags: [script, world, expedition, orchestrator]
---

# ArenaRunDirector.cs

**Ruta:** `World/Expedition/ArenaRunDirector.cs`

**Responsabilidad:** Orquestador runtime que maneja la secuencia de pisos de una bajada. Crea `ArenaRun` en Awake si viene de la tienda (ExpeditionHandoff.CameFromStore). Todas las criaturas entran con vida máxima. Coordina ciclo: EnterFloor → juega arena → Update detecta fin → RecordFloor → panel permite Continuar/Retirarse. Evento `FloorEnded` notifica cambios de estado. DefaultExecutionOrder(-50) asegura que se inicialice antes que ArenaRound.

**S124:** Creado. Instancia única en escena ArenaSandbox. Punto de conexión entre persistencia (ExpeditionHandoff) y sandbox (ArenaSandbox + ArenaRound).

**S129:** Ya no llama `SetStartHealth()`; todas entran con MaxHealth.

## Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Active` | `bool` | True si run ≠ null (bajada en curso) |
| `Run` | `ArenaRun` | Instancia actual; null si no activa |
| `CurrentKind` | `ArenaFloorKind` | Tipo piso actual (Enemies/Buff); default Enemies si no activa |
| `FloorRecorded` | `bool` | True si última ronda fue procesada por RecordFloor |
| `TeamNames` | `IReadOnlyDictionary<string, string>` | Mapeo ID → nombre de criatura (poblado en Start) |

## Eventos

| Evento | Cuándo dispara |
|--------|--------|
| `FloorEnded` | Después de ejecutar `RecordFloor()` en Update |

## Métodos Clave

| Método | Descripción |
|--------|--------|
| `Continue()` | Avanza al próximo piso: `run.EnterFloor()`, regenera arena (ArenaSandbox.SetFloor), resetea ArenaRound, limpia FloorRecorded |
| `Retreat()` | Retorno a tienda: llama `ExpeditionHandoff.ReturnToStore(run.ToResult())` |
| `GiveUp()` | Alias de Retreat() |

## Ciclo de Vida (S129)

1. **Awake()**: si CameFromStore, crea `new ArenaRun(RunSeed, SelectedIds)` e invoca `run.EnterFloor()` (piso 1)
2. **Start()**: lee IDs + nombres de criatura del sandbox pool (todas empiezan con MaxHealth)
3. **Update()**: monitorea `round.IsOver`; si True y no FloorRecorded, ejecuta RecordFloor, dispara FloorEnded
4. Gameflow: panel detecta FloorRecorded y permite Continuar o Retirarse
5. Continuar → Continue() → piso siguiente
6. Retirarse → Retreat() → carga tienda con resultado

## Dependencias Inyectadas

- `[Required] ArenaSandbox sandbox` — generador de arena y pool de criaturas
- `[Required] ArenaRound round` — resultado de ronda (IsOver, Winner, PlayerSecured, Summary)

## Invariantes S129

- Run creado solo en Awake si CameFromStore (seguridad scene-reload)
- Health inicial = MaxHealth (todas entran llenas)
- FloorRecorded toggleado en Update (evita RecordFloor doble)
- Retreat limpia SelectedIds (no persistir datos entre viajes)

## Vinculado a

[[Index/22 - Bajada Nocturna y Linaje]] (S122), [[Index/26 - Plan H0 - Bajada por pisos]] (S124)

## Conexiones

- [[ArenaRun]] — modelo de estado
- [[ArenaFloorPanel]] — UI que consume Active/Run/FloorRecorded
- [[ArenaSandbox]] — regenera arena por semilla
- [[ArenaRound]] — input de resultados
- [[ExpeditionHandoff]] — entrada/salida de escena, SelectedIds, RunSeed
