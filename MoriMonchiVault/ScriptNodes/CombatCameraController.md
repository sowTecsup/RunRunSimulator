---
tags: [script, combat-prototype, presentation]
---

# CombatCameraController.cs

**Ruta:** `CombatPrototype/CombatCameraController.cs`

**Responsabilidad:** Órbita de cámara alrededor del tablero (pivote = centro). Update: lee flechas izq/der para rotar yaw por pasos de 90° (duración tunable), **S84: WASD pan con clamp al tablero**. Camera gira y orbita con suavidad (MoveTowardsAngle). Inyección: CombatBoardBuilder (para leer dimensiones del tablero y clamp). Zoom con rueda del mouse (zoomStep, minZoom/maxZoom, zoomDuration). State: _zoom (actual), _targetZoom (target smooth). Lógica: scroll.y > 0 → _targetZoom -= zoomStep; scroll.y < 0 → _targetZoom += zoomStep. MoveTowards interpola _zoom hacia _targetZoom. Posición final = _pivot + rot * (_baseOffset * _zoom), rota + orbita + pan simultáneamente.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatBoardBuilder]]
