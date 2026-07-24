---
tags: [script, world, ai]
---

# MoriMochiAgent.cs

**Ruta:** `World/AI/MoriMochiAgent.cs`

**Responsabilidad:** Núcleo delgado que orquesta la vida de una criatura en el mundo (comportamiento autónomo + física de lanzamiento). Compone seis colaboradores internos: `AgentContext` (estado compartido), `AgentBrain` (máquina de estados NavMesh), `AgentPhysics` (handoff ragdoll), `AgentConfinement` (pens/cortejo), `AgentSenses` (percepción social throttled) y `AgentSocial` (decisiones y comportamiento social). Implementa `IThrowable` (agarrar/lanzar/knock) e `IInteractable` (petting). Ciclo de vida: `Initialize()` (wiring, setup NavMesh), `Rebind()` (reload rápido), `PrepareForPool()` (pooling). Update() despachador de ticks por estado; FixedUpdate() para FixedTick del physics. Expone fachada pública inmutable (`DNA`, `Intent`, `IsHeld`, `IsAirborne`, `IsPenned`, `CanBePetted`, etc.) y switchboard interno para que colaboradores pidan operaciones. **S55 RESUELTO:** ya NO es partial; composición pura. **S64:** agregados AgentSenses y AgentSocial. **S65:** AgentSocial nuevos modos Sleeping/Fighting.

## Máquina de Estados

| Estado | Responsable | Descripción |
|--------|-------------|-------------|
| `Idle` | AgentBrain | Esperando aleatorio |
| `Roaming` | AgentBrain | NavMesh autónomo |
| `Reacting` | AgentBrain | Persigue/huye del jugador |
| `Carried` | AgentPhysics | Agarrado por el jugador |
| `Thrown` | AgentPhysics | Ragdoll en aire, refleja bounces |
| `Recovering` | AgentPhysics | Get-up post-lanzamiento |
| `SeekingNeed` | AgentBrain | Navega a estación crítica |
| `UsingStation` | AgentBrain | Consume de estación |
| `Courting` | AgentConfinement | Danza de apareamiento |
| `Socializing` | AgentSocial | Acercándose, persiguiendo, durmiendo o peleando con otro MoriMochi |

## Estructura (Composición S55 + S64 + S65)

```
MoriMochiAgent (fachada pública)
  ├─ AgentContext (estado puro: componentes, DNA, profile, masks, percepts)
  ├─ AgentBrain (ticks: Idle, Roaming, Reacting, SeekingNeed, UsingStation)
  ├─ AgentPhysics (handoff NavMesh ⇄ Rigidbody, ragdoll, bounce, recovery)
  ├─ AgentConfinement (pens, courtship, rebake prep)
  ├─ AgentSenses (escaneo throttled de Perceivables, población de ctx.Percepts)
  └─ AgentSocial (decisiones y tick de interacciones sociales: Approach/PlayChase/SleepTogether/Fight)
```

Cada colaborador tiene UNA responsabilidad; MoriMochiAgent = dispatcher + fachada pública.

## Métodos Públicos

**Lifecycle:**
- `Initialize(CreatureDNA dna, RoleWorldProfileSO profileTable, Transform player)` — wiring inicial, setup NavMesh, NameTag binding
- `Rebind(CreatureDNA dna, RoleWorldProfileSO profileTable)` — reload: rebind DNA + profile, NO reinicia NavMesh
- `PrepareForPool()` — pre-pool: libera estaciones, detach si es necesario

**Propiedades (fachada):**
- `DNA → CreatureDNA` — read-only
- `Intent → CreatureIntent` — intención actual (Idle, Wandering, Following, Eating, Fleeing, Socializing, Chasing, SleepingTogether, Fighting, etc.)
- `IsHeld → bool` — en Carried
- `IsAirborne → bool` — en Thrown
- `IsPenned → bool` — confinado en pen
- `IsForSale → bool` — ocupante de StoreContainer
- `IsRecovering → bool` — en Recovering
- `IsInFriendlyReaction → bool` — Reacting pero no fleeing
- `IsBeingPetted → bool` — petting display (1.5s)
- `CanBePetted → bool` — condiciones de petting cumplidas
- `IsPlayerFacingMe() → bool` — player está a petRadius y mira hacia esta criatura
- `IsCourting → bool` — en Courting
- `IsSocializing → bool` — en Socializing (acercándose, jugando, durmiendo o peleando)
- `Condition → CreatureCondition` — Healthy/Sick/InNeed (derived from needs vs thresholds)

**Eventos:**
- `OnEmote` — evento que dispara cuando AgentSocial emite emoción (suscriptor: MonchiEmoteBubble, NameTag)

