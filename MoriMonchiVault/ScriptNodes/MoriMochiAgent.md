---
tags: [script, world, ai]
---

# MoriMochiAgent.cs

**Ruta:** `World/AI/MoriMochiAgent.cs`

**Responsabilidad:** Núcleo delgado que orquesta la vida de una criatura en el mundo (comportamiento autónomo + física de lanzamiento). Compone siete colaboradores internos: `AgentContext` (estado compartido), `AgentBrain` (máquina de estados NavMesh), `AgentPhysics` (handoff ragdoll), `AgentConfinement` (pens/cortejo), `AgentSenses` (percepción social throttled), `AgentSocial` (decisiones y comportamiento social) y **S97 NUEVO:** `AgentExpedition` (evaluación y persecución de objetivos recolectables). Implementa `IThrowable` (agarrar/lanzar/knock) e `IInteractable` (petting). Ciclo de vida: `Initialize()` (wiring, setup NavMesh), `Rebind()` (reload rápido), `PrepareForPool()` (pooling). Update() despachador de ticks por estado; FixedUpdate() para FixedTick del physics. Expone fachada pública inmutable (`DNA`, `Intent`, `Percepts`, `CollectedMaterial`, `ExpeditionTarget`, `SocialPartner`, etc.). **S55 RESUELTO:** ya NO es partial; composición pura. **S64:** agregados AgentSenses y AgentSocial. **S65:** AgentSocial nuevos modos Sleeping/Fighting. **S69:** Petting hold-E, HandFeed state. **S97 NUEVO:** AgentExpedition, estado Expedition con prioridad sobre Social, propiedades de fachada para percepciones y objetivo.

## Máquina de Estados

| Estado | Responsable | Descripción |
|--------|-------------|-------------|
| `Idle` | AgentBrain | Esperando aleatorio |
| `Roaming` | AgentBrain | NavMesh autónomo |
| `Reacting` | AgentBrain | Persigue/huye del jugador (o petting hold-E) |
| `Carried` | AgentPhysics | Agarrado por el jugador |
| `Thrown` | AgentPhysics | Ragdoll en aire, refleja bounces |
| `Recovering` | AgentPhysics | Get-up post-lanzamiento |
| `SeekingNeed` | AgentBrain | Navega a estación crítica |
| `UsingStation` | AgentBrain | Consume de estación |
| `Courting` | AgentConfinement | Danza de apareamiento |
| `Socializing` | AgentSocial | Acercándose, persiguiendo, durmiendo o peleando con otro MoriMochi |
| `HandFeed` | **S69** AgentBrain | Aceptando comida de la mano del jugador |
| `Expedition` | **S97 NUEVO** AgentExpedition | Persiguiendo mineral recolectable |

## Estructura (Composición S55 + S64 + S65 + S69 + S97)

```
MoriMochiAgent (fachada pública)
  ├─ AgentContext (estado puro: componentes, DNA, profile, masks, percepts)
  ├─ AgentBrain (ticks: Idle, Roaming, Reacting, SeekingNeed, UsingStation, HandFeed; S69: petting+handFeed)
  ├─ AgentPhysics (handoff NavMesh ⇄ Rigidbody, ragdoll, bounce, recovery; S69: void-fall rescue)
  ├─ AgentConfinement (pens, courtship, rebake prep)
  ├─ AgentSenses (escaneo throttled de Perceivables, población de ctx.Percepts)
  ├─ AgentSocial (decisiones y tick de interacciones sociales: Approach/PlayChase/SleepTogether/Fight)
  └─ AgentExpedition (S97 NUEVO: evaluación de reglas, persecución de objetivos, recolección)
```

Cada colaborador tiene UNA responsabilidad; MoriMochiAgent = dispatcher + fachada pública.

## Métodos Públicos

**Lifecycle:**
- `Initialize(CreatureDNA dna, RoleWorldProfileSO profileTable, Transform player, MonchiVisualBankSO visualBank, FurTypeDatabaseSO furDb)` — **S97 notas:** wiring inicial, setup NavMesh, NameTag binding, llama `physics.CaptureNavAnchor(pos)` si on-mesh para rescate de void-fall, llama `expedition.ResetForReuse()` en `RestoreNavMeshControl()` para limpiar expedición anterior.
- `Rebind(CreatureDNA dna, RoleWorldProfileSO profileTable)` — reload: rebind DNA + profile, NO reinicia NavMesh
- `PrepareForPool()` — pre-pool: libera estaciones, detach si es necesario

