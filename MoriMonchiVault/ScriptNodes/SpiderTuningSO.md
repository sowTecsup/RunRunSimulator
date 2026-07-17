---
tags: [script, prototype, data]
---

# SpiderTuningSO.cs

**Ruta:** `Prototype/Spider/SpiderTuningSO.cs`

**Responsabilidad:** ScriptableObject dueño único de los knobs del prototipo de araña procedural. Expone parámetros de tuning en rangos acotados: movimiento (velocidad, giro, altura de cabalgada), patas (distancia, altura, duración de paso, splay, overshoot, anticipación, torsión máxima), movimiento del cuerpo (idle, bob, lean), elasticidad del cuerpo (elasticAmount factor 0-1, controla deformación vía velocidad vertical), salto (jumpImpulse impulso inicial 1-6), y ragdoll (impulso de lanzamiento). Sin lógica de juego, solo datos. Los componentes leen con fallback a sus campos serializados propios. `SpiderDevPanel` edita los campos directamente en el GUI de desarrollo.

**Notas de prototipo:** Este es un prototipo POC procedural en escena aislada `MorimonchiNewModel`. A futuro se reemplaza por modelo real + Animator + RigBuilder. Los knobs están calibrados para el modelo temp actual.

**Cambios S50:** Se agregó Header "Elastico" con campos `elasticAmount` [0,1] default 0.5 (factor de deformación elástica) y `jumpImpulse` [1,6] default 3 (impulso vertical del salto).

**Vinculado a:** Prototype/Spider

**Conexiones:** [[SpiderBodyController]], [[SpiderLegStepper]], [[SpiderBodyMotion]], [[SpiderDevPanel]], [[SpiderElasticBody]], [[SpiderJump]]
