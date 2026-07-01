---
tags: [combat, data, persistence, history]
---

# CombatRecord

DTO [Serializable] que almacena el historial completo de una pelea terminada, persistido en `CreatureDNA.CombatHistory`. Es un record simétrico (los mismos turnos para ambos luchadores), estructurado turno-a-turno para replay local determinista. Serializa como JSON en cloud y almacenamiento local.

## Responsabilidad

Peristir el resultado y la secuencia de turnos de un combate completado, de forma que el visualizador local pueda reproducir el replay sin recomputar. Ambos motores (C# local y servidor JS) emiten la misma forma, pero solo el servidor es autoritario; el cliente lee y almacena.

## Campos Públicos

### CombatRecord

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `OpponentName` | `string` | Nombre customizado de la criatura rival |
| `OpponentPlayerName` | `string` | Nombre del jugador rival (async only; "" para local) |
| `Date` | `DateTime` | Cuándo ocurrió (UTC) |
| `Outcome` | `CombatOutcome` | Resultado desde POV de THIS creature (Won/Lost/Draw) |
| `Died` | `bool` | Si esta criatura murió en la pelea |
| `EvolvedSlot` | `string` | Slot que evolucionó si ganó (null si no evolucionó o perdió) |
| `SelfWasA` | `bool` | Si true, esta criatura era combatante A; false = era B |
| `Turns` | `List<CombatTurn>` | Turnos de la pelea en orden |

## Método Implícito

| Método | Retorna |
|--------|---------|
| (serialización JSON) | El objeto serializa como string para persistencia |

## Vinculado a

- [[CreatureDNA]] — `CombatHistory` es `List<CombatRecord>`
- [[CombatService]] — emite via `RecordHistory()` (privado)
- [[CombatVisualizerService]] — lee records para replay
- [[CombatTurn]] — estructura interna

## Conexiones

**Entrada:**
- `CombatService.RecordHistory()` — crea e inserta en `dna.CombatHistory`

**Salida:**
- `CombatVisualizerService.Play(self, opponent, record)` — ingiere record para construir replay
- Persistencia via `GameManager` (Cloud Save) cuando `GameEvents.OnRegistryChanged` se dispara

## Notas sobre Cambios (S31)

**MODIFICADO:** `CombatTurn` gana dos campos nuevos:
- `bool NoAttack` — true si el turno no tuvo golpe (stun-skip, muerte antes de pegar)
- `List<CombatProcEvent> Procs` — lista de eventos de proc en orden (antes/después golpe)

Backward compatible: records viejos sin estos campos deserializan con lista vacía y NoAttack=false.

---

# CombatTurn

DTO [Serializable] que representa un único ataque dentro de una pelea.

## Campos Públicos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `TurnNumber` | `int` | Número de turno (1-indexed) |
| `AttackerName` | `string` | Nombre del atacante |
| `DefenderName` | `string` | Nombre del defensor |
| `AttackerIsA` | `bool` | Si true, atacante es combatante A |
| `Damage` | `float` | Daño infligido (0 si fue dodgeado) |
| `WasCrit` | `bool` | Si fue crítico |
| `DefenderHpAfter` | `float` | HP resultante del defensor tras golpe |
| `NoAttack` | `bool` | **NUEVO S31** Si true, no hubo golpe (stun-skip o muerte por aflicción) |
| `Procs` | `List<CombatProcEvent>` | **NUEVO S31** Eventos de proc en este turno, en orden |

## Método Implícito

| Método | Retorna |
|--------|---------|
| (serialización JSON) | El objeto serializa como parte de `CombatRecord.Turns` |

## Vinculado a

- [[CombatRecord]] — `CombatRecord.Turns` es `List<CombatTurn>`
- [[CombatProcEvent]] — `Procs` es `List<CombatProcEvent>`
- [[CombatService]] — emite via `EmitTurn()` (privado)
- [[CombatVisualizerService]] — lee para construir replay

## Conexiones

**Entrada:**
- `CombatService.EmitTurn()` — crea turn, llena campos, inserta en `result.Turns`
- `CombatService.TakeTurn()` — acumula procs en `Resolver.TurnProcs` antes de emitir

**Salida:**
- `CombatVisualizerService.BuildStates()` — itera `Turn.Procs` y `Turn.NoAttack`
- `CombatVisualizerService.ForwardRoutine()` — anima turnos basándose en fields

## Notas sobre Campos Nuevos

- `NoAttack` sirve para los visualizador saltar `FireWindup`/`FireImpact` si no hay ataque
- `Procs` contiene todos los procs del turno con timestamp `BeforeStrike` para ordenar animación
- Si `Procs == null`, visualizador trata como lista vacía (backward compat)
