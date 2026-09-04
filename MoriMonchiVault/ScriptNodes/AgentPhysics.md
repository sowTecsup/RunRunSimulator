---
tags: [script, world, agent, internal]
---

# AgentPhysics.cs

**Ruta:** `World/AI/AgentPhysics.cs`

**Responsabilidad:** Handoff NavMeshAgent ⇄ Rigidbody y secuencia de ragdoll (Carried → Thrown → Recovering). Maneja trayectoria balística post-lanzamiento: reflejo en superficies (bounce), cadenas de impacto entre criaturas, settle en el piso, y get-up (animación de levantarse del suelo). Implementa [[IThrowable]]: `OnGrab()`, `OnRelease()`, `OnThrow()`, `Knock()`, y `Launch()` (para cannon spawn). **S65:** Sobrecargo `Knock(Vector3, bool stress)` para permitir golpes sin estrés (p.ej. final de pelea de gremlins). **S69:** Anti-void-fall: `CaptureNavAnchor(pos)` captura punto seguro, campos `lastNavAnchor/hasNavAnchor/voidRescues`. **S100:** TickRecovering llama `owner.NotifyRecovered()` en vez de `RequestRoam` para coordinar con [[AgentClash.OnRecovered()]]; HandleCollisionEnter/HandleTriggerEnter validan `!owner.IgnoresChainKnock(otherAgent)` para evitar dominó del atacante inmune.

## Métodos públicos (IThrowable + llamadas desde MoriMochiAgent)

- `OnGrab(Transform anchor)` — desacopla a física, flotador a la mano
- `OnRelease()` — entra en ragdoll
- `OnThrow(Vector3 force)` — aplica impulso
- `Knock(Vector3 force)` — golpeado por otra criatura, ragdoll + impulso + estrés. **Alias:** `Knock(force, true)`
- `Knock(Vector3 force, bool stress)` — **S65 NUEVO** sobrecargo: golpeado con opción de estrés. Si `stress=true`, resta Affect por golpe. Si `stress=false`, ragdoll sin estrés (usado en pelea de gremlins para evitar penalidad doble). **S100:** también usado en cadena de choque para no restar Affect extra.
- `Launch(Vector3 launchPos, velocity)` — cannon spawn: teleporta a muzzle, aplica velocidad
- `CaptureNavAnchor(Vector3 pos)` — **S69 NUEVO** captura punto seguro en NavMesh (llamado en Initialize si on-mesh, en DetachToPhysics si on-mesh). Almacenado en `lastNavAnchor`, usado para rescate de void-fall.
- `EnterRagdoll()` — desacopla, aplica física (shared por throw/release/knock)
- `TickThrown()` — **S69 ACTUALIZADO** monitorea settle + safety timeout + void-fall detection, llama a BeginGetUp
- `TickRecovering()` — **S100 ACTUALIZADO** lerp rotación/posición, re-ancla al NavMesh, al terminar llama `owner.NotifyRecovered()` en vez de `RequestRoam()`
- `RecoverIfStuckOffMesh()` — detección de stuck kinematic off-mesh, recupera

## Física handoff internals

- `DetachToPhysics()` — **S69 ACTUALIZADO** disable agente, enable Rigidbody dynamic, llama `CaptureNavAnchor()` si on-mesh
- `RejoinNavMesh(Vector3 desired, int mask) → bool` — kinematic + Warp + ResetPath
- `ApplyThrownPhysics()` — setup de damping, reset bounce/settle counters

## Colisiones (S100 ACTUALIZADO)

- `HandleCollisionEnter(Collision)` — reflects bounce, chain-knockes otras IThrowable; **S100:** valida `!owner.IgnoresChainKnock(otherAgent)` antes de aplicar knock para evitar dominó del atacante; aplica Affect hit si impacto fuerte (affectOnHardCollision)
- `HandleTriggerEnter(Collider)` — knock on soft colliders; **S100:** igual validación de chain immunity (alternativo a HandleCollisionEnter)

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
- **S100:** `MoriMochiAgent.ReceiveClashHit()` — llama `Knock(force, false)` para no restar Affect extra (el choque ya cuesta en el combate)
- `MoriMonchiController.Throw()` — llama `Knock()` (stress=true, comportamiento original)

## Cambios S100: Clash Integration

