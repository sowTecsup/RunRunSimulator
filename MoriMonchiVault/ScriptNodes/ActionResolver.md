---
tags: [script, combat-prototype, logic]
---

# ActionResolver.cs

**Ruta:** `Systems/CombatPrototype/ActionResolver.cs`

**Responsabilidad:** Lógica pura de resolución. Entrada: estado canónico + Beat/EnemyTurn. Salida: lista ResolutionEvents (con Wave grouping). ResolveBeat: ejecuta acciones jugador, aplica efectos, reacciones enemigo. ResolveEnemyTurn: movimiento/ataque de enemigos.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatSimState]], [[Beat]], [[PlannedAction]], [[ResolutionEvent]], [[CombatEffects]], [[AbilityTargeting]], [[EnemyIntent]], [[PlanProjection]], [[PlanExecutor]]
