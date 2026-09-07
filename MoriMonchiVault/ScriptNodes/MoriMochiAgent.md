---
tags: [script, world, ai, agent, facade, expedition]
---

# MoriMochiAgent.cs

**Ruta:** `World/AI/MoriMochiAgent.cs`

**Responsabilidad:** Núcleo delgado que orquesta vida en mundo. Compone 8 colaboradores: AgentContext (estado), AgentBrain (máquina), AgentPhysics (ragdoll), AgentConfinement (pens), AgentSenses (percepción), AgentSocial (social), AgentExpedition (recolección), AgentClash (combate). Fachada pública de todas las responsabilidades. Despachador por estado en Update; Physics en FixedUpdate. S103: expedición, pizarrón. S104: órdenes de arena, cooldowns de ocupación.

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
- `Occupation Occupation { get; }` — desde ctx.Occupation (S104)
- `ArenaOrders Orders { get; }` — desde ctx.Orders (S104 NUEVO)
- `void SetOrders(ArenaOrders orders)` — asigna ctx.Orders, derives Occupation (S104 NUEVO)
- `int Carried { get; }` — desde expedition.Carried
- `int CarryCapacity { get; }` — desde expedition.CarryCapacity (S104 NUEVO)
- `int CollectedMaterial { get; }` — desde expedition.Collected
- `int SecuredMaterial { get; }` — desde expedition.Secured
- `int TimesFled { get; }` — desde expedition.Fled (S104 NUEVO)
- `float MiningProgress { get; }` — desde expedition.MiningProgress
- `Transform ExpeditionTarget { get; }` — desde expedition.TargetTransform
- `MoriMochiAgent TrustedGuardian { get; }` — desde expedition.Guardian (S104 NUEVO alias)
- `int ScoutReports { get; }` — desde expedition.Reports
- `float ClashCooldown01 { get; }` — normalized [0,1] (S104 NUEVO)
- `float FleeCooldown01 { get; }` — desde expedition.FleeCooldown01 (S104 NUEVO)
- `float DecoyCooldown01 { get; }` — desde expedition.DecoyCooldown01 (S104 NUEVO)
- `float Retreat01 { get; }` — desde expedition.Retreat01 (S104 NUEVO)
- `bool IsChasing { get; }` — desde expedition.IsChasing (S104 NUEVO)

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
- `void SetBlackboard(TeamBlackboard board)` → ctx.Board (S103)
- `void SetHomeExit(ExitZone exit)` → ctx.HomeExit
- `void SetGuardPost(Transform post)` → ctx.GuardPost
- `bool ForceClash(ClashMoveSO move, MoriMochiAgent rival) → bool` → clash.ForceMove

**Update() Flow:**
1. DevTrackState(), forceRagdoll check, RecoverIfStuckOffMesh
2. brain.TickAlways
3. senses.Tick()
4. ApplyGaitSpeed()
5. Por State (switch):
   - Idle/Roaming: si no clash.TryEngage() y no expedition.TryEngage(), social.TryEngage()
   - Expedition: si clash.TryEngage() retorna true, expedition.Cancel(); else expedition.TickExpedition()
   - Otros: delegado a colaborador responsable

**OnEnable/OnDisable (S104 "blindados"):**
- Solo ejecutan si confinement != null (lazy init pattern: cierra event suscripción si constructor no completó)

**S103 Cambios:**
- Propiedades expedición: ScoutReports, SecuredMaterial, CollectedMaterial
- Método SetBlackboard(board)
- Prioridad clash sobre expedición en Update

**S104 Cambios:**
- Propiedades Orders, SetOrders (órdenes de arena)
- Propiedades TimesFled, TrustedGuardian, CarryCapacity, ClashCooldown01, FleeCooldown01, DecoyCooldown01, Retreat01, IsChasing
- Orders afecta Occupation (ArenaOrderRules.ToOccupation)
- OnEnable/OnDisable blindados (null-safe confinement)
- Intent cuestión resuelta por composición (no hay bloqueo)

**Internals (composición pura, S55):**
- Sin partial class
- Colaboradores como campos privados
- Orquestación en Update/FixedUpdate
- ctx autoridad única de estado

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]], [[Index/06 - Player & World]]

**Conexiones:** [[AgentContext]], [[AgentBrain]], [[AgentPhysics]], [[AgentExpedition]], [[AgentClash]], [[AgentSenses]], [[AgentSocial]], [[AgentConfinement]], [[MoriMonchiController]], [[CreatureDNA]], [[ArenaOrders]], [[TeamBlackboard]], [[ExitZone]], [[Occupation]]
