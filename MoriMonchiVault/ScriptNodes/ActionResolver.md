---
tags: [script, combat-prototype, logic]
---

# ActionResolver.cs

**Ruta:** `Systems/CombatPrototype/ActionResolver.cs`

**Responsabilidad:** Ejecutor de acciones y turnos de enemigos. **Cambios S82:** ResolvePlayerAction: viaja al aterrizaje (Move event), golpea en anclaje, fizzle si aterrizaje ocupado. ResolveBeat no tiene reacciones. ResolveEnemyTurn: todos atacan siempre (onda 1+i) con AttackDirection del Facing + offsets de Pattern; fase final TryEndOfTurnMove (ajedrez de golpeados: intenta offsets, si bloqueado → Rotate 90° horario). Elimina eventos de reacción antiguo. **Cambios S83:** ResolveAttack emite evento `Impact` (onda 0, sin mover unidad) con Cells = celdas de plantilla calculadas por GetAffectedCells; se dispara siempre que no haya fizzle, independientemente de si hay víctimas.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[PlannedAction]], [[CombatSimState]], [[CombatAbilitySO]], [[AbilityTargeting]], [[CombatEffects]], [[ResolutionEvent]], [[EnemyUnit]]
