---
tags: [script, combat-prototype, presentation]
---

# CombatCameraController.cs

**Ruta:** `CombatPrototype/CombatCameraController.cs`

**Responsabilidad:** Controla cámara ortográfica + pivote-orbita. **S86 GRAN CAMBIO:** Cámara ahora es **ORTOGRÁFICA**, no perspective. En Start, calcula bounding box del tablero (centro + elevación máxima), establece pivote en centro del tablero con offset de altura (`pivotHeight`). Calcula `orthographicSize` como (max(ancho, profundidad) * 0.5f) * framePadding para encuadre real. Tunables: `topBandFraction` (HUD top) y `bottomBandFraction` (HUD bottom) restan del viewport para evitar oclusión de UI. **S88 CAMBIO PERSPECTIVA**: encuadre reescrito con muestreo de silueta (hU/hR) compartido entre rama ortho y perspectiva, compensación de franjas de UI en perspectiva (resta del tamaño ortho basado en aspecto), `perspectiveFill` serializado (tunable para escalar viewport, default 1.3f), `minZoom` tunable (escena 0.25). Update: flechas ←/→ rotan yaw por pasos 90° (duración `rotateDuration`). Zoom con rueda del mouse escala posición cámara. Pan WASD con clamp al tablero.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatBoardBuilder]], [[CombatBoard]]