**Línea 191 en TickRecovering():** Termina en `owner.NotifyRecovered()` en vez de `RequestRoam()`:
```csharp
internal void TickRecovering()
{
    // ... lerp posición/rotación ...
    if (recoverTimer >= effDownedDelay + effGetUpDuration)
    {
        if (!ctx.Agent.enabled) ctx.Agent.enabled = true;
        ctx.Agent.Warp(getUpToPos);
        ctx.Agent.ResetPath();
        owner.onGetUp?.Invoke();
        owner.NotifyRecovered();  // S100: coordina con AgentClash.OnRecovered() para decisión Dazed/counter/retreat
    }
}
```

**Razón:** Permite que [[AgentClash.OnRecovered()]] (llamado desde NotifyRecovered) tome decisión sobre counter-attack o retrete post-Dazed, sin volver inmediatamente a Roaming.

**Líneas 62 y 94 en HandleCollisionEnter/HandleTriggerEnter:** Validación de chain immunity:
```csharp
internal void HandleCollisionEnter(Collision collision)
{
    // ...
    var otherAgent = collision.collider.GetComponentInParent<MoriMochiAgent>();
    if (otherAgent == null || (!ExpeditionTeams.AreAllies(owner.Team, otherAgent.Team) && !owner.IgnoresChainKnock(otherAgent)))
    {
        // ... knock el rival ...
    }
}

internal void HandleTriggerEnter(Collider other)
{
    // ...
    var hitAgent = other.GetComponentInParent<MoriMochiAgent>();
    if (hitAgent == null || (!ExpeditionTeams.AreAllies(owner.Team, hitAgent.Team) && !owner.IgnoresChainKnock(hitAgent)))
    {
        // ... knock el rival ...
    }
}
```

**Razón:** Detiene dominó infinito de knockes en cadena. Si A golpea B (B recibe inmunidad por 0.8s del atacante A), B no vuelve a golpear a A durante ese tiempo. Evita "ping-pong" de criaturas golpeándose mutuamente indefinidamente.

## Invariantes S100 + S69 + S65 + S93

- `TickRecovering` → `NotifyRecovered()`: transición coordinada con AgentClash para decisiones post-ragdoll (counter, retrete o roaming)
- `Knock`: un knock en pleno vuelo NO resetea el timeout de seguridad (`thrownTimer`); un cluster de criaturas golpeándose lo resetearía indefinidamente y quedarían colgadas en el aire.
- `RecoverIfStuckOffMesh`: red de seguridad de cold-start — un handoff fallido (primera carga antes de bakear, pull tardío, rebake) puede dejar una criatura kinematic FUERA de la malla; un criador encerrado se re-ancla sin tocar el censo del corral ni cancelar su huevo (`Release` cancelaría la cría); uno libre cae a física.
- `TickThrown`: el settle solo cuenta si está lento Y apoyado en el piso — velocidad baja en medio de un rebote o cayendo de un borde no cuenta.
- `IgnoresChainKnock`: valida que el atacante en cadena sea el mismo que el original y que aún esté en ventana de inmunidad (0.8s).

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
- [[Index/23 - Arena Sandbox y Expedicion]] (S100: clash integration)
- [[MoriMonchiVault/Index/14 - Social V2]] (S65 Fighting mode)

## Conexiones

**Entrada:**
- `MoriMochiAgent.Initialize()` → llama `CaptureNavAnchor(pos)` si on-mesh (S69)
- `AgentPhysics.DetachToPhysics()` — **S69** llama `CaptureNavAnchor()` si on-mesh
- `AgentSocial.TickSocializing()` — llama Knock(force, stress=false) en modo Fighting
- **S100:** `MoriMochiAgent.ReceiveClashHit()` — llama Knock(force, stress=false) desde choque
- `MoriMonchiController.Throw()` — llama OnThrow vía IThrowable
- Colisiones en HandleCollisionEnter — cadena de knockes entre criaturas
- **S100:** `AgentClash.ReceiveHit()` → marca knockedByClash, llamará a `owner.NotifyKnocked()` (que cancela clash)

**Salida:**
- `AgentContext.State` — Thrown/Recovering durante ragdoll
- `CreatureDNA.Needs.Affect` — resta por impacto si stress=true
- Rigidbody velocity — impulso aplicado
- `AgentContext.Agent` — warp + rejoin en rescate (S69)
- **S100:** `owner.NotifyRecovered()` — avisa a [[AgentClash]] del fin de recuperación
- **S100:** `owner.IgnoresChainKnock(otherAgent)` — consulta [[AgentClash]] para validar chain immunity
