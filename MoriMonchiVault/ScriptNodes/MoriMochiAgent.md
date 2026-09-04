---
tags: [script, world, ai]
---

# MoriMochiAgent.cs

**Ruta:** `World/AI/MoriMochiAgent.cs`

**Responsabilidad:** Núcleo delgado que orquesta la vida de una criatura en el mundo (comportamiento autónomo + física de lanzamiento). Compone ocho colaboradores internos: `AgentContext` (estado compartido), `AgentBrain` (máquina de estados NavMesh), `AgentPhysics` (handoff ragdoll), `AgentConfinement` (pens/cortejo), `AgentSenses` (percepción social throttled), `AgentSocial` (decisiones y comportamiento social), `AgentExpedition` (evaluación y persecución de objetivos recolectables) y **S100 NUEVO:** `AgentClash` (combate físico). Implementa `IThrowable` (agarrar/lanzar/knock) e `IInteractable` (petting). Ciclo de vida: `Initialize()` (wiring, setup NavMesh), `Rebind()` (reload rápido), `PrepareForPool()` (pooling). Update() despachador de ticks por estado; FixedUpdate() para FixedTick del physics. Expone fachada pública inmutable (`DNA`, `Intent`, `Percepts`, `CollectedMaterial`, `ExpeditionTarget`, `SocialPartner`, `ClashTarget`, `ClashGesture`, `IsClashTargetable`, `Team`, etc.). **S55 RESUELTO:** ya NO es partial; composición pura. **S64:** agregados AgentSenses y AgentSocial. **S65:** AgentSocial nuevos modos Sleeping/Fighting. **S69:** Petting hold-E, HandFeed state. **S97:** AgentExpedition, estado Expedition. **S98:** Drivers visuales (gaze, gesture). **S99:** Team propagado. **S100 NUEVO:** AgentClash, estado Clashing, Intent Clashing/Dazed, UnityEvents clash (onClashTell/onClashHit/onKnocked), métodos internos ReceiveClashHit/NotifyKnocked/NotifyRecovered, fachada ClashTarget/ClashGesture/IsClashTargetable/ForceClash.

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
| `HandFeed` | S69 AgentBrain | Aceptando comida de la mano del jugador |
| `Expedition` | S97 AgentExpedition | Persiguiendo mineral recolectable; beat Noticing/Moving/Taking/Losing |
| `Clashing` | **S100 NUEVO** AgentClash | Combatiendo (estados Anticipating/Striking/Resolving/Dazed internos) |

## Propiedades (fachada) S100

**Esenciales:**
- `DNA → CreatureDNA` — read-only
- `Intent → CreatureIntent` — intención actual (prioridad: Clashing → Socializing → Expedition → brain)
- `IsHeld`, `IsAirborne`, `IsPenned`, `IsForSale`, `IsRecovering` — estados del agent
- `IsInFriendlyReaction`, `IsBeingPetted`, `CanBePetted` — player interacción
- `IsCourting`, `IsSocializing` — social states

**Percepciones y objetivos (S97-S100):**
- `Percepts → IReadOnlyList<Percept>` — percepciones pobladas por AgentSenses
- `CollectedMaterial → int` — acumulador de material recolectado
- `ExpeditionTarget → Transform` — transform del mineral actual o null
- `SocialPartner → MoriMochiAgent` — agente con el que socializa o null
- `Team → ExpeditionTeam` — bando del agente (None/Player/Rival)

**Clash (S100 NUEVO):**
- `ClashTarget → MoriMochiAgent` — rival actual del combate (Anticipating/Striking) o null
- `ClashGesture → string` — nombre del gesto a disparar (TellGesture/StrikeGesture) según fase
- `IsClashTargetable → bool` — si puede ser golpeado (valida gracia post-golpe)
- `ForceClash(ClashMoveSO move, MoriMochiAgent rival) → bool` — dev tool para forzar ataque

## Ciclo de Actualización S100

```csharp
Update():
  brain.TickAlways(dt)     // decay necesidades
  senses.Tick()            // scan perceivables
  
  switch (ctx.State):
    Idle/Roaming → if (!clash.TryEngage()) {if (!expedition.TryEngage()) social.TryEngage()}
                     S100: clash.TryEngage() antes que expedition/social
    Thrown       → clash.TickAirborne()  // S100: detecta impacto en picada
                     physics.TickThrown()
    Expedition   → expedition.TickExpedition()
                     S100: saltea si clash.TryEngage() activo
    Clashing     → clash.TickClashing()  // S100 NUEVO
    Socializing  → social.TickSocializing()
```

## UnityEvents (S100 NUEVO)

Serializados en "Tuning/Presentation" tab, wiring con MMF_Player para VFX:
- `onClashTell` — disparado al Begin (Anticipating), aviso visual
- `onClashHit` — disparado al Impact, feedback del golpe
- `onKnocked` — disparado al ReceiveHit, reacción de ser golpeado

