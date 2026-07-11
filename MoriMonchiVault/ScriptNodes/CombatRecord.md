---
tags: [combat, data, persistence, history]
---

# CombatRecord

**Ruta:** `Data/Combat/CombatRecord.cs`

**Responsabilidad:** DTO serializable que persiste el historial completo y replayable de una pelea terminada. Vive en `CreatureDNA.CombatHistory` y se sincroniza via Cloud Save. Almacena turno-a-turno para que el visualizador local reproduzca el combate sin recomputar. **S37:** Campos aditivos para equipos (SelfTeam/OpponentTeam/SelfTeamIds/OpponentTeamIds); backward compatible con 1v1 legacy records.

## Descripción General (S32 + S37)

Un único motor C# seeded: el servidor (JS) proporciona seed + snapshots DNA de todos los combatientes. Ambos clientes corren `CombatService.SimulateCore` con idéntica seed y snapshots → idéntico resultado → idéntico record. El visualizador es 100% local y determinista. **S37:** Expandido a 3v3 teams (1..3 por lado), order de rolls por EffSpeed, efectos de rol (escudo, backline, heal).

## Cambios S33

**Nuevos campos:** `SelfStats` y `OpponentStats` (tipo `CombatFighterSnapshot`) capturan stats **post-equipment** de ambos luchadores en el momento del combate. Usado en tab Combate de detail panel para mostrar efectos reales de equipo. Records viejos (pre-S33) tienen estos campos `null` — backward compatible.

## Cambios S34

**CombatTurn:** Nuevos campos `StatusA` y `StatusB` (listas de `CombatStatusMark`) registran el estado de efectos activos en ambos luchadores al cierre de cada turno. Poblado por `CombatService.StatusMarks()`. Records viejos deserializan listas vacías.

**CombatFighterSnapshot:** Nuevos campos (`BodyTier`, `ArmTier`, `EyeTier`, `MouthTier`, `ColorHex`, `Name`, `Role`, `Row`) capturan el tier de evolución, color base, nombre, rol y fila en el momento del combate. Usado por UI para renderizar visualización compacta.

## Cambios S37

**NUEVOS campos aditivos:**
- `SelfTeam` | `List<CombatFighterSnapshot>` — snapshots de todos los units del equipo propio en 3v3 (null en 1v1 legacy)
- `OpponentTeam` | `List<CombatFighterSnapshot>` — snapshots de todos los units del equipo rival (null en 1v1 legacy)
- `SelfTeamIds` | `List<string>` — UniqueIDs de las criaturas del equipo propio (índice = unit index matching snapshots)
- `OpponentTeamIds` | `List<string>` — UniqueIDs de las criaturas del equipo rival

**Compatibilidad:** Records 3v3 nuevos tienen SelfTeam/OpponentTeam != null (y SelfTeamIds/OpponentTeamIds también). Records 1v1 legacy tienen estos campos null; `SelfStats`/`OpponentStats` seguirán siendo el snapshot único (backward compat).

**Determinismo por equipo:** Snapshots alineados con índices en `CombatTurn.AttackerIndex`/`DefenderIndex` (0..2 por equipo).

## Campos Públicos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `OpponentName` | `string` | Nombre de la criatura rival (para 1v1 display) |
| `OpponentPlayerName` | `string` | Nombre del jugador rival (async only; "" para local) |
| `Date` | `DateTime` | Cuándo ocurrió (UTC) |
| `Outcome` | `CombatOutcome` | Resultado desde POV de THIS creature (Won/Lost/Draw) |
| `Died` | `bool` | Si esta criatura murió en la pelea |
| `EvolvedSlot` | `string` | Slot que evolucionó si ganó (null si perdió o no evolucionó) |
| `SelfStats` | `CombatFighterSnapshot` | **S33** Stats post-equipment de THIS fighter (1v1 legacy) o null (3v3) |
| `OpponentStats` | `CombatFighterSnapshot` | **S33** Stats post-equipment del rival (1v1 legacy) o null (3v3) |
| `Seed` | `int` | Seed usado para SimulateCore (reproducibilidad) |
| `OpponentDnaId` | `string` | UniqueID del rival (para búsqueda en async, 1v1 legacy) |
| `OpponentPlayerId` | `string` | Player ID del rival (async only; "" para local) |
| `SelfWasA` | `bool` | Si true, esta criatura era combatante A (1v1) o está en índice en SelfTeam (3v3) |
| `Turns` | `List<CombatTurn>` | Turnos de la pelea en orden |
| `SelfTeam` | `List<CombatFighterSnapshot>` | **S37** Snapshots de todo el equipo propio (null en 1v1 legacy) |
| `OpponentTeam` | `List<CombatFighterSnapshot>` | **S37** Snapshots de todo el equipo rival (null en 1v1 legacy) |
| `SelfTeamIds` | `List<string>` | **S37** UniqueIDs del equipo propio en orden (null en 1v1 legacy) |
| `OpponentTeamIds` | `List<string>` | **S37** UniqueIDs del equipo rival en orden (null en 1v1 legacy) |

