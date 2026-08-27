---
tags: [script, combat-prototype, presentation]
---

# BoardImpactFeedback.cs

**Ruta:** `CombatPrototype/BoardImpactFeedback.cs`

**Responsabilidad:** Proporciona feedback visual/haptic en el tablero ante impactos. `ShakeAt(cell)` vibra la celda específica vía MMWiggle.WigglePosition(); opcionalmente dispara MMF_Player extra (aural/haptic). **S85 cambio:** solo sacude la celda pedida (eliminado el campo radius antiguo que iteraba un radio); ResolutionAnimator itera las celdas de cada evento. Tunables: wiggleDuration (0.3s). Inyección: CombatBoardBuilder (para acceso a bloques por celda).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatBoardBuilder]], [[ResolutionAnimator]]
