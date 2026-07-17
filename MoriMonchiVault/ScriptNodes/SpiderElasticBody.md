---
tags: [script, prototype, anim]
---

# SpiderElasticBody.cs

**Ruta:** `Prototype/Spider/SpiderElasticBody.cs`

**Responsabilidad:** Dueño EXCLUSIVO de `localScale` del pivote visual `BodyVisual`. Aplica resorte subamortiguado (parámetros `frequency` y `dampingRatio` serializados) que persigue target derivado de la velocidad vertical REAL del root (capturada cada frame vía diferencia de Y). La cantidad de compresión/estiramiento se controla por `tuning.elasticAmount` [0,1] (default 0.5), escalando el efecto de velocidad vertical. Conserva volumen: `xz = 1/sqrt(y)`, asegurando que la deformación parezca masa constante. Clamp final [0.55, 1.6] previene valores extremos. Guarda anti-teleport: ignora cambios de velocidad vertical >12 m/s. Resetea completamente en ragdoll (vuelve a pose base). Las posiciones y rotaciones del root las controla `SpiderBodyMotion`; este script SOLO toca scale.

**Notas de prototipo:** El resorte es smooth spring via classical dampened harmonic oscillator math (omega-based). Parámetros calibrados para que se sienta elástico pero no inestable. Detecta teleport por delta Y/dt para evitar popeos en discontinuidades de la escena.

**Cambios S50:** Script nuevo (primera implementación). Deformación elástica vía Hooke's law + conservación de volumen.

**Vinculado a:** Prototype/Spider

**Conexiones:** [[SpiderTuningSO]], [[SpiderRagdollMode]], [[SpiderBodyMotion]]
