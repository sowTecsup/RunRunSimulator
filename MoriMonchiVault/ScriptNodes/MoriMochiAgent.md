---
tags: [script, world, ai, agent, facade, expedition]
---

# MoriMochiAgent.cs

**Ruta:** `World/AI/MoriMochiAgent.cs`

**Responsabilidad:** Núcleo delgado que orquesta vida en mundo. Compone 9 colaboradores: AgentContext (estado), AgentBrain (máquina), AgentPhysics (ragdoll), AgentConfinement (pens), AgentSenses (percepción), AgentSocial (social), AgentExpedition (recolección), AgentClash (combate), AgentAbilities (habilidades dinámicas). Fachada pública de todas las responsabilidades. Despachador por estado en Update; Physics en FixedUpdate. S103: expedición, pizarrón. S104: órdenes de arena. S107: sistema de habilidades. S108: Expone fachadas de telegrafía. S109: Stats pasivos. **S116:** Fachadas ClashHitAt, ClashHitPoint para post-impacto visual (anillo).

**Máquina de Estados (responsables):**
- Idle, Roaming → AgentBrain
- Reacting → AgentBrain
- Carried, Thrown, Recovering → AgentPhysics
- SeekingNeed, UsingStation, HandFeed → AgentBrain
- Courting → AgentConfinement
- Socializing → AgentSocial
- Expedition → AgentExpedition
- Clashing → AgentClash

**Propiedades Públicas (Fachada):**
- `CreatureDNA DNA { get; }`
- `CreatureIntent Intent { get; }` — prioridad: Clashing > Socializing > Expedition > Brain
- `ExpeditionTeam Team { get; }` — de Perceivable
- `Occupation Occupation { get; }` — desde ctx.Occupation
- `ArenaOrders Orders { get; }` — desde ctx.Orders
- `void SetOrders(ArenaOrders orders)` — asigna ctx.Orders, derives Occupation, invoca abilities.RefreshStats()
- `int Carried { get; }` — desde expedition.Carried
- `int CarryCapacity { get; }` — desde ctx.Stats.CarryCapacity
- `int CollectedMaterial { get; }` — desde expedition.Collected
- `int SecuredMaterial { get; }` — desde expedition.Secured
- `int TimesFled { get; }` — desde expedition.Fled
- `float MiningProgress { get; }` — desde expedition.MiningProgress
- `Transform ExpeditionTarget { get; }` — desde expedition.TargetTransform
- `MoriMochiAgent TrustedGuardian { get; }` — desde expedition.Guardian
- `int ScoutReports { get; }` — desde expedition.Reports
- `float ClashCooldown01 { get; }` — normalized [0,1]
- `float FleeCooldown01 { get; }` — desde expedition.FleeCooldown01
- `float DecoyCooldown01 { get; }` — desde expedition.DecoyCooldown01
- `float Retreat01 { get; }` — desde expedition.Retreat01
- `bool IsChasing { get; }` — desde expedition.IsChasing
- **S107:**
  - `int AbilityCount { get; }` — retorna 3 (siempre 3 slots)
  - `AbilitySO Ability(int i) { get; }` — acceso a habilidad del slot i
  - `float AbilityCharge01(int i) → float` — carga normalizada [0,1] del slot i
  - `float AbilityFiredAt(int i) → float` — timestamp último disparo o -1
  - `int ClashTimesKnocked { get; }` — desde clash.timesKnocked
- **S108:**
  - `ClashMoveSO ClashMove { get; }` — desde clash.Move
  - `bool ClashTelegraphing { get; }` — desde clash.Telegraphing
  - `float ClashTell01 { get; }` — desde clash.Tell01
  - `Vector3 ClashImpactPoint { get; }` — desde clash.ImpactPoint
- **S109:**
  - `ExpeditionStats Stats { get; }` — desde ctx.Stats
