---
tags: [script, combat-prototype, presentation]
---

# CombatCameraController.cs

**Ruta:** `Systems/CombatPrototype/CombatCameraController.cs`

**Responsabilidad:** Órbita de cámara alrededor del tablero (pivote = centro). Update: lee flechas izq/der para rotar yaw por pasos de 90° (duración tunable). Camera gira y orbita con suavidad (MoveTowardsAngle). Inyección: CombatBoardBuilder (para leer dimensiones del tablero). **Cambios S83:** zoom con rueda del mouse. Campos: zoomStep (decremento/incremento por scroll), minZoom/maxZoom (límites), zoomDuration (tiempo de interpolación). State: _zoom (zoom actual), _targetZoom (target para smooth). Lógica: scroll.y > 0.01 → _targetZoom -= zoomStep; scroll.y < -0.01 → _targetZoom += zoomStep. MoveTowards interpola _zoom hacia _targetZoom. Posición final = _pivot + rot * (_baseOffset * _zoom), rota + orbita simultáneamente.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatBoardBuilder]]
