---
tags: [script, prototype, tooling]
---

# SpiderGaitMonitor.cs

**Ruta:** `Prototype/Spider/SpiderGaitMonitor.cs`

**Responsabilidad:** Panel IMGUI de telemetría (derecha). Monitorea cada pata en tiempo real: número de pasos, drag máximo, tiempo promedio y máximo esperando turno. Actualiza stats cada frame vía `Update()` (compara estados stepping/wants_step de cada pata). Expone `Report()` (string con estadísticas resumidas). Botón "Reset stats" limpia contadores. Gizmos de color: rojo (pisando), naranja (esperando), verde (apoyada).

**Notas de prototipo:** Tooling puro de análisis. Ayuda a verificar gait balance y urgencia adaptativa. No toca gameplay.

**Vinculado a:** Prototype/Spider

**Conexiones:** [[SpiderLegStepper]]
