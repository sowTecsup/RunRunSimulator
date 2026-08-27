---
tags: [script, combat-prototype, logic]
---

# ActionResolver.cs

**Ruta:** `CombatPrototype/ActionResolver.cs`

**Responsabilidad:** Ejecutor de acciones y turnos de enemigos. `ResolveBeat()` ejecuta acciones de jugador: viaja al aterrizaje (Move event), golpea en anclaje, fizzle si aterrizaje ocupado. `ResolveEnemyTurn()` ataca todos (onda 1+i) con AttackDirection del Facing + offsets de Pattern; **S84: movimiento final solo-si-golpeado** (WasHitThisTurn), intenta offsets, si bloqueado → rota 90° horario con evento Rotate. **S84 NUEVO:** `ResolveGermination(state)` mata todos los enemigos vivos cuando germina la semilla (turno de victoria). `ResolveAttack()` emite evento Impact (onda 0) con Cells = celdas de plantilla. Genera `ResolutionEvent` por cada mutación.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[PlannedAction]], [[CombatSimState]], [[CombatAbilitySO]], [[AbilityTargeting]], [[CombatEffects]], [[ResolutionEvent]], [[EnemyUnit]], [[SeedUnit]]
