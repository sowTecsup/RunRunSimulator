---
tags: [script, prototype, anim]
---

# SpiderBodyMotion.cs

**Ruta:** `Prototype/Spider/SpiderBodyMotion.cs`

**Responsabilidad:** Anima solo el pivote visual `BodyVisual` (no el root del controller). Aplica: respiración (idle sinusoidal), movimiento procedural (Perlin noise idle pitch/roll), bob (onda por velocidad actual), y lean (inclinación por velocidad forward y yaw rate). Toma velocidad 3D de la cadera, yaw rate, y aplica suavizado exponencial. Se apaga totalmente en ragdoll (resetea a base pose). Usa `SpiderTuningSO.idleAmount`, `bobAmount`, `leanAmount` como factores.

**Notas de prototipo:** Solo modifica `BodyVisual.localPosition` y `BodyVisual.localRotation`; el root lo mueve `SpiderBodyController`. Frecuencias internas: respiration 1.4Hz, bob 9Hz (scaled by speed).

**Vinculado a:** Prototype/Spider

**Conexiones:** [[SpiderTuningSO]], [[SpiderRagdollMode]]
