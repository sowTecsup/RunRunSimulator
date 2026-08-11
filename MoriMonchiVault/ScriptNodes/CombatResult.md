---
tags: [script, combat, data, result]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatResult.cs

**Ruta:** `Data/Combat/CombatResult.cs`

**Responsabilidad:** DTO transiente que encapsula el resultado de `CombatService.SimulateCore()` — outcome, evolución, muerte, turnos turn-by-turn y log debug. **S37:** Reshape a equipos (3v3 determinista team-based, tolerancia 1..3 por lado). No persiste (excepto via snapshot en `CombatRecord`); es el objeto de trabajo durante/inmediatamente post-combate. Estructura (S37) similar al JSON que emite Cloud Code JS para async seamless deserialization.

## Cambios S37

**RESHAPE COMPLETO a equipos:** Campos Winner/Loser eliminados. Nuevos campos:
- `TeamAWon` (bool) — verdadero si Team A ganó (false = Team B ganó o draw)
- `IsDraw` (bool) — verdadero si maxrounds alcanzados sin winner
- `EvolvedSlot`, `EvolvedUnitId`, `EvolvedUnitName` — stats de la 1 criatura ganadora que evolucionó
- `DiedUnitId`, `DiedUnitName` — stats de la 1 criatura perdedora que murió (5% chance)
- `TeamA`, `TeamB` — snapshots de todos los units post-fight (índice = unit index)
- `Turns` — simétricos, solo CombatTurn ha sido actualizado a incluir índices de unit + estado de equipo

## Campos Públicos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `TeamAWon` | `bool` | **S37** Verdadero si Team A ganó (false = Team B o draw) |
| `IsDraw` | `bool` | Si combate fue draw (MaxRounds alcanzados sin decisión) |
| `EvolvedSlot` | `string` | Slot evolucionado por ganador ("Body", "Arm", "Eye", "Mouth", o null si no evolucionó) |
| `EvolvedUnitId` | `string` | UniqueID de la criatura ganadora que evolucionó ("" si none) |
| `EvolvedUnitName` | `string` | Nombre de la criatura que evolucionó |
| `DiedUnitId` | `string` | **S37** UniqueID de la criatura perdedora que murió ("" si none) |
| `DiedUnitName` | `string` | **S37** Nombre de la criatura perdedora que murió |
| `TeamA` | `List<CombatFighterSnapshot>` | **S37** Snapshots post-fight de todos los units de Team A (índice = unit index) |
| `TeamB` | `List<CombatFighterSnapshot>` | **S37** Snapshots post-fight de todos los units de Team B (índice = unit index) |
| `Log` | `List<string>` | Debug log (round-by-round, rolls, daño, stuns, sinergias, evolución, muerte) |
| `Turns` | `List<CombatTurn>` | **S37** Turn-by-turn estructurado (atacante index, defensor index, daño, crit, HP/Shield resultante, estado de equipo) |

## CombatFighterSnapshot — S33 + S34 + S37

**Estructura idéntica a CombatRecord.CombatFighterSnapshot:**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `MaxHp` | `float` | HP máximo post-equipment |
| `Attack` | `float` | ATK final post-equipment |
| `Speed` | `float` | SPD final (base, no dinámico) |
| `Defense` | `float` | DEF final post-equipment |
| `Luck` | `float` | LCK final |
| `Evasion` | `float` | EVA final |
| `BodyTier` | `int` | **S34** Tier del Body shape (0 = pre-S34, 1..max) |
| `ArmTier` | `int` | **S34** Tier del Arm |
| `EyeTier` | `int` | **S34** Tier del Eye |
| `MouthTier` | `int` | **S34** Tier del Mouth |
| `ColorHex` | `string` | **S34** Color base en hex RGB (RRGGBB), "" = fallback |
| `Name` | `string` | **S37** Nombre de la criatura |
| `Role` | `Role` | **S37** Rol de combate (Protector, Agresivo, Empático) |
| `Row` | `int` | **S37** Fila que ocupaba (0=Front, 1=Mid, 2=Back) |

