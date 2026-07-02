---
tags: [combat, data, mutable-state]
---

# Combatant

**Ruta:** `Systems/Combat/Combatant.cs`

**Responsabilidad:** Modelo mutable de un combatiente *durante* la simulación. Almacena snapshot de DNA, stats finales (después de equipment), HP presente, stun/immunity counters, y lista de procs/efectos activos.

## Estructura

### Combatant (clase pública)

**Campos públicos:**

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Dna` | `CreatureDNA` | Referencia a la criatura (se mutará si gana/muere) |
| `Name` | `string` | Nombre para logging |
| `IsA` | `bool` | Si true, es combatante A (vs B) |
| `Hp` | `float` | HP actual durante combate |
| `MaxHp` | `float` | HP máximo = Constitution * BaseHpCombatMultiplier |
| `Attack` | `float` | Ataque total (base + equipment) |
| `Speed` | `float` | Velocidad total (base + equipment) |
| `Defense` | `float` | Defensa total |
| `Luck` | `float` | Suerte total |
| `Evasion` | `float` | Evasión total |
| `StunTurns` | `int` | Turnos de stun activos (decrementa cada turno) |
| `StunImmunityTurns` | `int` | Turnos de inmunidad a stun post-despertar (decrementa) |
| `Procs` | `List<CombatProcEffect>` | Todos los procs del equipment equipado |
| `Active` | `List<ActiveEffect>` | Estados en curso (Poison, Burn, Regen, etc.) |

### ActiveEffect (clase interna)

Estructura de un status activo durante combate.

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Kind` | `ModifierEffectKind` | Tipo (Poison, Burn, Regen, etc.) |
| `RemainingTurns` | `int` | Turnos restantes (decrementa) |
| `Magnitude` | `int` | Daño/curación por turno |

## Ciclo de Vida

1. `CombatService.BuildCombatant()` — crea instancia, carga DNA y equipment
2. `CombatService.SimulateCore()` — pasa A y B a `TakeTurn()`
3. `TakeTurn()` muta: `Hp`, `StunTurns`, `StunImmunityTurns`, `Active`
4. Final de combate: si ganó/perdió, las mutaciones vuelven al DNA persistente via `CombatEvolution.AdvanceTier()` u otros

## Vinculado a

- [[Index/03 - Combat]]
- [[CombatService]] — construye y muta durante simulación
- [[CombatResolver]] — accede a Self/Opponent para aplicar acciones
- [[ActiveEffect]] — lista de efectos activos

## Conexiones

**Entrada:**
- `CombatService.BuildCombatant(dna, db, equipDb, isA)` — crea instancia

**Salida:**
- Cambios en `Hp`, stun counters, `Active` vía métodos como `DamageOpponent()` (in `CombatResolver`)

## Notas

- No se serializa; es exclusivamente un modelo de simulación en tiempo real.
- Su `Dna` apunta a la misma criatura que en la registry, y se mutará solo si el combate modifica tiers/muerte.
- `StunImmunityTurns` implementa anti-permastun (ver `CombatManagerSO.StunImmunityTurns`).
