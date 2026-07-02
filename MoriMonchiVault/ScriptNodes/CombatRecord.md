---
tags: [combat, data, persistence, history]
---

# CombatRecord

**Ruta:** `Data/Combat/CombatRecord.cs`

**Responsabilidad:** DTO serializable que persiste el historial completo y replayable de una pelea terminada. Vive en `CreatureDNA.CombatHistory` y se sincroniza via Cloud Save. Almacena turno-a-turno para que el visualizador local reproduzca el combate sin recomputar.

## Descripción General (S32)

Un único motor C# seeded: el servidor (JS) ya no simula combates, solo proporciona seed + snapshots DNA de ambos combatientes. Ambos clientes corren `CombatService.SimulateCore` con idéntica seed y snapshots → idéntico resultado → idéntico record. El visualizador es 100% local y determinista.

## Cambios S33

**Nuevos campos:** `SelfStats` y `OpponentStats` (tipo `CombatFighterSnapshot`) capturan stats **post-equipment** de ambos luchadores en el momento del combate. Usado en tab Combate de detail panel para mostrar efectos reales de equipo. Records viejos (pre-S33) tienen estos campos `null` — backward compatible.

## Cambios S34

**CombatTurn:** Nuevos campos `StatusA` y `StatusB` (listas de `CombatStatusMark`) registran el estado de efectos activos en ambos luchadores al cierre de cada turno. Poblado por `CombatService.StatusMarks()`. Records viejos deserializan listas vacías.

**CombatFighterSnapshot:** Nuevos campos (`BodyTier`, `ArmTier`, `EyeTier`, `MouthTier`, `ColorHex`) capturan el tier de evolución y color base en el momento del combate. Usado por UI para renderizar visualización compacta de stats + evolución en la tarjeta de combate. Valores viejos = 0 (sin tier) o "" (color fallback).

## Campos Públicos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `OpponentName` | `string` | Nombre customizado de la criatura rival |
| `OpponentPlayerName` | `string` | Nombre del jugador rival (async only; "" para local) |
| `Date` | `DateTime` | Cuándo ocurrió (UTC) |
| `Outcome` | `CombatOutcome` | Resultado desde POV de THIS creature (Won/Lost/Draw) |
| `Died` | `bool` | Si esta criatura murió en la pelea |
| `EvolvedSlot` | `string` | Slot que evolucionó si ganó (null si perdió o no evolucionó) |
| `SelfStats` | `CombatFighterSnapshot` | **S33** Stats post-equipment de THIS fighter en el momento del combate (null = record viejo) |
| `OpponentStats` | `CombatFighterSnapshot` | **S33** Stats post-equipment del rival en el momento del combate (null = record viejo) |
| `Seed` | `int` | Seed usado para SimulateCore (reproducibilidad) |
| `OpponentDnaId` | `string` | UniqueID del rival (para búsqueda en async) |
| `OpponentPlayerId` | `string` | Player ID del rival (async only; "" para local) |
| `SelfWasA` | `bool` | Si true, esta criatura era combatante A; false = era B |
| `Turns` | `List<CombatTurn>` | Turnos de la pelea en orden |

## CombatFighterSnapshot — S33 + S34

**Estructura:** Captura los stats **finales** (post-equipment) y tiers de evolución de un luchador en el momento exacto que comenzó el combate.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `MaxHp` | `float` | HP máximo post-equipment |
| `Attack` | `float` | ATK final post-equipment |
| `Speed` | `float` | SPD final |
| `Defense` | `float` | DEF final |
| `Luck` | `float` | LCK final |
| `Evasion` | `float` | EVA final |
| `BodyTier` | `int` | **S34** Tier del Body shape (0 = pre-S34, 1..max = tier de evolución) |
| `ArmTier` | `int` | **S34** Tier del Arm (idem) |
| `EyeTier` | `int` | **S34** Tier del Eye (idem) |
| `MouthTier` | `int` | **S34** Tier del Mouth (idem) |
| `ColorHex` | `string` | **S34** Color base del luchador en hex RGB (RRGGBB), "" = fallback a gris/color base UI |

**Creación:** En `CombatService.SimulateCore()`, antes de iterar rounds:
```csharp
result.StatsA = Snapshot(A);  // Helper Snapshot(Combatant)
result.StatsB = Snapshot(B);
```

El helper `Snapshot()` extrae tiers de `dna.{BodyTier, ArmTier, EyeTier, MouthTier}` y color via `ColorUtility.ToHtmlStringRGB(dna.BaseColor)`.

**Backward compat:** Records viejos con tiers = 0 (default) se renderizan sin chips de tier; ColorHex = "" fallback a color de defensa.

## CombatTurn — S34

Estructura interna que representa un único ataque dentro de un combate. **S34 nota:** StatusA/StatusB reflejan el estado de efectos activos de ambos luchadores tras this turno, antes de pasar al siguiente.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `TurnNumber` | `int` | Número de turno (1-indexed) |
| `AttackerName` | `string` | Nombre del atacante |
| `DefenderName` | `string` | Nombre del defensor |
| `AttackerIsA` | `bool` | Si true, atacante es combatante A |
| `Damage` | `float` | Daño infligido (0 si fue dodgeado) |
| `WasCrit` | `bool` | Si fue crítico |
| `DefenderHpAfter` | `float` | HP resultante del defensor tras golpe |
| `NoAttack` | `bool` | Si true, no hubo golpe (stun-skip, muerte por aflicción) |
| `Procs` | `List<CombatProcEvent>` | Eventos de proc en este turno, en orden |
| `StatusA` | `List<CombatStatusMark>` | **S34** Marcas de estado activo de combatante A tras este turno |
| `StatusB` | `List<CombatStatusMark>` | **S34** Marcas de estado activo de combatante B tras este turno |