**Poblado en SimulateCore():** Antes de iterar rounds, para cada Combatant se extrae via helper `Snapshot()` sus stats finales + tiers + color + nombre + rol + row.

## Flujo de Construcción (S37)

1. `CombatService.Simulate(idsA, idsB, rowsA, rowsB, registry, db, config, equipDb, seed)` valida equipos
2. Llama `SimulateCore(dnasA, dnasB, resolvedRowsA, resolvedRowsB, db, config, equipDb, rng)`
3. `SimulateCore()` instancia `CombatResult`, construye `Combatant` list para cada equipo
4. **Popula snapshots:** `result.TeamA = dnasA.Select(Snapshot(c))`, idem TeamB
5. Itera rounds: cada turno llama `TakeTurn()` sobre attackers de ambos equipos (en orden de EffSpeed)
6. Al terminar: elige 1 ganador (uniform de equipo ganador) → `CombatEvolution.TryEvolveRandomSlot` → elige 1 perdedor (uniform) → `DeathChance` roll
7. Popula `TeamAWon`, `IsDraw`, `EvolvedSlot`, `EvolvedUnitId`, `EvolvedUnitName`, `DiedUnitId`, `DiedUnitName`, `Log`
8. Retorna `result`
9. Caller (`Simulate()`) llama `BuildRecord()` para cada unit de cada equipo (persiste en `CreatureDNA.CombatHistory`)

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Summary` | `string` (property) | **S37** Resumen 1-liner: "DRAW —" o "Winner: Team {A\|B} [EVOLVED {Unit} {Slot}] [DIED {Unit}]" |

## Serialización

JSON con Newtonsoft.Json. PascalCase campo names (match Cloud Code JS v2 emits). `CombatFighterSnapshot` es `[Serializable]` puro. Enums (`Role`) se serializan como int. Todos los lists se serializan normal.

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatService]] — genera en `SimulateCore()`, popula `TeamA/TeamB` via `Snapshot(Combatant)`
- [[CombatRecord]] — copia snapshots vía `BuildRecord()`
- [[CombatTurn]], [[CombatProcEvent]], [[CombatUnitState]] — nested structures
- [[CombatFighterSnapshot]] — S33/S34/S37
- [[AsyncCombatService]] — deserializa result homólogo desde Cloud Code JS v2
- [[RoleTableSO]] — Role enum, perfiles aplicados en Combatant

## Conexiones

**Entrada:**
- `CombatService.SimulateCore(dnasA, dnasB, rowsA, rowsB, db, config, equipDb, rng)` → retorna `CombatResult`

**Salida:**
- `CombatService.Simulate()` → `BuildRecord()` para cada unit → `CombatRecord.SelfTeam/SelfTeamIds` (S37) + `CombatRecord.SelfStats` (backward compat) → persistencia + display
- `CombatController` → dispatch a `UIManager` (panel visual, notifs)
- Async: `CloudMatchBlob` emitida por JS v2 contiene idéntica estructura `CombatResult` (3v3) → ambos clientes deserializan

## Notas

- **Transiente:** Solo vive durante la simulación y el ensamblaje de records. No persiste directo.
- **S37 Teams:** Reshape de 1v1 a 3v3 es aditivo en persistencia (BuildRecord copia snapshots + guarda nuevos campos SelfTeam/SelfTeamIds para registros 3v3; backward compat para 1v1 legacy)
- **Log:** Contiene traces detalladas (round-by-round, rolls, evasiones, statuses, sinergias, evolución, muerte, efectos de rol)
- **Turns:** Simétrico — ambos equipos (async) ven idéntica secuencia de `CombatTurn` con índices y estados de equipo
- **Unit index:** Snapshots en TeamA/TeamB están indexadas por orden de Combatant en la lista (0..2), matching AttackerIndex/DefenderIndex en turns
- **Rol metadata:** Role incluido en snapshot para UI display (chip de rol en card)
