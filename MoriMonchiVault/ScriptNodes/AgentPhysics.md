---
tags: [script, world, agent, internal]
---

# AgentPhysics.cs

**Ruta:** `World/AI/AgentPhysics.cs`

**Responsabilidad:** Handoff NavMeshAgent ⇄ Rigidbody y secuencia de ragdoll (Carried → Thrown → Recovering). Maneja trayectoria balística post-lanzamiento: reflejo en superficies (bounce), cadenas de impacto entre criaturas, settle en el piso, y get-up (animación de levantarse del suelo). Implementa [[IThrowable]]: `OnGrab()`, `OnRelease()`, `OnThrow()`, `Knock()`, y `Launch()` (para cannon spawn). **S65:** Sobrecargo `Knock(Vector3, bool stress)` para permitir golpes sin estrés (p.ej. final de pelea de gremlins). **S69:** Anti-void-fall: `CaptureNavAnchor(pos)` captura punto seguro, campos `lastNavAnchor/hasNavAnchor/voidRescues`. En `TickThrown()`, si caída > `voidFallDrop` (20 default), 1er rescate teleport 1m sobre anchor + velocidad cero, 2do rescate warp `RejoinNavMesh()` + roam. Tick de físicas: `FixedTick()` (carry follow), `TickThrown()` (settle timeout + void-fall detection), `TickRecovering()` (lerp upright). Recuperación por frío: `RecoverIfStuckOffMesh()` (creature stuck kinematic fuera del NavMesh).

## Métodos públicos (IThrowable + llamadas desde MoriMochiAgent)

- `OnGrab(Transform anchor)` — desacopla a física, flotador a la mano
- `OnRelease()` — entra en ragdoll
- `OnThrow(Vector3 force)` — aplica impulso
- `Knock(Vector3 force)` — golpeado por otra criatura, ragdoll + impulso + estrés. **Alias:** `Knock(force, true)`
- `Knock(Vector3 force, bool stress)` — **S65 NUEVO** sobrecargo: golpeado con opción de estrés. Si `stress=true`, resta Affect por golpe. Si `stress=false`, ragdoll sin estrés (usado en pelea de gremlins para evitar penalidad doble).
- `Launch(Vector3 launchPos, velocity)` — cannon spawn: teleporta a muzzle, aplica velocidad
- `CaptureNavAnchor(Vector3 pos)` — **S69 NUEVO** captura punto seguro en NavMesh (llamado en Initialize si on-mesh, en DetachToPhysics si on-mesh). Almacenado en `lastNavAnchor`, usado para rescate de void-fall.
- `EnterRagdoll()` — desacopla, aplica física (shared por throw/release/knock)
- `TickThrown()` — **S69 ACTUALIZADO** monitorea settle + safety timeout + void-fall detection, llama a BeginGetUp
- `TickRecovering()` — lerp rotación/posición, re-ancla al NavMesh
- `RecoverIfStuckOffMesh()` — detección de stuck kinematic off-mesh, recupera

## Física handoff internals

- `DetachToPhysics()` — **S69 ACTUALIZADO** disable agente, enable Rigidbody dynamic, llama `CaptureNavAnchor()` si on-mesh
- `RejoinNavMesh(Vector3 desired, int mask) → bool` — kinematic + Warp + ResetPath
- `ApplyThrownPhysics()` — setup de damping, reset bounce/settle counters

## Colisiones

- `HandleCollisionEnter(Collision)` — reflects bounce, chain-knockes otras IThrowable; aplica Affect hit si impacto fuerte (affectOnHardCollision)
- `HandleTriggerEnter(Collider)` — knock on soft colliders (alternativo a HandleCollisionEnter)

## Cambios S69: Anti-Void-Fall Rescue

**Campos nuevos:**
```csharp
private Vector3 lastNavAnchor;
private bool    hasNavAnchor;
private int     voidRescues;
```

**Captura de anchor:**
```csharp
internal void CaptureNavAnchor(Vector3 pos)
{
    lastNavAnchor = pos;
    hasNavAnchor  = true;
}
```

**Lógica de rescate en TickThrown():**
1. Si `hasNavAnchor` y posición Y cae más de `owner.voidFallDrop` (default 20 unidades) por debajo del anchor:
   - **1er rescate (voidRescues == 0):** Teleport 1m sobre el anchor, velocidad a cero, enter get-up (caída blanda)
   - **2do rescate (voidRescues == 1):** Hard warp `RejoinNavMesh(anchorNearby)`, entra Roaming (recuperación forzada)
   - **3+ rescates:** Creature se reinicia (quirk: no debería alcanzar este punto)

**Knob en MoriMochiAgent.Tuning.Physics:**
- `voidFallDrop` float (default 20) — threshold de caída bajo el cual dispara rescate

**Interpretación:**
- Previene criaturas caídas al vacío por glitches o lanzamientos extremos
- 1er rescate amortiguado (landing suave), 2do rescate es hard snap
- Sistema fallback: no deja criaturas pegadas fuera del mapa

**Consumo:**
- `MoriMochiAgent.Initialize()` → llama `physics.CaptureNavAnchor(pos)` si spawn on-mesh
- `AgentPhysics.DetachToPhysics()` → llama `CaptureNavAnchor()` si el agente estaba on-mesh
- `AgentPhysics.TickThrown()` → verifica cada frame si Y cae bajo threshold, ejecuta rescate

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
- `lastNavAnchor, hasNavAnchor, voidRescues` — **S69** para rescate de caída

## Vinculado a

- [[Index/06 - Player & World]]
- [[Index/02 - Genetics & Breeding]]
- [[MoriMonchiVault/Index/14 - Social V2]] (S65 Fighting mode)

## Conexiones

**Entrada:**
- `MoriMochiAgent.Initialize()` → llama `CaptureNavAnchor(pos)` si on-mesh (S69)
- `AgentPhysics.DetachToPhysics()` — **S69** llama `CaptureNavAnchor()` si on-mesh
- `AgentSocial.TickSocializing()` — llama Knock(force, stress=false) en modo Fighting
- `MoriMonchiController.Throw()` — llama OnThrow vía IThrowable
- Colisiones en HandleCollisionEnter — cadena de knockes entre criaturas

**Salida:**
- `AgentContext.State` — Thrown/Recovering durante ragdoll
- `CreatureDNA.Needs.Affect` — resta por impacto si stress=true
- Rigidbody velocity — impulso aplicado
- `AgentContext.Agent` — warp + rejoin en rescate (S69)
