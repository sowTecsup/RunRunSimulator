---
tags: [script, combat-prototype, orchestration]
---

# EnemyTurnController.cs

**Ruta:** `Systems/CombatPrototype/EnemyTurnController.cs`

**Responsabilidad:** Ejecuta fases post-turno de jugador (reacciones y ataque). `CommitIntents(state)` computa intenciones de enemigos vivos (EnemyBrain). `PaintIntents(highlighter)` visualiza ataques planeados en celdas Telegraph. `RunTurn(state, views, board, onComplete)` **cierre de ciclo**: resuelve con ActionResolver.ResolveEnemyTurn (aterrizajes + ataques + movimientos) y anima. `RunReactions(state, views, board, onComplete)` **nuevo S88**, gemelo sin ataques: resuelve con ActionResolver.ResolveEnemyReactions (solo aterrizajes y movimientos post-golpe para turnos 1-2 del ciclo). Ambos animan con ResolutionAnimator y llaman callback al completar.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatSimState]], [[EnemyUnit]], [[EnemyBrain]], [[ActionResolver]], [[ResolutionAnimator]], [[BoardHighlighter]], [[CombatPrototypeManager]]
