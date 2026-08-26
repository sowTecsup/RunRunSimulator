---
tags: [script, combat-prototype, orchestration]
---

# EnemyTurnController.cs

**Ruta:** `Systems/CombatPrototype/EnemyTurnController.cs`

**Responsabilidad:** Ejecuta fase de turno enemigo: CommitIntents computa intenciones (EnemyBrain), PaintIntents visualiza ataques planeados, RunTurn resuelve con ActionResolver.ResolveEnemyTurn y anima.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatSimState]], [[EnemyUnit]], [[EnemyBrain]], [[ActionResolver]], [[ResolutionAnimator]], [[BoardHighlighter]], [[CombatPrototypeManager]]