**Interacción:**
- `Interact()` — E-acariciar (del gameplay)
- `Launch(Vector3 launchPos, launchVelocity)` — cannon spawn
- `OnGrab(Transform anchor), OnRelease(), OnThrow(Vector3)` — IThrowable contract
- `Knock(Vector3 force)` — golpeado por otra criatura
- `EnterConfinement(MoriMochiContainer pen) → bool` — confinamiento a pen
- `EnterCourtship(MoriMochiAgent partner, Vector3 anchor), ExitCourtship()` — cortejo
- `TryJoinSocialPlay(MoriMochiAgent initiator) → bool` — **S64 NUEVO** handshake receptor: otro agente pide juego
- `TryJoinSocialSleep(MoriMochiAgent initiator, NeedStation station, Vector3 fallbackSpot) → bool` — **S65 NUEVO** handshake receptor: otro agente invita a dormir juntos (pasa su estación reservada y el punto fallback)
- `TryJoinSocialFight(MoriMochiAgent initiator) → bool` — **S65 NUEVO** handshake receptor: otro agente inicia pelea de juego
- `RequestPlayfulKnock(Vector3 force)` — **S65 NUEVO** switchboard interno: knock sin penalización de Affect (AgentPhysics.Knock(force, stress:false)) para el final de la pelea
- `CompleteSocialPlayFromPartner()` — **S64 NUEVO** notificación one-way: compañero completó juego

**Switchboard interno (RequestRoam, etc.):**
- `RequestRoam()` — AgentBrain.EnterRoaming()
- `RequestReleaseStation()` — AgentBrain.ReleaseStation()
- `RequestEnterRagdoll()` — AgentPhysics.EnterRagdoll()
- `RequestDetachToPhysics()` — AgentPhysics.DetachToPhysics()
- `RequestRejoinNavMesh(Vector3 desired, int mask) → bool` — AgentPhysics.RejoinNavMesh()
- `RequestReleaseFromPen()` — AgentConfinement.ReleaseFromPen()
- `AdjustRoamForAvoidance(Vector3) → Vector3` — **S64 NUEVO** AgentSocial.AdjustRoamForAvoidance() filtro repulsivo

**Internal:**
- `EmitEmote(EmoteKind) → void` — dispara OnEmote (usado por AgentSocial)

## Campos Tuning (Odin Tabs)

**Tuning > References:**
- `nameTag` (NameTag) — label world-space

**Tuning > Movement:**
- NavMesh sampling: `sampleRadius`
- Proximity: `repathInterval`, `followDuration`, `reactCooldown`, `petRadius`, `petLookAngle`
- Breeding: `breedingAreaName`, `courtSpeedMultiplier`, `courtOrbitRadius`, `courtAngularSpeed`, `courtLookahead`, `courtRepath`, `courtTendRadius`, `courtTendInterval`
- Read-only: `ProfileProximityRadius`, `ProfileRoamRadius`, `ProfileFollowDistance` (viven en RoleWorldProfile)

**Tuning > Needs:**
- Live readouts (play mode): `Health`, `Energy`, `Affect` (progress bars)
- Decay per second: `healthDecayPerSecond`, `energyDecayPerSecond`, `affectDecayPerSecond`
- Critical thresholds: `criticalHealth`, `criticalEnergy`, `criticalAffect`
- Stress events: `affectOnThrow`, `affectOnHardCollision`, `hardImpactThreshold`, `affectOnPet`

**Tuning > Stats:**
- Live readouts (play mode): `StatCon`, `StatAtk`, `StatSpd`, `StatDef`, `StatLck`, `StatEva` (Base → Final + delta)

**Tuning > Physics:**
- Hold feel: `followSpeed`, `settleSpeed`, `settleDelay`
- Throw: `thrownLinearDamping`, `thrownAngularDamping`, `groundCheckDistance`, `maxThrownTime`, `offMeshRecoverDelay`
- Bounce: `bounciness`, `maxBounces`, `minBounceSpeed`, `bounceSpin`
- Knock: `knockTransfer`, `knockUpBias`
- Recovery: `downedDelay`, `getUpDuration`, `getUpJitter`

**Tuning > Presentation:**
- UnityEvents: `onGrab`, `onThrow`, `onBounce`, `onLand`, `onGetUp`, `onPet`

**Tuning > Dev:**
- Live readouts: `CurrentState`, `NavStatus`, `CourtInfo`
- Toggles: `forceRagdoll`, `logStateTransitions`, `snapWarnThreshold`
- Buttons: `DevForceRagdoll()`, `DevForceRoam()`

## Ciclo de Actualización

