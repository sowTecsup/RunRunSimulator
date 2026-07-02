---
tags: [combat, data, persistence, history]
---

# CombatRecord

**Ruta:** `Data/Combat/CombatRecord.cs`

**Responsabilidad:** DTO serializable que persiste el historial completo y replayable de una pelea terminada. Vive en `CreatureDNA.CombatHistory` y se sincroniza via Cloud Save. Almacena turno-a-turno para que el visualizador local reproduzca el combate sin recomputar.

## Descripción General (S32)

Un único motor C# seeded: el servidor (JS) ya no simula combates, solo proporciona seed + snapshots DNA de ambos combatientes. Ambos clientes corren `CombatService.SimulateCore` con idéntica seed y snapshots → idéntico resultado → idéntico record. El visualizador es 100% local y determinista.

## Campos Públicos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `OpponentName` | `string` | Nombre customizado de la criatura rival |
| `OpponentPlayerName` | `string` | Nombre del jugador rival (async only; "" para local) |
| `Date` | `DateTime` | Cuándo ocurrió (UTC) |
| `Outcome` | `CombatOutcome` | Resultado desde POV de THIS creature (Won/Lost/Draw) |
| `Died` | `bool` | Si esta criatura murió en la pelea |
| `EvolvedSlot` | `string` | Slot que evolucionó si ganó (null si perdió o no evolucionó) |
| `Seed` | `int` | **NUEVO S32** Seed usado para SimulateCore (reproducibilidad) |
| `OpponentDnaId` | `string` | **NUEVO S32** UniqueID del rival (para búsqueda en async) |
| `OpponentPlayerId` | `string` | **NUEVO S32** Player ID del rival (async only; "" para local) |
| `SelfWasA` | `bool` | Si true, esta criatura era combatante A; false = era B |
| `Turns` | `List<CombatTurn>` | Turnos de la pelea en orden |

## CombatTurn

Estructura interna que representa un único ataque dentro de un combate.

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

## Backward Compatibility (S32)

Los tres campos nuevos (`Seed`, `OpponentDnaId`, `OpponentPlayerId`) son aditivos:
- Old records sin estos campos deserializan OK (valores por defecto: 0, "", "").
- Visulizador tolera registros viejos sin Seed (no puede verificar determinismo, pero replay funciona).
- Búsqueda async sin OpponentDnaId falla silenciosamente; nuevo código lo llena.

## Serialización

JSON con Newtonsoft.Json, `StringEnumConverter` para enums (`Outcome`). `Date` se serializa como ISO-8601 UTC.

## Vinculado a

- [[Index/03 - Combat]]
- [[CreatureDNA]] — `CombatHistory` es `List<CombatRecord>`
- [[CombatService]] — construye via `BuildRecord(result, self, opponent, selfWasA, oppPlayerName, oppPlayerId, seed, date)`
- [[AsyncCombatService]] — popula desde `CloudMatchBlob`; lee y aplica
- [[CombatVisualizerService]] — lee records para replay local
- [[CombatTurn]] — estructura interna

## Conexiones

**Entrada:**
- `CombatService.Simulate()` → construye 2× `BuildRecord()` (uno por combatiente, perspectivas opuestas)
- `AsyncCombatService.ApplyResult()` → construye via `CombatService.BuildRecord()`

**Salida:**
- Persistencia via `GameManager.SaveDatabase()` cuando `GameEvents.OnRegistryChanged` se dispara
- `CombatVisualizerService.Play(self, opponent, record)` — ingiere record, construye estados, anima

## Notas

- Historial simétrico: ambos combatientes guardan los mismos turnos (mismo ataque, mismo defensor).
- `SelfWasA` dice al visualizador si "A" = "yo" o "ellos" en los turnos grabados.
- En async, `Seed` + `OpponentDnaId` + `OpponentPlayerId` permiten reproducir y verificar la pelea.
- Replay es puro local; no requiere servidor ni cómputo.
