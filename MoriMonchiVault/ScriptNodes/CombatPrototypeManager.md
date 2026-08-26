---
tags: [script, combat-prototype, orchestration]
---

# CombatPrototypeManager.cs

**Ruta:** `Systems/CombatPrototype/CombatPrototypeManager.cs`

**Responsabilidad:** Núcleo delgado que orquesta fase: Planning → Executing → EnemyTurn → loop o Victory/Defeat. Respons: spawn unidades (Canonical state), mantener Plan (Choreography), RefreshProjection (PlanProjection), transiciones (ExecutePlan, callbacks).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatSimState]], [[Choreography]], [[PlanProjection]], [[TargetingController]], [[PlanExecutor]], [[EnemyTurnController]], [[CombatPrototypeHUD]], [[EnemyBriefPanel]]
