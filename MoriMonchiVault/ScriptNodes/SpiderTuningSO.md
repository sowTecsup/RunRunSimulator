---
tags: [script, prototype, data]
---

# SpiderTuningSO.cs

**Ruta:** `Prototype/Spider/SpiderTuningSO.cs`

**Responsabilidad:** ScriptableObject dueño único de los knobs del prototipo de araña procedural. Expone parámetros de tuning en rangos acotados: movimiento (velocidad, giro, altura de cabalgada), patas (distancia, altura, duración de paso, splay, overshoot, anticipación, torsión máxima), movimiento del cuerpo (idle, bob, lean) y ragdoll (impulso). Sin lógica de juego, solo datos. Los componentes leen con fallback a sus campos serializados propios. `SpiderDevPanel` edita los campos directamente en el SultanaGUI.

**Notas de prototipo:** Este es un prototipo POC procedural en escena aislada `MorimonchiNewModel`. A futuro se reemplaza por modelo real + Animator + RigBuilder. Los knobs están calibrados para el modelo temp actual.

**Vinculado a:** Prototype/Spider

**Conexiones:** [[SpiderBodyController]], [[SpiderLegStepper]], [[SpiderBodyMotion]], [[SpiderDevPanel]]
