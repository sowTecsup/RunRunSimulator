---
tags: [script, combat-prototype, orchestration]
---

# PlanExecutor.cs

**Ruta:** `Systems/CombatPrototype/PlanExecutor.cs`

**Responsabilidad:** Corrutina que ejecuta Choreography sobre estado canónico: itera Beats, resuelve cada uno con ActionResolver.ResolveBeat, anima eventos con ResolutionAnimator. Callback al completar.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[Choreography]], [[CombatSimState]], [[ActionResolver]], [[ResolutionAnimator]], [[CombatPrototypeManager]]
