---
tags: [script, world, ai, agent, facade]
---

# MoriMochiAgent.cs

**Ruta:** `World/AI/MoriMochiAgent.cs`

**Responsabilidad:** Núcleo delgado que orquesta vida en mundo. Compone 8 colaboradores: AgentContext (estado), AgentBrain (máquina), AgentPhysics (ragdoll), AgentConfinement (pens), AgentSenses (percepción), AgentSocial (social), AgentExpedition (recolección), AgentClash (combate). **S103:** Expone fachadas de expedición (Velocity, knockSpin, ScoutReports, SecuredMaterial, ClashHitsLanded, ClashTimesKnocked, SetBlackboard). Cancela expedición si clash ocurre (prioridad combate). Update() despachador por estado; FixedUpdate() physics.

**Máquina de Estados (responsables):**
- Idle, Roaming → AgentBrain
- Reacting → AgentBrain
- Carried, Thrown, Recovering → AgentPhysics
- SeekingNeed, UsingStation, HandFeed → AgentBrain
- Courting → AgentConfinement
- Socializing → AgentSocial
- Expedition → AgentExpedition
- Clashing → AgentClash (S100)

**Propiedades Públicas (Fachada):**
- `DNA → CreatureDNA`
- `Intent → CreatureIntent` — prioridad: Clashing > Socializing > Expedition > Brain
- `Team → ExpeditionTeam` — inyectado por ArenaSandbox
- `Occupation → Occupation` — inyectado por ArenaSandbox (S101)
- `Carried → int` — carga actual
- `CollectedMaterial → int` — recolectado acumulativo
- `MiningProgress → float` — 0-1
- `ExpeditionTarget → Transform`

**S103 Propiedades Nuevas:**
- `float Velocity { get; }` — magnitud de NavMeshAgent.velocity + fallback Rigidbody (M)
- `float knockSpin { get; set; }` — scalar de torque en Knock (tuning MonchiSquashDriver)
- `int SecuredMaterial { get; }` — inyectado por ExitZone al depositar (contador)
- `int ScoutReports { get; }` — consulta expedition.scout.Reports
- `int ClashHitsLanded { get; }` — consulta clash.hitsLanded
- `int ClashTimesKnocked { get; }` — consulta clash.timesKnocked

**S103 Métodos Nuevos:**
- `SetBlackboard(TeamBlackboard board)` — inyecta pizarrón en ctx.Board (ArenaSandbox lo llama)

**Métodos Públicos (IThrowable + IInteractable):**
- `OnGrab(Transform anchor)` → physics.OnGrab()
- `OnRelease()` → physics.OnRelease()
- `OnThrow(Vector3 force)` → physics.OnThrow()
- `Knock(Vector3 force)` → physics.Knock() (+ knockSpin torque vía AgentPhysics S103)
- `Launch(Vector3 pos, vel)` → physics.Launch()
- `Interact(Transform player)` → social.InitiatePetting()
- `Initialize(DNA, profile, player)` — setup inicial
- `Rebind(DNA, profile)` — reload rápido
- `PrepareForPool()` — antes de pooling
- `EmitEmote(EmoteKind)` — dispara emote

**Update() Flow (S103 Actualizado):**
1. TickAlways
2. Senses.Tick()
3. ApplyGaitSpeed()
4. Por State (switch):
   - Idle/Roaming: si no clash.TryEngage() y no expedition.TryEngage(), social.TryEngage()
   - **S103:** Si `clash.TryEngage()` retorna true, `expedition.Cancel()` — prioridad combate
   - Expedition: si `clash.TryEngage()` retorna true, `expedition.Cancel()`, sino `expedition.Tick()`

**FixedUpdate() Flow:**
- physics.FixedTick() → actualiza velocity si Carried/Thrown

**S103 Cambios Principales:**
- Propiedades `Velocity`, `knockSpin`, `SecuredMaterial`, `ScoutReports`, `ClashHitsLanded`, `ClashTimesKnocked` (exposiciones a fachada)
- Método `SetBlackboard(board)` para inyectar pizarrón (S103 NUEVO)
- En Update, si clash.TryEngage() en estado Idle/Roaming/Expedition: `expedition.Cancel()` (prioridad combate, S103)
- knockSpin se aplica en AgentPhysics.Knock() (S103)

**Internals (sin cambios):**
- OnEnable/OnDisable suscripciones a GameEvents.NavMesh
- Awake instancia colaboradores
- Initialize/Rebind delegados
- RestoreNavMeshControl resetea todos

**Composición Pura (S55):**
- Sin partial class
- Colaboradores como campos privados
- Orquestación en Update/FixedUpdate

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]], [[Index/06 - Player & World]]

**Conexiones:** [[AgentContext]], [[AgentBrain]], [[AgentPhysics]], [[AgentExpedition]], [[AgentClash]], [[AgentSenses]], [[AgentSocial]], [[AgentConfinement]], [[MoriMonchiController]], [[CreatureDNA]], [[TeamBlackboard]], [[ExitZone]]
