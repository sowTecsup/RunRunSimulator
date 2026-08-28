---
tags: [script, combat-prototype, presentation]
---

# CombatCameraController.cs

**Ruta:** `CombatPrototype/CombatCameraController.cs`

**Responsabilidad:** Controla cámara ortográfica + pivote-orbita. **S86 GRAN CAMBIO:** Cámara ahora es **ORTOGRÁFICA**, no perspective. En Start, calcula bounding box del tablero (centro + elevación máxima), establece pivote en centro del tablero con offset de altura (`pivotHeight`). Calcula `orthographicSize` como (max(ancho, profundidad) * 0.5f) * framePadding para encuadre real. Tunables: `topBandFraction` (HUD top) y `bottomBandFraction` (HUD bottom) restan del viewport para evitar oclusión de UI. Update: flechas ←/→ rotan yaw por pasos de 90° (duración `rotateDuration`), suavidad con MoveTowardsAngle. Zoom con rueda del mouse no cambia orthographicSize sino la posición de la cámara (simulado). Pan WASD con clamp al tablero. Posición final = _pivot + rot * (_baseOffset * _zoom).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatBoardBuilder]], [[CombatBoard]]
