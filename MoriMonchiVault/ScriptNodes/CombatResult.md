---
tags: [script, combat, data, result]
---

# CombatResult.cs

**Ruta:** `Data/Combat/CombatResult.cs`

**Responsabilidad:** DTO transiente que encapsula el resultado de `CombatService.SimulateCore()` — outcome, loot, evolución, turnos turn-by-turn y log debug. No persiste (excepto via snapshot en `CombatRecord`); es el objeto de trabajo durante/inmediatamente post-combate. Estructura idéntica al JSON que emite Cloud Code JS para async seamless deserialization.

## Cambios S33

**Nuevos campos:** `StatsA` y `StatsB` (tipo `CombatFighterSnapshot`) capturan stats post-equipment de ambos combatientes al inicio de la simulación. Poblados por `SimulateCore()` directamente; copiados a `CombatRecord.SelfStats/OpponentStats` en `BuildRecord()` para persistencia + display.

## Campos Públicos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `WinnerID` | `string` | UniqueID ganador (null si draw) |
| `LoserID` | `string` | UniqueID perdedor (null si draw) |
| `WinnerName` | `string` | Nombre customizado ganador |
| `LoserName` | `string` | Nombre customizado perdedor |
| `LoserDied` | `bool` | Si perdedor llegó a ≤0 HP |
| `WinnerEvolved` | `bool` | Si ganador evolucionó parte (true = EvolvedSlot != null) |
| `IsDraw` | `bool` | Si combate fue draw (MaxRounds alcanzados) |
| `EvolvedSlot` | `string` | Slot evolucionado ("Body", "Arm", "Eye", "Mouth", o null) |
| `StatsA` | `CombatFighterSnapshot` | **NUEVO S33** Stats post-equipment de combatante A al inicio |
| `StatsB` | `CombatFighterSnapshot` | **NUEVO S33** Stats post-equipment de combatante B al inicio |
| `Log` | `List<string>` | Debug log (turno-a-turno, rolls, evasiones, statuses, etc.) |
| `Turns` | `List<CombatTurn>` | Turn-by-turn estructurado (atacante, daño, crit, HP restante, procs) |

## CombatFighterSnapshot — S33

**Misma estructura que en CombatRecord:**

| Campo | Tipo |
|-------|------|
| `MaxHp` | `float` |
| `Attack` | `float` |
| `Speed` | `float` |
| `Defense` | `float` |
| `Luck` | `float` |
| `Evasion` | `float` |

**Poblado en SimulateCore():**
```csharp
var result = new CombatResult();
var A = BuildCombatant(dnaA, db, equipDb, true);
var B = BuildCombatant(dnaB, db, equipDb, false);
result.StatsA = Snapshot(A);  // Helper privado: extrae 6 fields de Combatant
result.StatsB = Snapshot(B);
```

## Flujo de Construcción

1. `CombatService.Simulate()` llama `SimulateCore(...)`
2. `SimulateCore()` instancia `CombatResult`, popula `StatsA/StatsB`, simu rounds
3. Para cada turno, llama `EmitTurn(result, ...)` → agrega `CombatTurn` a `result.Turns`
4. Al terminar, popula `WinnerID`, `LoserID`, `EvolvedSlot`, `IsDraw`, `Log`
5. Retorna `result`
6. Caller (`Simulate()`) llama 2× `BuildRecord(result, ...)` → copia `StatsA/StatsB` a cada record

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Summary` | `string` (property) | Resumen 1-liner: "DRAW —" o "Winner: {WinnerName} [EVOLVED {Slot}] [LOSER DIED]" |

## Serialización

JSON con Newtonsoft.Json. PascalCase campo names (match Cloud Code JS). `CombatFighterSnapshot` es `[Serializable]` puro.

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatService]] — genera en `SimulateCore()`, popula `StatsA/StatsB`
- [[CombatRecord]] — copia snapshots vía `BuildRecord()`
- [[CombatTurn]], [[CombatProcEvent]] — nested structures
- [[CombatFighterSnapshot]] — S33
- [[AsyncCombatService]] — deserializa result homólogo desde Cloud Code JS

## Conexiones

**Entrada:**
- `CombatService.SimulateCore(dnaA, dnaB, db, config, equipDb, rng)` → retorna `CombatResult`

**Salida:**
- `CombatService.Simulate()` → `BuildRecord()` 2× → `CombatRecord.SelfStats/OpponentStats` → persistencia + display
- `CombatController` → dispatch a `UIManager` (panel visual, notifs)
- Async: `CloudMatchBlob` emitida por JS contiene idéntica estructura `CombatResult` → ambos clientes deserializan

## Notas

- **Transiente:** Solo vive durante la simulación y el ensamblaje del record. No persiste directo.
- **S33 Stats:** Ambos `StatsA/StatsB` populated en `SimulateCore()` (el kernel puro) — antes de elegir ganador, antes de cualquier lógica client-side. Garantiza simetría: ambos clientes async ven idénticos stats post-equipment.
- **Log:** Contiene traces detalladas (rolls, evasiones, statuses, sinergias, evolución, muerte). Útil debug, también para future "combat replay log viewer".
- **Turns:** Simétrico — ambos fighters en async ven idéntica secuencia de `CombatTurn`.
