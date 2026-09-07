---
tags: [script, world, ai, context, internal]
---

# AgentContext.cs

**Ruta:** `World/AI/AgentContext.cs`

**Responsabilidad:** Contenedor de estado puro compartido entre colaboradores (AgentBrain, AgentPhysics, AgentExpedition, AgentClash, AgentSenses, AgentSocial, AgentConfinement). Almacena refs de componentes, DNA/perfil, estado de juego, máscaras NavMesh, percepciones, pizarrón de equipo (S103), **órdenes de arena (S104 NUEVO)**. Sin lógica de estado; solo datos y helpers (SetDestinationSafe, IsMoving, PlanarDistance, etc.).

**Enum AgentState:**
Idle, Roaming, Reacting, Carried, Thrown, Recovering, SeekingNeed, UsingStation, Courting, Socializing, HandFeed, Expedition, Clashing

**Campos Internos:**
- Refs: Owner, Body, Agent, Rb, Col (componentes)
- DNA, Profile (genética + rol)
- Player, HoldAnchor (refs externas)
- CurrentContainer (corral si confinado)
- State (AgentState actual)
- BaseSpeed (velocidad base NavMesh)
- `Occupation Occupation` — ocupación arena (S104 NUEVO). Derivada de Orders o Gather por defecto. Consultada por AgentExpedition para switch de colaborador
- `ArenaOrders Orders` — órdenes vigentes (S104 NUEVO). Struct con Loot/Contact/Posture. Clampeado por DNA si personalidad bloquea. Consultado por clash (Break solo golpea sin custodio), expedición (loot bias, huida), y UI
- TeamBlackboard Board (S103) — pizarrón de equipo
- Occupación/expedición: HomeExit, GuardPost
- Percepción: Percepts (List<Percept>)
- NavMesh: FreeAreaMask, ConfinedAreaMask, RebakeInProgress

**Métodos Públicos:**
- `bool IsNavMeshControlled() → bool` — si state es controlado por navmesh (Idle, Roaming, SeekingNeed, UsingStation, Expedition, Clashing)
- `bool IsBreeding { get; }` — si DNA.BusyState == Breeding
- `bool IsMoving { get; }` — si Agent activo y en movimiento
- `void SetStopped(bool stopped)` — Agent.isStopped
- `void SetDestinationSafe(Vector3 desired)` — SetDestination con sample check
- `void ApplyGaitSpeed()` — aplica factor velocidad según State/Profile (solo único dueño de Agent.speed)
- `void SetColliderTrigger(bool isTrigger)`
- `float PlanarDistanceToPlayer() → float` — XZ distance al player
- `static Vector3 RandomPointInBounds(Bounds b) → Vector3`

**S104 Cambios:**
- Campos `Occupation` y `ArenaOrders Orders` agregados
- Orders inyectado por ArenaCastPlanner vía `arenaCast.Orders`; clampeado por ArenaOrderRules.Clamp(DNA, rules, orders)
- Occupation derivado de Orders vía ArenaOrderRules.ToOccupation()
- Consulta en AgentClash.TryEngage(): Contact fuerza fight si Boldness alto O Posture bloquea Gather
- Consulta en AgentExpedition: ocupación elige colaborador, Orders pasa a loot bias en Gather

**Invariantes:**
- Contenedor puro: sin lógica de transición de estado
- Ref compartida: todos los colaboradores leen/escriben ctx, no hay duplicación
- State autoridad única: solo colaboradores pueden cambiar State
- Board nullable: null si no en expedición
- Orders inmutable dentro de expedición (derivado de Orders al inicio; si cambia → reset y re-engage)

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

**Conexiones:** [[MoriMochiAgent]], [[AgentBrain]], [[AgentPhysics]], [[AgentExpedition]], [[AgentClash]], [[AgentSenses]], [[AgentSocial]], [[AgentConfinement]], [[TeamBlackboard]], [[CreatureDNA]], [[ArenaOrders]], [[ArenaOrderRules]]
