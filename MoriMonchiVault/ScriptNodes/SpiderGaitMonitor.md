---
tags: [script, prototype, tooling]
---

# SpiderGaitMonitor.cs

**Ruta:** `Prototype/Spider/SpiderGaitMonitor.cs`

**Responsabilidad:** Panel IMGUI de telemetría (derecha). Monitorea cada pata en tiempo real: número de pasos, drag máximo, tiempo promedio y máximo esperando turno. Actualiza stats cada frame vía `Update()` (compara estados stepping/wants_step de cada pata). Expone `Report()` (string con estadísticas resumidas). Botón "Reset stats" limpia contadores. Gizmos de color: rojo (pisando), naranja (esperando), verde (apoyada). Se ancla al borde derecho en coordenadas virtuales y escala automáticamente con el tamaño de pantalla vía `GUI.matrix = Scale(max(1, Screen.height/1080))` (regla Juan S50) para mantener legibilidad en diferentes resoluciones.

**Notas de prototipo:** Tooling puro de análisis. Ayuda a verificar gait balance y urgencia adaptativa. No toca gameplay.

**Cambios S50:** Se agregó scaling automático vía GUI.matrix (escala por `Screen.height/1080`, coherente con DevPanel); se ajustó anclaje al borde derecho usando `Screen.width / scale - panelWidth - 10f` en coordenadas virtuales.

**Vinculado a:** Prototype/Spider

**Conexiones:** [[SpiderLegStepper]]
