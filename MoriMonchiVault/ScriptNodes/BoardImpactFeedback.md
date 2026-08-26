---
tags: [script, combat-prototype, presentation]
---

# BoardImpactFeedback.cs

**Ruta:** `Systems/CombatPrototype/BoardImpactFeedback.md`

**Responsabilidad:** Proporciona feedback visual/haptic en el tablero ante impactos. ShakeAt(cell) vibra bloques en radio Chebyshev alrededor del impacto vía MMWiggle.WigglePosition(); opcionalmente dispara MMF_Player extra (aural/haptic). Tunables: radius (1 por defecto), wiggleDuration (0.3s). Inyección: CombatBoardBuilder (para acceso a bloques).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatBoardBuilder]], [[ResolutionAnimator]]
