---
tags: [script, combat-prototype, orchestration]
---

# CombatPrototypeManager.cs

**Ruta:** `Systems/CombatPrototype/CombatPrototypeManager.cs`

**Responsabilidad:** Orquestador principal del combate prototipo. Fases: Planning → Executing → EnemyTurn (ciclo). **Novedades S82:** spawns enemigos con facing del layout (GetEnemySpawnsWithFacing); guard presupuesto (MaxActions = 2) en ConfirmAction. RefreshProjection calcula proyección y pinta intents de enemigos.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatBoardBuilder]], [[BoardLayoutSO]], [[TargetingController]], [[PlanExecutor]], [[EnemyTurnController]], [[CombatPrototypeHUD]], [[PlayerUnitDefinitionSO]], [[EnemyDefinitionSO]]
