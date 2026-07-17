---
tags: [script, prototype, core]
---

# SpiderJump.cs

**Ruta:** `Prototype/Spider/SpiderJump.cs`

**Responsabilidad:** Integrador vertical del salto procedural. Lee impulso inicial de `tuning.jumpImpulse` y aplica gravedad propia vía factor `gravityScale` (acoplada a `Physics.gravity.y`). Mantiene estado: `heightOffset` (desplazamiento vertical lógico) y `verticalVelocity` (integración de aceleración). Contrato público: `HeightOffset` (float, sumado por `SpiderBodyController` al raycast ground), `IsAirborne` (bool, true si heightOffset > 0), `Jump()` (método, dispara salto si no ya airborne y no en ragdoll). Input directo: Espacio (`Keyboard.current.spaceKey.wasPressedThisFrame`, patrón prototipo). Resetea completamente en ragdoll. Sin Update de física del Rigidbody: es offset puro sobre la altura calculada del ground.

**Notas de prototipo:** El salto es determinista y desacoplado de la física de UnityEngine. No es un projectile real; es un offset que el controller suma al ride height. Patrón POC: lógica simple + parámetros calibrados en el tuning. A futuro se integra con Animator jump animations.

**Cambios S50:** Script nuevo (primera implementación). Integración vertical del salto + gravity scale + guardias (no saltar si airborne, ragdoll o tuning nulo).

**Vinculado a:** Prototype/Spider

**Conexiones:** [[SpiderTuningSO]], [[SpiderRagdollMode]], [[SpiderBodyController]]
