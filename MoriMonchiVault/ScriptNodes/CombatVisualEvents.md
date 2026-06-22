---
tags: [script, combat]
---

# CombatVisualEvents.cs

**Ruta:** `Systems/CombatVisualizer/CombatVisualEvents.cs`

**Responsabilidad:** Bus de eventos estático para la visualización de combate. Define enumeraciones (`CombatVisualSide`, `CombatVisualContext`, `CombatVisualHit`) y métodos para disparar eventos en cada fase del combate (inicio, turno, ataque, daño, crítico, muerte, fin).

**Vinculado a:** [[Index/03 - Combat]]

**Conexiones:** [[CombatVisualizerService]], [[CombatVisualHooks]], [[CombatVisualizerPanelUITK]], [[CombatHpBarUITK]], [[CombatRecord]], [[CombatTurn]], [[CreatureDNA]]
