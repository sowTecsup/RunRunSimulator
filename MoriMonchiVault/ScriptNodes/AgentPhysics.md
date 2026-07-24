---
tags: [script, world, agent, internal]
---

# AgentPhysics.cs

**Ruta:** `World/AI/AgentPhysics.cs`

**Responsabilidad:** Handoff NavMeshAgent ⇄ Rigidbody y secuencia de ragdoll (Carried → Thrown → Recovering). Maneja trayectoria balística post-lanzamiento: reflejo en superficies (bounce), cadenas de impacto entre criaturas, settle en el piso, y get-up (animación de levantarse del suelo). Implementa [[IThrowable]]: `OnGrab()`, `OnRelease()`, `OnThrow()`, `Knock()`, y `Launch()` (para cannon spawn). **S65:** Sobrecargo `Knock(Vector3, bool stress)` para permitir golpes sin estrés (p.ej. final de pelea de gremlins). Tick de físicas: `FixedTick()` (carry follow), `TickThrown()` (settle timeout), `TickRecovering()` (lerp upright). Recuperación por frío: `RecoverIfStuckOffMesh()` (creature stuck kinematic fuera del NavMesh).

## Métodos públicos (IThrowable + llamadas desde MoriMochiAgent)

- `OnGrab(Transform anchor)` — desacopla a física, flotador a la mano
- `OnRelease()` — entra en ragdoll
- `OnThrow(Vector3 force)` — aplica impulso
- `Knock(Vector3 force)` — golpeado por otra criatura, ragdoll + impulso + estrés. **Alias:** `Knock(force, true)`
- `Knock(Vector3 force, bool stress)` — **S65 NUEVO** sobrecargo: golpeado con opción de estrés. Si `stress=true`, resta Affect por golpe. Si `stress=false`, ragdoll sin estrés (usado en pelea de gremlins para evitar penalidad doble).
- `Launch(Vector3 launchPos, velocity)` — cannon spawn: teleporta a muzzle, aplica velocidad
- `EnterRagdoll()` — desacopla, aplica física (shared por throw/release/knock)
- `TickThrown()` — monitorea settle + safety timeout, llama a BeginGetUp
- `TickRecovering()` — lerp rotación/posición, re-ancla al NavMesh
- `RecoverIfStuckOffMesh()` — detección de stuck kinematic off-mesh, recupera

## Física handoff internals

- `DetachToPhysics()` — disable agente, enable Rigidbody dynamic
- `RejoinNavMesh(Vector3 desired, int mask) → bool` — kinematic + Warp + ResetPath
- `ApplyThrownPhysics()` — setup de damping, reset bounce/settle counters

## Colisiones

- `HandleCollisionEnter(Collision)` — reflects bounce, chain-knockes otras IThrowable; aplica Affect hit si impacto fuerte (affectOnHardCollision)
- `HandleTriggerEnter(Collider)` — knock on soft colliders (alternativo a HandleCollisionEnter)

## Sobrecargo S65: Knock(Vector3 force, bool stress)

**Razón:** En peleas de gremlins, el golpe final debería ragdoll al oponente SIN restarle Affect adicional (ya pierde Affect durante la pelea). El sobrecargo permite:

```csharp
internal void Knock(Vector3 force) => Knock(force, true);  // legacy: con estrés
internal void Knock(Vector3 force, bool stress)
{
    // ... setup ragdoll ...
    if (stress) ctx.Dna?.Needs.AddAffect(-owner.affectOnThrow);
    ctx.Rb.AddForce(force, ForceMode.Impulse);
}
```

**Consumo:**
- `AgentSocial.TickSocializing()` (Fighting mode) — llama `Knock(force, false)` para abalanzada final
- `AgentPhysics.HandleCollisionEnter()` — llama `Knock(impulse)` en cadena (stress=true, comportamiento original)
- `MoriMonchiController.Throw()` — llama `Knock()` (stress=true, comportamiento original)

## State internals

- `thrownTimer, settleTimer, bounceCount` — timing y contadores de ragdoll
- `lastVelocity` — capturado para reflejos en impactos
- `recoverTimer, getUpFrom/To, getUpFromPos/toPos` — animación get-up
- `offMeshGrace` — acumulador de detección stuck
- `effDownedDelay, effGetUpDuration` — timings escalados por RecoverySpeed

## Vinculado a

- [[Index/06 - Player & World]]
- [[MoriMonchiVault/Index/14 - Social V2]] (S65 Fighting mode)

## Conexiones

**Entrada:**
- `AgentSocial.TickSocializing()` — llama Knock(force, stress=false) en modo Fighting
- `MoriMonchiController.Throw()` — llama OnThrow vía IThrowable
- Colisiones en HandleCollisionEnter — cadena de knockes entre criaturas

**Salida:**
- `AgentContext.State` — Thrown/Recovering durante ragdoll
- `CreatureDNA.Needs.Affect` — resta por impacto si stress=true
- Rigidbody velocity — impulso aplicado
