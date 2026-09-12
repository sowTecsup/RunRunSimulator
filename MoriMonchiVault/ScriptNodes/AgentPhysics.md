---
tags: [script, world, ai, agent, internal, physics]
---

# AgentPhysics.cs

**Ruta:** `World/AI/AgentPhysics.cs`

**Responsabilidad:** Handoff NavMeshAgent ⇄ Rigidbody. Ragdoll, balística post-lanzamiento, colisiones, cadenas de knockback, recuperación. **S103:** Torque `knockSpin` agregado en `Knock()` para girar criatura al tumbar. **S116:** Launch escribe Rb.position además de Body.position; HandleCollisionEnter no rebota si dirección del impacto y velocidad apuntan en sentido contrario (Dot producto > 0).

**Métodos públicos (IThrowable):**
- `OnGrab(Transform anchor)` — flotador a mano
- `OnRelease()` — ragdoll
- `OnThrow(Vector3 force)` — impulso balístico
- `Knock(Vector3 force)` — golpeado, ragdoll + impulso. **S103:** aplica torque knockSpin
- `Launch(Vector3 pos, velocity)` — **S116:** escribe `ctx.Body.position = launchPos; ctx.Rb.position = launchPos`
- `CaptureNavAnchor(Vector3 pos)` — captura punto seguro pre-vuelo
- `EnterRagdoll()` — disable navmesh, enable rigidbody
- `TickThrown()` — monitorea settle, bounce, void-fall, llama GetUp
- `TickRecovering()` — lerp pose, re-ancla, llama NotifyRecovered
- `RecoverIfStuckOffMesh()` — detección kinematic stuck

**Colisiones (S116 ACTUALIZADO):**
- `HandleCollisionEnter(Collision)` — **S116:** valida dirección antes de rebotar:
  - Calcula `contactNormal` desde primer contact point
  - Chequea `Vector3.Dot(lastVelocity, contactNormal) > 0f` (velocidad apunta en sentido de normal = alejándose)
  - Si Dot > 0, retorna sin rebotar (impacto por detrás, no colisión frontal)
  - Chain-knock: valida `!owner.IgnoresChainKnock()` antes de knockear vecinos
- `HandleTriggerEnter(Collider)` — soft knock, igual validación

**S103 Cambios:**
- En `Knock(Vector3 force)`:
  - Después de aplicar impulso, agrega torque: `ctx.Rb.AddTorque(push * owner.knockSpin, ForceMode.Impulse)`
  - `push` = dirección del golpe normalizada
  - `owner.knockSpin` = scalar del campo MoriMochiAgent (tuning visual de spin)

**S116 Cambios:**
- `Launch(Vector3 launchPos, Vector3 launchVelocity)`:
  - Línea 132: `ctx.Body.position = launchPos;` (además de Rb.position)
  - Sincroniza Transform y Rigidbody en el momento del lanzamiento
- `HandleCollisionEnter(Collision collision)`:
  - Línea 71: `if (Vector3.Dot(lastVelocity, contactNormal) > 0f) return;`
  - No rebota si velocidad y normal apuntan en sentido contrario (alejándose ya)

**Internals (sin cambios S103):**
- settleTimer, thrownTimer, bounceCount
- recoverTimer, getUpFrom/To, getUpFromPos/toPos
- lastNavAnchor, voidRescues

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]], [[Index/10 - Visualization]], S116

**Conexiones:** [[MoriMochiAgent]], [[AgentContext]], [[MonchiSquashDriver]], [[IThrowable]], [[AgentClash]]