**Propiedades (fachada):**
- `DNA → CreatureDNA` — read-only
- `Intent → CreatureIntent` — intención actual. **S97:** ahora prioriza Socializing → Expedition → brain (línea 172-175)
- `IsHeld → bool` — en Carried
- `IsAirborne → bool` — en Thrown
- `IsPenned → bool` — confinado en pen
- `IsForSale → bool` — ocupante de StoreContainer
- `IsRecovering → bool` — en Recovering
- `IsInFriendlyReaction → bool` — Reacting pero no fleeing
- `IsBeingPetted → bool` — **S69** petting display (mientras petTimer > 0)
- `CanBePetted → bool` — **S69** condiciones de petting cumplidas (Reacting + amistosa + facing)
- `IsPlayerFacingMe() → bool` — player está a petRadius y mira hacia esta criatura
- `IsCourting → bool` — en Courting
- `IsSocializing → bool` — en Socializing (acercándose, jugando, durmiendo o peleando)
- `Condition → CreatureCondition` — Healthy/Sick/InNeed (derived from needs vs thresholds)
- **S97 NUEVAS:**
  - `Percepts → IReadOnlyList<Percept>` — percepciones sociales pobladas por `AgentSenses`; leída por UI/overlay
  - `CollectedMaterial → int` — acumulador de material recolectado (sesión local)
  - `ExpeditionTarget → Transform` — transform del recolectable actual siendo perseguido (null si idle)
  - `SocialPartner → MoriMochiAgent` — agente con el que está interactuando (null si idle)

**Eventos:**
- `OnEmote` — evento que dispara cuando AgentSocial emite emoción (suscriptor: MonchiEmoteBubble, NameTag)

**Interacción:**
- `BeginPetting()` — **S69 NUEVO** press-E: entra petting dentro de Reacting vía `brain.BeginPetSession()`
- `EndPetting()` — **S69 NUEVO** release-E: cancela petting vía `brain.EndPetSession()`
- `Launch(Vector3 launchPos, launchVelocity)` — cannon spawn
- `OnGrab(Transform anchor), OnRelease(), OnThrow(Vector3)` — IThrowable contract
- `Knock(Vector3 force)` — golpeado por otra criatura
- `Knock(Vector3 force, bool stress)` — **S65 NUEVO** golpeado con opción de estrés
- `EnterConfinement(MoriMochiContainer pen) → bool` — confinamiento a pen
- `EnterCourtship(MoriMochiAgent partner, Vector3 anchor), ExitCourtship()` — cortejo
- `TryJoinSocialPlay(MoriMochiAgent initiator) → bool` — **S64 NUEVO** handshake receptor: otro agente pide juego
- `TryJoinSocialSleep(MoriMochiAgent initiator, NeedStation station, Vector3 fallbackSpot) → bool` — **S65 NUEVO** handshake receptor: otro agente invita a dormir juntos
- `TryJoinSocialFight(MoriMochiAgent initiator) → bool` — **S65 NUEVO** handshake receptor: otro agente inicia pelea de juego
- `RequestPlayfulKnock(Vector3 force)` — **S65 NUEVO** switchboard interno: knock sin penalización de Affect para el final de la pelea
- `CompleteSocialPlayFromPartner()` — **S64 NUEVO** notificación one-way: compañero completó juego

**Switchboard interno:**
- `RequestRoam()` — AgentBrain.EnterRoaming()
- `RequestReleaseStation()` — AgentBrain.ReleaseStation()
- `RequestEnterRagdoll()` — AgentPhysics.EnterRagdoll()
- `RequestDetachToPhysics()` — AgentPhysics.DetachToPhysics()
- `RequestRejoinNavMesh(Vector3 desired, int mask) → bool` — AgentPhysics.RejoinNavMesh()
- `RequestReleaseFromPen()` — AgentConfinement.ReleaseFromPen()
- `AdjustRoamForAvoidance(Vector3) → Vector3` — **S64 NUEVO** AgentSocial.AdjustRoamForAvoidance() filtro repulsivo

