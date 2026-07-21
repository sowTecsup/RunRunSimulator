---
tags: [script, world, agent, internal]
---

# AgentPhysics.cs

**Ruta:** `World/AI/AgentPhysics.cs`

**Responsabilidad:** Handoff NavMeshAgent ⇄ Rigidbody y secuencia de ragdoll (Carried → Thrown → Recovering). Maneja trayectoria balística post-lanzamiento: reflejo en superficies (bounce), cadenas de impacto entre criaturas, settle en el piso, y get-up (animación de levantarse del suelo). Implementa [[IThrowable]]: `OnGrab()`, `OnRelease()`, `OnThrow()`, `Knock()`, y `Launch()` (para cannon spawn). Tick de físicas: `FixedTick()` (carry follow), `TickThrown()` (settle timeout), `TickRecovering()` (lerp upright). Recuperación por frío: `RecoverIfStuckOffMesh()` (creature stuck kinematic fuera del NavMesh).

**Métodos públicos (IThrowable + llamadas desde MoriMochiAgent):**
- `OnGrab(Transform anchor)` — desacopla a física, flotador a la mano
- `OnRelease()` — entra en ragdoll
- `OnThrow(Vector3 force)` — aplica impulso
- `Knock(Vector3 force)` — golpeado por otra criatura, ragdoll + impulso
- `Launch(Vector3 launchPos, velocity)` — cannon spawn: teleporta a muzzle, aplica velocidad
- `EnterRagdoll()` — desacopla, aplica física (shared por throw/release/knock)
- `TickThrown()` — monitorea settle + safety timeout, llama a BeginGetUp
- `TickRecovering()` — lerp rotación/posición, re-ancla al NavMesh
- `RecoverIfStuckOffMesh()` — detección de stuck kinematic off-mesh, recupera

**Física handoff internals:**
- `DetachToPhysics()` — disable agente, enable Rigidbody dynamic
- `RejoinNavMesh(Vector3 desired, int mask) → bool` — kinematic + Warp + ResetPath
- `ApplyThrownPhysics()` — setup de damping, reset bounce/settle counters

**Colisiones:**
- `HandleCollisionEnter(Collision)` — reflects bounce, chain-knockes otras IThrowable
- `HandleTriggerEnter(Collider)` — knock on soft colliders (alternativo a HandleCollisionEnter)

**State internals:**
- `thrownTimer, settleTimer, bounceCount` — timing y contadores de ragdoll
- `lastVelocity` — capturado para reflejos en impactos
- `recoverTimer, getUpFrom/To, getUpFromPos/toPos` — animación get-up
- `offMeshGrace` — acumulador de detección stuck
- `effDownedDelay, effGetUpDuration` — timings escalados por RecoverySpeed

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[MoriMochiAgent]], [[AgentContext]], [[IThrowable]], [[MoriMonchiController]], [[ThrowableObject]]