## Métodos internos para Clash (S100 NUEVO)

- `ReceiveClashHit(MoriMochiAgent attacker, Vector3 force)` — combo: marca clash.ReceiveHit(attacker) + physics.Knock(force, stress=false)
- `NotifyKnocked()` — llamado desde physics.Knock durante cadena; cancela clash combate
- `NotifyRecovered()` — llamado desde physics.TickRecovering al levantarse; permite decisión Dazed o roam
- `IgnoresChainKnock(MoriMochiAgent other) → bool` — delega a clash.IgnoresChainKnock()

## Cambios S100: Combate Físico

**Línea 20:** Agregado `AgentClash clash` colaborador

**Línea 50:** Inicialización en Awake:
```csharp
clash = new AgentClash(this, ctx);
```

**Línea 115:** ResetForReuse ahora llama `clash.ResetForReuse()`

**Líneas 139-140:** TryEngage prioridad en Idle/Roaming:
```csharp
if (!clash.TryEngage() && !expedition.TryEngage()) social.TryEngage()
```

**Línea 142:** Tick en Thrown ahora llama `clash.TickAirborne()` (picada):
```csharp
case AgentState.Thrown: clash.TickAirborne(); physics.TickThrown(); break;
```

**Línea 149:** En Expedition, salta si clash activo:
```csharp
case AgentState.Expedition: if (clash.TryEngage()) expedition.ResetForReuse(); else expedition.TickExpedition(); break;
```

**Línea 150:** Tick de clash (NUEVO estado):
```csharp
case AgentState.Clashing: clash.TickClashing(); break;
```

**Línea 179-183:** Intent ahora delega a clash si en Clashing:
```csharp
public CreatureIntent Intent =>
    ctx.State == AgentState.Clashing ? clash.Intent :
    ctx.State == AgentState.Socializing ? social.Intent :
    ctx.State == AgentState.Expedition ? expedition.Intent :
    brain.Intent;
```

**Línea 191-194:** Fachada de clash pública:
```csharp
public MoriMochiAgent ClashTarget => clash.Target;
public string ClashGesture => clash.Gesture;
public bool IsClashTargetable => clash.IsTargetable;
public bool ForceClash(ClashMoveSO move, MoriMochiAgent rival) => clash.ForceMove(move, rival);
```

**Líneas 229-233:** Métodos internos para flujo clash-physics:
```csharp
internal void RequestPlayfulKnock(Vector3 force) => physics.Knock(force, false);
internal void ReceiveClashHit(MoriMochiAgent attacker, Vector3 force) { clash.ReceiveHit(attacker); physics.Knock(force, false); }
internal void NotifyKnocked() => clash.Cancel();
internal void NotifyRecovered() => clash.OnRecovered();
internal bool IgnoresChainKnock(MoriMochiAgent other) => clash.IgnoresChainKnock(other);
```

**Líneas 517-521:** UnityEvents nuevos:
```csharp
[SerializeField] internal UnityEvent onClashTell;
[SerializeField] internal UnityEvent onClashHit;
[SerializeField] internal UnityEvent onKnocked;
```

## Invariantes S100

- **Clash antes que Expedition/Social:** Maximiza oportunidades de combate automático
- **TickAirborne en Thrown:** Permite detectar impacto en picada (Wings dive) durante vuelo
- **ReceiveClashHit sin estrés:** Golpe de choque es ragdoll sin Affect adicional (el estrés viene del choque mismo)
- **NotifyRecovered post-Dazed:** Permite transición a counter-attack o retrete sin volver a Roam inmediatamente

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]

## Conexiones

**Colaboradores internos:**
- [[AgentContext]], [[AgentBrain]], [[AgentPhysics]], [[AgentConfinement]], [[AgentSenses]], [[AgentSocial]], [[AgentExpedition]], **S100:** [[AgentClash]]

**Datos & servicios:**
- [[CreatureDNA]], [[RoleWorldProfileSO]], [[Perceivable]]
- **S100:** [[ClashTuningSO]], [[ClashMoveSO]], [[AgentClash]]

**Realismo visual (S98-S100):**
- [[MonchiGazeDriver]] (cabeza hacia targets)
- [[MonchiGestureDriver]] (gestos por intent, **S100:** lee ClashGesture)
- [[MonchiGestureSetSO]] (mapping intent → gesto, **S100:** Dazed → "No")
- [[MonchiLocomotionAnimator]] (ejecuta gestos)
- **S100:** [[MonchiMoodDriver]] (Clashing → Enojado, Dazed → Mareado)

**Visual de arena (S100):**
- **S100:** [[ArenaCameraDirector]] (enfoca combatientes)
- **S100:** [[ArenaCueOverlay]] (dibuja flecha de clash)

**Otra:**
- [[ArenaSandbox]] (crea agentes, setea Team)
- [[PlayerController]], [[HotbarController]], [[NameTag]]