```csharp
Update():
  DevTrackState()  // logging
  if (forceRagdoll && NavMesh-controlled) → ragdoll
  physics.RecoverIfStuckOffMesh()
  brain.TickAlways(dt)  // needs decay
  senses.Tick()  // S64: scan perceivables, populate ctx.Percepts
  
  switch (ctx.State):
    Idle       → brain.TickIdle()
                 if (state still Idle) social.TryEngage()  // S64: intenta iniciar social
    Roaming    → brain.TickRoaming()
                 if (state still Roaming) social.TryEngage()
    Reacting   → brain.TickReacting()
    Thrown     → physics.TickThrown()
    Recovering → physics.TickRecovering()
    SeekingNeed   → brain.TickSeekingNeed()
    UsingStation  → brain.TickUsingStation()
    Courting      → confinement.TickCourting()
    Socializing   → social.TickSocializing()  // S64/S65: tick social modes
    Carried    → (nothing, follow runs in FixedUpdate)

FixedUpdate():
  physics.FixedTick()  // carry follow
  
OnCollisionEnter(Collision):
  physics.HandleCollisionEnter()
  
OnTriggerEnter(Collider):
  physics.HandleTriggerEnter()
```

## Eventos Suscritos

- `GameEvents.OnNavMeshWillRebake` → `confinement.OnNavMeshWillRebake()`
- `GameEvents.OnNavMeshRebaked` → `confinement.OnNavMeshRebaked()`

## Gizmos

Dibuja en Play mode (cuando initialized):
- Esfera amarilla: ProximityRadius (detección jugador)
- Esfera azul: RoamRadius (destino aleatorio)
- Esfera magenta: petRadius
- Esfera verde: FollowDistance (si reacciona amistosamente)
- Punto coloreado: rol tint
- Línea magenta: destino actual si tiene path

## Vinculado a

- [[Index/06 - Player & World]]
- [[Index/02 - Genetics & Breeding]]
- [[MoriMonchiVault/Index/14 - Social V2]]

## Conexiones

**Colaboradores internos:**
- [[AgentContext]], [[AgentBrain]], [[AgentPhysics]], [[AgentConfinement]], [[AgentSenses]], [[AgentSocial]]

**Datos & servicios:**
- [[CreatureDNA]] — DNA viva
- [[RoleWorldProfileSO]], [[RoleWorldProfile]] — perfil comportamiento
- [[NeedStationRegistry]] — búsqueda de estaciones
- [[PerceivableRegistry]] — S64 índice social
- [[SocialGraphService]] — S65 historial dinámico
- [[CombatStats]], [[EquipmentStats]] — stats (live readout)

**Visualización & UI:**
- [[MoriMonchiController]] — contiene este + visualizer
- [[MoriMonchiVisualizer]] — assembly 3D
- [[NameTag]] — label world-space
- [[MonchiEmoteBubble]] — S64 burbuja de emoción
- [[MoriMonchiProceduralAnimator]] — lee transforms para animation

**Eventos & física:**
- [[GameEvents]] — OnNavMeshRebake, etc.
- [[IThrowable]], [[IInteractable]] — interfaces

**Mundo:**
- [[MoriMochiContainer]] — pen/breeding confinement
- [[NeedStation]] — estaciones (Feeder, RestZone, PlayZone)
- [[MoriMochiSpawner]] — instancia y wirea
- [[Perceivable]] — S64 marcas sociales

## Notas S65

- **Nuevos modos sociales:** AgentSocial ahora maneja Sleeping (busca RestZone, regen energía) y Fighting (abalanzadas, -Affect). Ambos son gateados por energía/salud.
- **Historial dinámico:** AgentSenses consulta `SocialGraphService.EffectiveAffinity()` para afinidad seed + delta.
- **Handshakes:** TryJoinSocialSleep y TryJoinSocialFight siguen el patrón de TryJoinSocialPlay (fachada → internos TryJoinSleep/TryJoinFight de AgentSocial).

## Notas S64

- **Percepción y conducta social:** AgentSenses scannea Perceivables en throttle (2–4s), poblando ctx.Percepts. AgentSocial consulta en TryEngage() para decidir acción.
- **Estados nuevos:** Socializing mapea a CreatureIntent múltiples (Socializing, Chasing, SleepingTogether, Fighting).
- **Emociones visuales:** OnEmote event dispara pictogramas en MonchiEmoteBubble.
- **Evitación de roam:** AgentBrain.NextRoamDestination() llama AdjustRoamForAvoidance() para empujar puntos lejos de Avoid rules.

## Notas S55

- **Refactor S55 resuelto:** Deuda Fase 8 cerrada. Ya NO partial class; composición de mini-managers.
- **Fachada intacta:** Métodos públicos y propiedades sin cambios desde vista externa.
- **Switchboard:** RequestRoam, RequestReleaseStation, etc. son puertas de entrada de colaboradores.
- **Tuning absorbido:** Todos los campos del viejo .Tuning.cs ahora viven como tabs Odin.
- **Gizmos preservados:** Los del viejo .Debug.cs ahora en OnDrawGizmos().