**Internal:**
- `EmitEmote(EmoteKind) → void` — dispara OnEmote (usado por AgentSocial y **S97:** AgentExpedition)

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
- Stress events: `affectOnThrow`, `affectOnHardCollision`, `hardImpactThreshold`
- **S69 NUEVOS (Petting):** `petAffectPerSecond` (2), `petRampPerSecond` (0.1), `petMaxDuration` (30), `petEmoteInterval` (2)
- **S69 NUEVOS (HandFeed):** `feedNoticeRadius` (3), `feedDistance` (1.5), `feedShyBelow` (0.4), `feedShyDistance` (3), `feedHesitateSeconds` (5), `feedEatSeconds` (3), `feedHungerThreshold` (40), `feedHealthBoost` (30), `feedAffectBoost` (5), `feedCooldown` (10)

**Tuning > Stats:**
- Live readouts (play mode): `StatCon`, `StatAtk`, `StatSpd`, `StatDef`, `StatLck`, `StatEva` (Base → Final + delta). **S75:** usa `CreatureStats.GetEffectiveStats()` para base

**Tuning > Physics:**
- Hold feel: `followSpeed`, `settleSpeed`, `settleDelay`
- Throw: `thrownLinearDamping`, `thrownAngularDamping`, `groundCheckDistance`, `maxThrownTime`, `offMeshRecoverDelay`
- Bounce: `bounciness`, `maxBounces`, `minBounceSpeed`, `bounceSpin`
- Knock: `knockTransfer`, `knockUpBias`
- Recovery: `downedDelay`, `getUpDuration`, `getUpJitter`
- **S69 NUEVO:** `voidFallDrop` (20) — threshold de caída bajo el cual dispara rescate de void-fall

**Tuning > Presentation:**
- UnityEvents: `onGrab`, `onThrow`, `onBounce`, `onLand`, `onGetUp`, `onPet` (S69: onPet nuevo), **S97 NUEVOS:** `onTakeOff`, `onFlyLand` (enchufados en hijo `Feedbacks/` del prefab; ver invariantes)

**Tuning > Dev:**
- Live readouts: `CurrentState`, `NavStatus`, `CourtInfo`, **S69 NUEVO:** `Dials` (muestra Sociability/Boldness)
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
                 if (state still Idle    && !expedition.TryEngage()) social.TryEngage()  // S97: Expedition antes que Social
    Roaming    → brain.TickRoaming()
                 if (state still Roaming && !expedition.TryEngage()) social.TryEngage()
    Reacting   → brain.TickReacting()
    Thrown     → physics.TickThrown()  // S69: con detección void-fall
    Recovering → physics.TickRecovering()
    SeekingNeed   → brain.TickSeekingNeed()
    UsingStation  → brain.TickUsingStation()
    Courting      → confinement.TickCourting()
    Socializing   → social.TickSocializing()  // S64/S65: tick social modes
    HandFeed      → brain.TickHandFeed()  // S69 NUEVO
    Expedition    → expedition.TickExpedition()  // S97 NUEVO
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

## Cambios S97

**Nuevos colaboradores:**
- `expedition` (AgentExpedition) — instanciado en Awake, inicializado en RestoreNavMeshControl

**Nuevas propiedades de fachada:**
- `Percepts → IReadOnlyList<Percept>` — acceso read-only a percepciones pobladas por senses
- `CollectedMaterial → int` — lectura del acumulador de expedition
- `ExpeditionTarget → Transform` — target actual o null
- `SocialPartner → MoriMochiAgent` — social partner actual o null

**Update() cambios:**
- Líneas 133-134: En Idle y Roaming, `expedition.TryEngage()` se llama ANTES que `social.TryEngage()` (prioridad)
- Línea 143: nuevo caso `AgentState.Expedition: expedition.TickExpedition()`

**Intent property cambios (línea 172-175):**
```csharp
public CreatureIntent Intent =>
    ctx.State == AgentState.Socializing ? social.Intent :
    ctx.State == AgentState.Expedition  ? expedition.Intent :    // S97 NEW
    brain.Intent;
```

