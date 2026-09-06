---
tags: [script, world, ai, agent, internal, physics]
---

# AgentPhysics.cs

**Ruta:** `World/AI/AgentPhysics.cs`

**Responsabilidad:** Handoff NavMeshAgent ⇄ Rigidbody. Ragdoll, balística post-lanzamiento, colisiones, cadenas de knockback, recuperación. **S103:** Torque `knockSpin` agregado en `Knock()` para girar criatura al tumbar (coordina con MonchiSquashDriver).

**Métodos públicos (IThrowable):**
- `OnGrab(Transform anchor)` — flotador a mano
- `OnRelease()` — ragdoll
- `OnThrow(Vector3 force)` — impulso balístico
- `Knock(Vector3 force)` — golpeado, ragdoll + impulso. **S103:** aplica torque knockSpin
- `Launch(Vector3 pos, velocity)` — cannon spawn
- `CaptureNavAnchor(Vector3 pos)` — captura punto seguro pre-vuelo
- `EnterRagdoll()` — disable navmesh, enable rigidbody
- `TickThrown()` — monitorea settle, bounce, void-fall, llama GetUp
- `TickRecovering()` — lerp pose, re-ancla, llama NotifyRecovered
- `RecoverIfStuckOffMesh()` — detección kinematic stuck

**Colisiones (S100 ACTUALIZADO):**
- `HandleCollisionEnter(Collision)` — reflect bounce, chain-knock; valida `!owner.IgnoresChainKnock()` antes de knockear vecinos
- `HandleTriggerEnter(Collider)` — soft knock, igual validación

**S103 Cambios:**
- En `Knock(Vector3 force)`:
  - Después de aplicar impulso, agrega torque: `ctx.Rb.AddTorque(push * owner.knockSpin, ForceMode.Impulse)`
  - `push` = dirección del golpe normalizada
  - `owner.knockSpin` = scalar del campo MoriMochiAgent (tuning visual de spin)
  - Coordina con MonchiSquashDriver para rotación de deformación

**Internals (sin cambios S103):**
- settleTimer, thrownTimer, bounceCount
- recoverTimer, getUpFrom/To, getUpFromPos/toPos
- lastNavAnchor, voidRescues (S69)

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]], [[Index/10 - Visualization]]

**Conexiones:** [[MoriMochiAgent]], [[AgentContext]], [[MonchiSquashDriver]], [[IThrowable]]