## CombatStatusMark — S34

**Nueva estructura:** Registra un único tipo de estado activo y su conteo de stacks.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Kind` | `ModifierEffectKind` | Tipo de efecto (Poison, Burn, Regen, Stun, ReturnDamage, Heal, Synergy, etc.) |
| `Stacks` | `int` | Número de stacks activos (1+ = al menos una instancia activa) |

**Creación:** `CombatService.StatusMarks(Combatant c)` itera los efectos activos (`c.Active` list), cuenta por `Kind` en orden de enum, y añade Stun como mark separado si `c.StunTurns > 0`.

**Consumo:** `CombatVisualizerService.BuildStates()` mapea StatusA/StatusB del record a posición visual (A=self o rival según `SelfWasA`); `MoriMonchiCombatVisualizerUITK.SetStatus()` renderiza chips (iniciales V/Q/R/A/E con stack count).

## Backward Compatibility (S32 + S33 + S34)

- S32 campos (`Seed`, `OpponentDnaId`, `OpponentPlayerId`) son aditivos
- S33 campos (`SelfStats`, `OpponentStats`) son también aditivos
- S34 campos (`CombatTurn.StatusA/B`, `CombatFighterSnapshot.{BodyTier, ArmTier, EyeTier, MouthTier, ColorHex}`) aditivos
- Old records deserializan OK (valores por defecto: null para snapshots, vacías para listas de status, 0 para tiers, "" para ColorHex)
- `MorimonchiDetailInfoUITK.BuildCombatHistory()` detecta `SelfStats == null` y muestra "Combate antiguo — sin stats registradas"
- `CombatVisualizerService` null-tolera StatusA/StatusB al mapear a visual

## Serialización

JSON con Newtonsoft.Json, `StringEnumConverter` para enums (`Outcome`, `ModifierEffectKind`). `Date` se serializa como ISO-8601 UTC. `CombatFighterSnapshot` y `CombatStatusMark` son `[Serializable]` con fields PascalCase para match contrato JS.

## Vinculado a

- [[Index/03 - Combat]]
- [[CreatureDNA]] — `CombatHistory` es `List<CombatRecord>`
- [[CombatService]] — construye via `BuildRecord(result, self, opponent, selfWasA, ...)` y popula `result.StatsA/StatsB` en `SimulateCore()`, `StatusMarks()` en `EmitTurn()`
- [[AsyncCombatService]] — popula desde `CloudMatchBlob`; lee y aplica
- [[CombatVisualizerService]] — lee records para replay local, mapea StatusA/StatusB
- [[MorimonchiDetailInfoUITK]] — consume CombatRecord en tab Combate (BuildCombatHistory), muestra CombatFighterSnapshot stats + tiers
- [[MoriMonchiCombatVisualizerUITK]] — renderiza efectos vía SetStatus(StatusA/StatusB)
- [[CombatTurn]], [[CombatProcEvent]], [[CombatStatusMark]] — estructuras nested
- [[CombatFighterSnapshot]] — S33/S34, snapshot stats + tiers + color

## Conexiones

**Entrada:**
- `CombatService.SimulateCore()` → genera `result.StatsA/StatsB` via `Snapshot(Combatant)`
- `CombatService.EmitTurn()` → puebla `StatusA/StatusB` via `StatusMarks()`
- `CombatService.BuildRecord()` → copia snapshot a `record.SelfStats/OpponentStats`
- `AsyncCombatService.ApplyResult()` → construye via `CombatService.BuildRecord()`

**Salida:**
- Persistencia via `GameManager.SaveDatabase()` cuando `GameEvents.OnRegistryChanged` se dispara
- `MorimonchiDetailInfoUITK.BuildCombatHistory()` → `BuildCombatCard(record)` → `BuildCombatColumn(title, snapshot)` + `AddTierChips(snapshot)`
- `CombatReplayRequest.CanReplay()` → valida `Turns != null && Turns.Count > 0`
- `CombatVisualizerService.Play(self, opponent, record)` — ingiere record, construye estados con tiers/colores de snapshot, anima con StatusA/B en cada turno

## Notas

- **Simétrico:** Ambos combatientes guardan los mismos turnos (mismo ataque, mismo defensor).
- **SelfWasA** dice al visualizador si "A" = "yo" o "ellos" en los turnos grabados.
- **S33 Stats Display:** Tab Combate ahora muestra stats reales de ambos luchadores gracias a CombatFighterSnapshot.
- **S34 Tiers + Color:** Snapshot guarda el estado visual completo (evolución + color) para renderización offline compacta.
- **Backward compat:** Records viejos deserializan OK; StatusA/StatusB vacías renderizadas sin chips; tiers=0 = sin display; ColorHex="" = fallback a gris.
- En async, `Seed` + `OpponentDnaId` + `OpponentPlayerId` + stats + tiers permiten reproducir y verificar la pelea con detalles visuales completos.
