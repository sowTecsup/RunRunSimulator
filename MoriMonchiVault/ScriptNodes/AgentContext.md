---
tags: [script, world, ai, agent, internal, data]
---

# AgentContext.cs

**Ruta:** `World/AI/AgentContext.cs`

**Responsabilidad:** Contenedor de estado puro compartido entre colaboradores (AgentBrain, AgentPhysics, AgentExpedition, AgentClash, AgentSenses, AgentSocial, AgentConfinement). Almacena refs de componentes, DNA/perfil, estado de juego, máscaras NavMesh, percepciones, **pizarrón de equipo** (S103 NUEVO). Sin lógica de estado; solo datos y helpers (SetDestinationSafe, IsMoving, PlanarDistance, etc.).

**Enum AgentState (S103 sin cambios):**
Idle, Roaming, Reacting, Carried, Thrown, Recovering, SeekingNeed, UsingStation, Courting, Socializing, HandFeed, Expedition, Clashing

**Campos Internos:**
- Refs: Owner, Body, Agent, Rb, Col (componentes)
- DNA, Profile (genética + rol)
- Player, HoldAnchor (refs externas)
- CurrentContainer (corral si confinado)
- **S103 NUEVO:** `TeamBlackboard Board` — pizarrón de equipo (inyectado por ArenaSandbox.SpawnCast)
- Occupación/expedición: Occupation, HomeExit, GuardPost (S101)
- Percepción: Percepts (List<Percept>, S64)
- NavMesh: FreeAreaMask, ConfinedAreaMask, RebakeInProgress
- State (AgentState actual)
- BaseSpeed (S98)

**Métodos Públicos:**
- `IsNavMeshControlled() → bool` — si state es controlado por navmesh
- `IsBreeding` — si DNA.BusyState == Breeding
- `IsMoving` — si agente en movimiento
- `SetStopped(bool)` — Agent.isStopped
- `SetDestinationSafe(Vector3)` — SetDestination con sample check
- `ApplyGaitSpeed()` — único dueño de Agent.speed (S98)
- `SetColliderTrigger(bool)`
- `PlanarDistanceToPlayer() → float`
- `RandomPointInBounds(Bounds) → Vector3`

**S103 Cambios:**
- Campo `TeamBlackboard Board` agregado (nullable)
- Inyectado por ArenaSandbox en SpawnCast vía `controller.Agent.SetBlackboard(board)`
- Consultado por AgentExpedition.TryGatherEngage() y AgentScout para navegación inteligente

**Invariantes:**
- Contenedor puro: sin lógica de transición de estado
- Ref compartida: todos los colaboradores leen/escriben ctx, no hay duplicación
- State autoridad única: solo colaboradores pueden cambiar State
- Board nullable: null si no en expedición, lazy-set por sandbox

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[MoriMochiAgent]], [[AgentBrain]], [[AgentPhysics]], [[AgentExpedition]], [[AgentClash]], [[AgentSenses]], [[AgentSocial]], [[AgentConfinement]], [[TeamBlackboard]], [[CreatureDNA]], [[RoleWorldProfile]]
