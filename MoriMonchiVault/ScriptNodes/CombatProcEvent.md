---
tags: [combat, data, dto, procs]
---

# CombatProcEvent

DTO [Serializable] que registra un evento de proc dentro de un turno de combate. Captura la magnitud, el objetivo afectado y el timing (antes o después del golpe), junto con el estado HP resultante del objetivo para la replay del visualizador.

## Responsabilidad

Transportar datos de un proc ejecutado durante `CombatService.TakeTurn()` → `CombatRecord.CombatTurn.Procs`. Fuente de verdad única para la visualización replay: el visualizador lee este DTO (nunca recomputa).

## Campos Públicos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Kind` | `ModifierEffectKind` | Tipo de efecto: ReturnDamage, Heal, Poison, Burn, Stun, Regen |
| `TargetIsA` | `bool` | Si true, el objetivo es combatante A; false = combatante B |
| `Amount` | `float` | Magnitud: daño, curación, turnos de stun, etc. |
| `TargetHpAfter` | `float` | HP absoluto del objetivo tras aplicar el proc (para graficar) |
| `BeforeStrike` | `bool` | Si true, el proc ocurrió en fase pre-ataque; false = on-connect (post-golpe) |

## Métodos

N/A (DTO puro, sin lógica)

## Vinculado a

- [[CombatRecord]] — `CombatTurn.Procs` es `List<CombatProcEvent>`
- [[CombatService]] — emite los eventos via `Resolver.Record()`
- [[CombatVisualizerService]] — consume para animar procs por turno

## Conexiones

**Entrada:**
- `CombatService.Resolver.Record()` — crea e inserta en `TurnProcs`

**Salida:**
- `CombatVisualizerService.BuildStates()` — lee `Turn.Procs` y aplica visualmente
- `CombatVisualizerService.PlayProc()` — anima un proc y rasura popup
- `CombatDamageNumbers` — suscriptor de `CombatVisualEvents.OnPopup` (indirecto vía visualizador)

## Notas

- Backward compatible: registros viejos sin procs deserializan como `List<CombatProcEvent> = []`
- Orden dentro de `Procs` es significativo: la replay los anima en secuencia
- El `Amount` de un Stun es la cantidad de turnos (flotante casteable a int en display)
