---
tags: [script, combat-prototype, orchestration]
---

# PlanExecutor.cs

**Ruta:** `CombatPrototype/PlanExecutor.cs`

**Responsabilidad:** Corrutina que ejecuta Choreography sobre estado canónico: itera Beats en secuencia, resuelve cada uno con `ActionResolver.ResolveBeat()`, anima eventos en paralelo con `ResolutionAnimator.Play()`, espera a que termine la onda, luego siguiente beat. Callback `onComplete(allEvents)` al terminar ejecución completa. Orquesta el flujo de resolución en tiempo real.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[Choreography]], [[CombatSimState]], [[ActionResolver]], [[ResolutionAnimator]], [[CombatPrototypeManager]], [[CombatUnitView]]