## CombatFighterSnapshot — S33 + S34 + S37

**Estructura:** Captura los stats **finales** (post-equipment), tiers de evolución, identidad y rol de un luchador en el momento exacto que comenzó el combate.

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
| `Name` | `string` | **S37** Nombre de la criatura (display en card/replay) |
| `Role` | `Role` | **S37** Rol de combate (Protector, Agresivo, Empático) |
| `Row` | `int` | **S37** Fila que ocupaba (0=Front, 1=Mid, 2=Back) |

**Creación:** En `CombatService.SimulateCore()`, antes de iterar rounds:
```csharp
result.TeamA = dnasA.Select(dna => {
    var c = BuildCombatant(dna, ...);
    return Snapshot(c);
}).ToList();
result.TeamB = dnasB.Select(...).ToList();
```

El helper `Snapshot()` extrae tiers, color, nombre, rol, row de cada Combatant.

**Backward compat:** Records viejos con SelfTeam = null siguen funcionando (mostrar SelfStats/OpponentStats en UI); CombatTurn.TeamStateA/TeamStateB también null (skip equipo states en visualización).

## CombatTurn — S34 + S37

Estructura interna que representa un único ataque dentro de un combate.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `TurnNumber` | `int` | Número de turno (1-indexed) |
| `AttackerName` | `string` | Nombre del atacante |
| `DefenderName` | `string` | Nombre del defensor |
| `AttackerIsA` | `bool` | **S37** (deprecated) Si true, atacante es del lado A (1v1 only) |
| `Damage` | `float` | Daño infligido (0 si fue dodgeado) |
| `WasCrit` | `bool` | Si fue crítico |
| `DefenderHpAfter` | `float` | HP resultante del defensor tras golpe |
| `NoAttack` | `bool` | Si true, no hubo golpe (stun-skip, muerte por aflicción) |
| `Procs` | `List<CombatProcEvent>` | Eventos de proc en este turno, en orden |
| `StatusA` | `List<CombatStatusMark>` | **S34** Marcas de estado activo tras este turno (1v1 only; 3v3 usa TeamStateA) |
| `StatusB` | `List<CombatStatusMark>` | **S34** Marcas de estado activo tras este turno (1v1 only; 3v3 usa TeamStateB) |
| `AttackerIndex` | `int` | **S37** Índice del atacante dentro su equipo (0..2) |
| `DefenderIndex` | `int` | **S37** Índice del defensor dentro su equipo (0..2) |
| `DefenderShieldAfter` | `float` | **S37** Escudo resultante del defensor tras golpe (Shield pool del defensor) |
| `TeamStateA` | `List<CombatUnitState>` | **S37** Estado completo de todos los units del team A tras este turno (null en 1v1) |
| `TeamStateB` | `List<CombatUnitState>` | **S37** Estado completo de todos los units del team B tras este turno (null en 1v1) |

## CombatStatusMark — S34

**Estructura:** Registra un único tipo de estado activo y su conteo de stacks.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Kind` | `ModifierEffectKind` | Tipo de efecto (Poison, Burn, Regen, Stun, etc.) |
| `Stacks` | `int` | Número de stacks activos (1+ = al menos una instancia activa) |

**Creación:** `CombatService.StatusMarks(Combatant c)` itera los efectos activos, cuenta por `Kind`, retorna listas de marks.

**Consumo:** Usado en 1v1 legacy records (S34+); 3v3 records (S37+) usan TeamStateA/B en su lugar.

## CombatUnitState — S37

**Estructura:** Estado completo de UN unit al cierre de cada turno, para 3v3 replay.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Hp` | `float` | HP actual del unit |
| `Shield` | `float` | Escudo actual del unit (Protector role) |
| `Marks` | `List<CombatStatusMark>` | Efectos activos en el unit (Poison, Burn, etc.) |

