---
tags: [script, combat-prototype, visuals]
---

# ResolutionAnimator.cs

**Ruta:** `Systems/CombatPrototype/ResolutionAnimator.cs`

**Responsabilidad:** Corrutina que reproduce ResolutionEvents sobre vistas. Agrupa por Wave, ejecuta movimientos en paralelo (Move/Reaction/Push/Launch/Land durations diferentes), secuencialmente Hits/Deaths.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[ResolutionEvent]], [[CombatUnitView]], [[CombatBoard]], [[PlanExecutor]], [[EnemyTurnController]]
