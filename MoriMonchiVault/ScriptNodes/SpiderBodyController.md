---
tags: [script, prototype, core]
---

# SpiderBodyController.cs

**Ruta:** `Prototype/Spider/SpiderBodyController.cs`

**Responsabilidad:** Orquestador principal del prototipo de araña. Lee input WASD directo de `Keyboard.current` + propiedades `AutoWalk`/`AutoTurn` para verificación. Computa flag `turning` a partir del eje de giro (umbral |turn| > 0.01). Mantiene el root (rotación, translación forward, altura por raycast a ground). Integra con `SpiderJump`: suma `jump.HeightOffset` al raycast height para permitir salto. Implementa selector de pata "most-overdue" por grupo de gait: itera legs, busca la que más quiera pisar (máximo Drag) dentro de su grupo de gait sin bloquear otros grupos activos. Tickea cada `SpiderLegStepper` pasando `mayStep` (verdadero solo si es candidata) y `turning` (flag de giro actual).

**Notas de prototipo:** Escena aislada sin action maps; lee directo de Input System nuevo por comodidad de prueba. En juego real se usaría action map. Gizmos de debug muestran raycast y grupos de gait por color. El flag `turning` gatealiza el comportamiento de anticipación de torsión en las patas.

**Cambios S50:** Se agregó ref serializada `SpiderJump jump`; en el snap de altura por raycast ahora suma `jump.HeightOffset` a `hit.point.y + ride`.

**Vinculado a:** Prototype/Spider

**Conexiones:** [[SpiderTuningSO]], [[SpiderLegStepper]], [[SpiderLegIK]], [[SpiderRagdollMode]], [[SpiderBodyMotion]], [[SpiderJump]]