**Creación:** En `CombatService.EmitTurn()`, tras resolver el turno, se copian estados de ambos equipos a `CombatTurn.TeamStateA/TeamStateB` (indices 0..team.Count-1).

**Consumo:** Visualizador 3v3 (futuro, Fase 4) lee TeamStateA/B para renderizar HP/Shield/efectos en tiempo real.

## Backward Compatibility (S32 + S33 + S34 + S37)

- **1v1 legacy:** SelfStats/OpponentStats != null, SelfTeam/OpponentTeam = null, StatusA/StatusB pobladas, TeamStateA/TeamStateB = null
- **3v3 new:** SelfStats/OpponentStats can be null, SelfTeam/OpponentTeam != null, StatusA/StatusB empty (usa TeamStateA/B), TeamStateA/TeamStateB poblados
- **Transición:** CombatController.SimulateLocal() y AsyncCombatService.ApplyResult() soportan ambos formatos; BuildRecord() produce 3v3 records (S37+)
- Old UI code (1v1) sigue leyendo SelfStats/OpponentStats; visualizador 3v3 (futuro) leerá SelfTeam/OpponentTeam

## Serialización

JSON con Newtonsoft.Json, `StringEnumConverter` para enums. PascalCase campo names (match contrato JS). Lists serializan normal (indices preservados). Backward compat: deserialización NULL-tolera campos faltantes (defaulean a null/empty).

## Vinculado a

- [[Index/03 - Combat]]
- [[CreatureDNA]] — `CombatHistory` es `List<CombatRecord>`
- [[CombatService]] — construye via `BuildRecord()` y popula en `SimulateCore()`, `EmitTurn()`
- [[AsyncCombatService]] — popula desde `CloudMatchBlob` JS v2 (3v3)
- [[CombatVisualizerService]] — lee records para replay local (S38+)
- [[MorimonchiDetailInfoUITK]] — consume CombatRecord en tab Combate (1v1 display via SelfStats)
- [[MoriMonchiCombatVisualizerUITK]] — renderiza efectos vía StatusA/B (1v1) o TeamStateA/B (3v3 futuro)
- [[CombatTurn]], [[CombatProcEvent]], [[CombatStatusMark]], [[CombatUnitState]] — estructuras nested

## Conexiones

**Entrada:**
- `CombatService.SimulateCore()` → genera snapshots via `Snapshot(Combatant)` → copia a result.TeamA/B
- `CombatService.EmitTurn()` → puebla `TeamStateA/TeamStateB` con HP/Shield/Marks de cada unit
- `CombatService.BuildRecord()` → copia snapshots + teamIds a record.SelfTeam/OpponentTeam/SelfTeamIds/OpponentTeamIds
- `AsyncCombatService.ApplyResult()` → construye via `CombatService.BuildRecord()`

**Salida:**
- Persistencia via `GameManager.SaveDatabase()` cuando `GameEvents.OnRegistryChanged`
- `MorimonchiDetailInfoUITK.BuildCombatHistory()` → lee SelfStats (1v1) o SelfTeam[0] (3v3, esta criatura es index en team)
- `CombatReplayRequest.CanReplay()` → valida `Turns != null && Turns.Count > 0`; **S37:** retorna false si SelfTeam != null (visualizador 3v3 en Fase 4)
- Visualizador futuro (Fase 4): lee SelfTeam/OpponentTeam/TeamStateA/B para replay 3v3

## Notas

- **Simétrico:** Ambos combatientes guardan los mismos turnos (mismo ataque, mismo defensor).
- **SelfWasA / SelfTeam index:** En 1v1 legacy, SelfWasA dice al visualizador si "A" = "yo". En 3v3, SelfTeam contiene la posición en el equipo; index de THIS creature en SelfTeamIds/SelfTeam es información display.
- **Equipo Lineup:** S37 copia SelfTeamIds en orden de simulación (índice 0 = first combatant, etc.); snapshot Name/Role/Row incluidos.
- **S37 S34 Tiers + Color:** Snapshot guarda estado visual completo (evolución + color + rol) para renderización offline.
- **EN REVISIÓN (S37):** CanReplay() retorna false para 3v3 records hasta que visualizador 3v3 esté listo (Fase 4). 1v1 records siguen soportados.
