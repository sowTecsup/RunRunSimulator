---
tags: [script, ui]
---

# CombatVisualizerPanelUITK.cs

**Ruta:** `UI/CombatVisualizerPanelUITK.cs`

**Responsabilidad:** Panel UITK que muestra turno actual/total y log deslizante de acciones durante el combate. Se suscribe a `CombatVisualEvents` (inicio, fin, turno, log) y actualiza `turn-label` y `log-container`. Limite de líneas de log configurable.

**Vinculado a:** [[Index/03 - Combat]]

**Conexiones:** [[CombatVisualEvents]], [[CombatVisualContext]]