**RestoreNavMeshControl() cambios:**
- Línea 110: `expedition.ResetForReuse()` agregado

**Prefab changes S97:**
- Hijo `Feedbacks/` nuevo: contiene 5-6 `MMF_Player` enchufados a eventos de Feel
- `onTakeOff` y `onFlyLand` de MonchiLocomotionAnimator enchufados a MMF_Players en Feedbacks/
- Todos los MMF usan `MMF_ParticlesInstantiation` en pool (ver Index/23 y regla de Feel)

## Invariantes S93 + S97

- `RestoreNavMeshControl`: un agente reusado del pool conserva el estado de su vida anterior si no se resetea; es el reset idempotente llamado al inicio de `Initialize`. Ahora también resetea expedición.
- `PrepareForPool` / `AgentConfinement.DetachForReuse`: detach de reciclaje silencioso, NO es una salida del jugador — no persiste ni cancela estado de dominio (el huevo). `Release` es exclusivo de `OnGrab`.
- `Initialize` (`breedingAreaName`/`areaMask`): los agentes libres EXCLUYEN el área de cría (rodean los corrales); un agente encerrado está RESTRINGIDO a ella; sin área configurada (-1) cae a `AllAreas`.
- El estado `Carried` no tiene tick propio: el seguimiento de carga corre en `FixedUpdate`.
- **S97:** `Expedition` es state NavMesh-controlled; incluido en `IsNavMeshControlled()` de AgentContext. Prioridad: Expedition > Social en intenciones.

## Impacto Diales Genéticos (S69 + S97)

Los knobs petting/handFeed NO son afectados directamente por Sociability/Boldness. Sin embargo:
- AgentBrain.TickHandFeed() chequea `ctx.Dna.Sociability < feedShyBelow` para dudar
- AgentSocial.End() usa `ScaledSocialCooldown(ctx.Dna.Sociability)` para cooldown social
- **S97:** AgentExpedition.TryEngage() pasa `self` a reglas; `SeekMaterialRule` usa `BoldnessBias * (boldness - 0.5) * 2` para modular scoring

Esto permite:
- **Sociable (0.8):** come rápido, interactúa frecuentemente, busca material más agresivamente
- **Tímido (0.2):** duda antes de comer, espera más entre interacciones, sesgo hacia material cercano

## Vinculado a

- [[Index/06 - Player & World]]
- [[Index/02 - Genetics & Breeding]]
- [[Index/23 - Arena Sandbox y Expedicion]] (S97)
- [[MoriMonchiVault/Index/14 - Social V2]]

## Conexiones

**Colaboradores internos:**
- [[AgentContext]], [[AgentBrain]], [[AgentPhysics]], [[AgentConfinement]], [[AgentSenses]], [[AgentSocial]], **S97:** [[AgentExpedition]]

**Datos & servicios:**
- [[CreatureDNA]] — DNA viva, diales Sociability/Boldness
- [[RoleWorldProfileSO]], [[RoleWorldProfile]] — perfil comportamiento
- [[NeedStationRegistry]] — búsqueda de estaciones
- [[PerceivableRegistry]] — S64 índice social
- [[SocialGraphService]] — S65/S69 historial dinámico
- [[CreatureStats]], [[EquipmentStats]] — stats (live readout). **S75:** cambio de CombatStats a CreatureStats
- **S97:** [[ExpeditionRulesSO]], [[ExpeditionRuleBase]], [[AgentExpedition]]

**Visualización & UI:**
- [[MoriMonchiController]] — contiene este + visualizer
- [[MoriMonchiVisualizer]] — assembly 3D
- [[NameTag]] — label world-space
- [[MonchiEmoteBubble]] — S64 burbuja de emoción
- [[MoriMonchiProceduralAnimator]] — lee transforms para animation
- [[PlayerController]] — press-E para BeginPetting, release-E para EndPetting
- [[HotbarController]] — IsOfferingFood para HandFeed
- **S97:** [[ArenaCueOverlay]], [[ArenaSandbox]] (lectura de fachada)
- **S97:** [[MonchiLocomotionAnimator]] (onTakeOff/onFlyLand events)