- **S116 NUEVAS:**
  - `float ClashHitAt { get; }` — desde clash.HitAt (timestamp último impacto exitoso o -1)
  - `Vector3 ClashHitPoint { get; }` — desde clash.HitPoint (posición rival del último impacto)

**Métodos Públicos (IThrowable + IInteractable):**
- `void OnGrab(Transform anchor)` → physics
- `void OnRelease()` → physics
- `void OnThrow(Vector3 force)` → physics
- `void Knock(Vector3 force)` → physics
- `void Launch(Vector3 pos, vel)` → physics
- `void Interact()` → brain
- `bool BeginPetting() → bool` → brain
- `void EndPetting()` → brain
- `void Initialize(CreatureDNA creature, RoleWorldProfileSO profileTable, Transform playerTransform)` — setup inicial
- `void Rebind(CreatureDNA creature, RoleWorldProfileSO profileTable)` — reload
- `void PrepareForPool()` — antes de pooling
- `void EmitEmote(EmoteKind kind)` — dispara evento OnEmote
- `void SetBlackboard(TeamBlackboard board)` → ctx.Board
- `void SetHomeExit(ExitZone exit)` → ctx.HomeExit
- `void SetGuardPost(Transform post)` → ctx.GuardPost
- `void SetAbilities(AbilitySO[] set)` → abilities.Bind(set)
- `bool ForceClash(ClashMoveSO move, MoriMochiAgent rival) → bool` → clash.ForceMove

**Update() Flow:**
1. DevTrackState(), forceRagdoll check, RecoverIfStuckOffMesh
2. brain.TickAlways
3. senses.Tick()
4. ApplyGaitSpeed(expedition.Carried > 0)
5. Si en Expedition: abilities.TickMobility()
6. Por State (switch):
   - Idle/Roaming: si no clash.TryEngage() y no expedition.TryEngage(), social.TryEngage()
   - Expedition: si clash.TryEngage() retorna true, expedition.Cancel(); else expedition.TickExpedition()
   - Otros: delegado a colaborador responsable

**Awake() - Inicialización de Colaboradores:**
- Crea AgentContext, AgentBrain, AgentPhysics, AgentConfinement, AgentSenses, AgentSocial, AgentExpedition, AgentClash, AgentAbilities

**OnEnable/OnDisable:**
- Null-safe: solo ejecutan si confinement != null (lazy init pattern)
- Suscribe a GameEvents.OnNavMeshWillRebake / OnNavMeshRebaked

## S116 Cambios

**Nuevas fachadas para post-impacto (S116):**
- `ClashHitAt { get; }` — delega a `clash.HitAt` (timestamp de último impacto exitoso, -1 si nunca)
- `ClashHitPoint { get; }` — delega a `clash.HitPoint` (posición rival donde se conectó el golpe)
- Leídas por ArenaCueOverlay para activar y posicionar ImpactRing post-golpe
- Contexto: plantilla única = hitbox visible durante impacto en punto de conexión real

**Integración con Visualización:**
- Visualización anterior (S114): Tell01 0→1 durante anticipación+bloqueo
- Visualización nueva (S116): ImpactRing centra en HitPoint tras conexión exitosa

**Internals (composición pura, S55):**
- Sin partial class
- Colaboradores como campos privados
- Orquestación en Update/FixedUpdate
- ctx autoridad única de estado

**Vinculado a:** [[Index/20 - MVP Combate]], [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion]], S116

**Conexiones:** [[AgentContext]], [[AgentBrain]], [[AgentPhysics]], [[AgentExpedition]], [[AgentClash]], [[AgentAbilities]], [[AgentSenses]], [[AgentSocial]], [[AgentConfinement]], [[MoriMonchiController]], [[CreatureDNA]], [[ArenaOrders]], [[TeamBlackboard]], [[ExitZone]], [[Occupation]], [[AbilitySO]], [[CreatureCueDrawer]], [[ArenaCueOverlay]], [[ExpeditionStats]]
