---
tags: [script, prototype, core]
---

# SpiderBodyController.cs

**Ruta:** `Prototype/Spider/SpiderBodyController.cs`

**Responsabilidad:** Orquestador principal del prototipo de araña. Lee input WASD directo de `Keyboard.current` + propiedades `AutoWalk`/`AutoTurn` para verificación. Mantiene el root (rotación, translación forward, altura por raycast a ground). Implementa selector de pata "most-overdue" por grupo de gait: itera legs, busca la que más quiera pisar (máximo Drag) dentro de su grupo de gait sin bloquear otros grupos activos. Asignación de gait group por convención: índice negativo = pata independiente fuera del sistema de turnos (la pata trasera). NO tiene Update propio en patas; tickea cada `SpiderLegStepper` pasando `mayStep` verdadero solo si es la candidata. Luego resuelve IK de todas las patas.

**Notas de prototipo:** Escena aislada sin action maps; lee directo de Input System nuevo por comodidad de prueba. En juego real se usaría action map. Gizmos de debug muestran raycast y grupos de gait por color.

**Vinculado a:** Prototype/Spider

**Conexiones:** [[SpiderTuningSO]], [[SpiderLegStepper]], [[SpiderLegIK]], [[SpiderRagdollMode]], [[SpiderBodyMotion]]
