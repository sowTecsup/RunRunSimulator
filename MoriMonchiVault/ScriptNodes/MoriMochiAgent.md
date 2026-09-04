---
tags: [script, world, ai]
---

# MoriMochiAgent.cs

**Ruta:** `World/AI/MoriMochiAgent.cs`

**Responsabilidad:** Núcleo delgado que orquesta la vida de una criatura en el mundo (comportamiento autónomo + física de lanzamiento). Compone siete colaboradores internos: `AgentContext` (estado compartido), `AgentBrain` (máquina de estados NavMesh), `AgentPhysics` (handoff ragdoll), `AgentConfinement` (pens/cortejo), `AgentSenses` (percepción social throttled), `AgentSocial` (decisiones y comportamiento social) y **S97 NUEVO:** `AgentExpedition` (evaluación y persecución de objetivos recolectables). Implementa `IThrowable` (agarrar/lanzar/knock) e `IInteractable` (petting). Ciclo de vida: `Initialize()` (wiring, setup NavMesh), `Rebind()` (reload rápido), `PrepareForPool()` (pooling). Update() despachador de ticks por estado; FixedUpdate() para FixedTick del physics. Expone fachada pública inmutable (`DNA`, `Intent`, `Percepts`, `CollectedMaterial`, `ExpeditionTarget`, `SocialPartner`, **S99 NUEVO:** `Team`, etc.). **S55 RESUELTO:** ya NO es partial; composición pura. **S64:** agregados AgentSenses y AgentSocial. **S65:** AgentSocial nuevos modos Sleeping/Fighting. **S69:** Petting hold-E, HandFeed state. **S97 NUEVO:** AgentExpedition, estado Expedition con prioridad sobre Social. **S99 NUEVO:** Expone `Team` desde `Perceivable.Team`.

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
| `Expedition` | **S97** AgentExpedition | Persiguiendo mineral recolectable; **S98-S99:** beat Noticing/Moving/Taking/Losing |

## Propiedades (fachada) S98

**Esenciales:**
- `DNA → CreatureDNA` — read-only
- `Intent → CreatureIntent` — intención actual (prioridad: Socializing → Expedition → brain)
- `IsHeld`, `IsAirborne`, `IsPenned`, `IsForSale`, `IsRecovering` — estados del agent
- `IsInFriendlyReaction`, `IsBeingPetted`, `CanBePetted` — player interacción
- `IsCourting`, `IsSocializing` — social states
- `Condition → CreatureCondition` — Healthy/Sick/InNeed

**Percepciones y objetivos (S97-S98):**
- `Percepts → IReadOnlyList<Percept>` — percepciones pobladas por AgentSenses (incluye Team S99)
- `CollectedMaterial → int` — acumulador de material recolectado
- `ExpeditionTarget → Transform` — transform del mineral actual o null
- `SocialPartner → MoriMochiAgent` — agente con el que socializa o null
- **S99 NUEVO:** `Team → ExpeditionTeam` — bando del agente (None/Player/Rival), leído desde `perceivable.Team`

## Ciclo de Actualización S98

```csharp
Update():
  brain.TickAlways(dt)     // decay necesidades
  senses.Tick()            // scan perceivables con Team S99
  
  switch (ctx.State):
    Idle/Roaming → if (!expedition.TryEngage()) social.TryEngage()
                     S99: social.TryEngage() saltea percepto rivales (expedition no filtra por equipo)
    Expedition   → expedition.TickExpedition()
                     S98-S99: 4 fases (Noticing/Moving/Taking/Losing) con intents discretos
    Socializing  → social.TickSocializing()
                     S99: AgentSocial saltea rivales en TryEngage() y los rechaza en CanPair() vía ExpeditionTeams.AreRivals()
    HandFeed     → brain.TickHandFeed()
```

## Nuevos drivers de realismo S98

**Hijos serializados del prefab (se wiring manual):**
- `MonchiGazeDriver` — LateUpdate: rota ModelRoot para mirar (ExpeditionTarget → SocialPartner → percept cercano)
- `MonchiGestureDriver` — Update: orquesta gestos (enter/hold/fidget) desde `MonchiGestureSetSO` según Intent

**Impacto en comportamiento visual:**
- MonchiGestureDriver reacciona a `CreatureIntent.Taking` y `CreatureIntent.Losing` (S98-S99 nuevos intents)
- MonchiGazeDriver prioriza ExpeditionTarget (mineral), mantiene atención reactiva

## Invariantes S98

- **Team propagación:** `Team` es read-only, derivado de `Perceivable.Team` (seteable vía `perceivable.SetTeam()` post-init)
- **Rivalidad en interacciones:** `ExpeditionTeams.AreRivals()` filtra Percepts en `AgentSocial.TryEngage()` y `AgentExpedition.ApproachPoint()`
- **Intents discretos:** `CreatureIntent.Taking` e `CreatureIntent.Losing` solo durante fases específicas de Expedition (popula desde `AgentExpedition.Intent`)
- **Drivers desacoplados:** MonchiGazeDriver y MonchiGestureDriver LEE Intent/ExpeditionTarget/SocialPartner públicamente; **nunca** mutan estado

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]

## Conexiones

**Colaboradores internos:**
- [[AgentContext]], [[AgentBrain]], [[AgentPhysics]], [[AgentConfinement]], [[AgentSenses]], [[AgentSocial]], [[AgentExpedition]]

**Datos & servicios:**
- [[CreatureDNA]], [[RoleWorldProfileSO]], [[Perceivable]] (**S99:** Team source)
- **S99:** [[ExpeditionTeam]], [[ExpeditionTeams]] (filtro de rivalidad)

**Realismo visual (S98):**
- **S98 NUEVO:** [[MonchiGazeDriver]] (cabeza hacia targets)
- **S98 NUEVO:** [[MonchiGestureDriver]] (gestos por intent)
- [[MonchiGestureSetSO]] (mapping intent → gesto)
- [[MonchiLocomotionAnimator]] (ejecuta gestos)

**Otra:**
- [[ArenaSandbox]] (crea agentes, setea Team vía Perceivable)
- [[ArenaCueOverlay]] (dibuja rutas/sociales, lector de Percepts/Team)
- [[PlayerController]], [[HotbarController]], [[NameTag]]
