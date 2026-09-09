---
tags: [script, world, ai, agent, facade, expedition]
---

# MoriMochiAgent.cs

**Ruta:** `World/AI/MoriMochiAgent.cs`

**Responsabilidad:** Núcleo delgado que orquesta vida en mundo. Compone 9 colaboradores: AgentContext (estado), AgentBrain (máquina), AgentPhysics (ragdoll), AgentConfinement (pens), AgentSenses (percepción), AgentSocial (social), AgentExpedition (recolección), AgentClash (combate), **AgentAbilities (S107)** (habilidades dinámicas). Fachada pública de todas las responsabilidades. Despachador por estado en Update; Physics en FixedUpdate. S103: expedición, pizarrón. S104: órdenes de arena, cooldowns de ocupación. S107: sistema de habilidades con slots dinámicos por partes del cuerpo. S108: Expone 4 propiedades nuevas de fachada que delegan a clash (telegrafía visual). **S109:** Stats pasivos desde habilidades, SetOrders() re-resuelve, ApplyGaitSpeed() con parámetro `loaded`.

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
- `ArenaOrders Orders { get; }` — desde ctx.Orders (S104)
- `void SetOrders(ArenaOrders orders)` (S109 ACTUALIZADO) — asigna ctx.Orders, derives Occupation, **invoca abilities.RefreshStats() para resolver stats pasivas con ocupación nueva**
- `int Carried { get; }` — desde expedition.Carried
- **`int CarryCapacity { get; }`** (S109 ACTUALIZADO) — ahora desde ctx.Stats.CarryCapacity (antes fijo por occupación)
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
- **S107 NUEVAS:**
  - `int AbilityCount { get; }` — retorna 3 (siempre 3 slots)
  - `AbilitySO Ability(int i) { get; }` — acceso a habilidad del slot i
  - `float AbilityCharge01(int i) → float` — carga normalizada [0,1] del slot i
  - `float AbilityFiredAt(int i) → float` — timestamp último disparo o -1
  - `int ClashTimesKnocked { get; }` — desde clash.timesKnocked (para ArenaHudCard pulsación)
- **S108 NUEVAS:**
  - `ClashMoveSO ClashMove { get; }` — desde clash.Move (movimiento vigente o null)
  - `bool ClashTelegraphing { get; }` — desde clash.Telegraphing (si debe dibujar plantilla)
  - `float ClashTell01 { get; }` — desde clash.Tell01 (progreso 0→1 de anticipación → impacto)
  - `Vector3 ClashImpactPoint { get; }` — desde clash.ImpactPoint (punto de impacto predicho)
- **S109 NUEVAS:**
  - `ExpeditionStats Stats { get; }` — desde ctx.Stats (capacidad, velocidad cargado, guarda, visibilidad)

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
- `void SetAbilities(AbilitySO[] set)` → abilities.Bind(set) (S107 NUEVO)
- `bool ForceClash(ClashMoveSO move, MoriMochiAgent rival) → bool` → clash.ForceMove

**Update() Flow:**
1. DevTrackState(), forceRagdoll check, RecoverIfStuckOffMesh
2. brain.TickAlways
3. senses.Tick()
4. **ApplyGaitSpeed(expedition.Carried > 0)** (S109: parámetro `loaded`)
5. Si en Expedition: abilities.TickMobility() (procesa buffs de movilidad)
6. Por State (switch):
   - Idle/Roaming: si no clash.TryEngage() y no expedition.TryEngage(), social.TryEngage()
   - Expedition: si clash.TryEngage() retorna true, expedition.Cancel(); else expedition.TickExpedition()
   - Otros: delegado a colaborador responsable

**Awake() - Inicialización de Colaboradores:**
- Crea AgentContext, AgentBrain, AgentPhysics, AgentConfinement, AgentSenses, AgentSocial, AgentExpedition, AgentClash, **AgentAbilities** (S107)

**OnEnable/OnDisable:**
- Null-safe: solo ejecutan si confinement != null (lazy init pattern)
- Suscribe a GameEvents.OnNavMeshWillRebake / OnNavMeshRebaked

**S103 Cambios:**
- Propiedades expedición: ScoutReports, SecuredMaterial, CollectedMaterial
- Método SetBlackboard(board)
- Prioridad clash sobre expedición en Update

**S104 Cambios:**
- Propiedades Orders, SetOrders (órdenes de arena)
- Propiedades TimesFled, TrustedGuardian, CarryCapacity, ClashCooldown01, FleeCooldown01, DecoyCooldown01, Retreat01, IsChasing
- Orders afecta Occupation (ArenaOrderRules.ToOccupation)

**S107 Cambios:**
- Nuevo colaborador `abilities` (AgentAbilities)
- Inicializado en Awake() como `abilities = new AgentAbilities(this, ctx)`
- Propiedades públicas: AbilityCount, Ability(i), AbilityCharge01(i), AbilityFiredAt(i), ClashTimesKnocked
- Método SetAbilities(AbilitySO[] set) para asignar array resuelto por AbilityDatabaseSO
- TickMobility() llamado en Update si Expedition (procesa habilidades de movilidad)
- ResetForReuse() en PrepareForPool() también llama abilities.ResetForReuse()

**S108 Cambios:**
- Cuatro propiedades nuevas de fachada para telegrafía visual:
  - `ClashMove` (ClashMoveSO) delega a clash.Move
  - `ClashTelegraphing` (bool) delega a clash.Telegraphing
  - `ClashTell01` (float) delega a clash.Tell01
  - `ClashImpactPoint` (Vector3) delega a clash.ImpactPoint
- Leídas por CreatureCueDrawer.Telegraph() para dibujar plantilla animada

**S109 Cambios:**

- `CarryCapacity` propiedad ahora retorna `ctx.Stats.CarryCapacity` en lugar de valor fijo
- `SetOrders()` actualizado: además de asignar Orders y Occupation, invoca **`abilities.RefreshStats()`** para re-resolver ExpeditionStats con ocupación nueva (ej: cambiar de Gather a Guard modifica capacidad)
- `ApplyGaitSpeed()` firmada como `ApplyGaitSpeed(bool loaded)` en lugar de sin parámetros:
  - Leída como `ApplyGaitSpeed(expedition.Carried > 0)` en Update
  - Delega a `ctx.ApplyGaitSpeed(loaded)` que multiplica por `Stats.LoadedSpeedFactor` si loaded
- Nueva propiedad pública `Stats { get; }` expone ctx.Stats para lectura (ej: MonchiCarryDisplay, MonchiGazeDriver)
- Habilidades pasivas ahora afectan dinámicamente operacionales (capacidad, velocidad, visibilidad) sin necesidad de code bloat

**Internals (composición pura, S55):**
- Sin partial class
- Colaboradores como campos privados
- Orquestación en Update/FixedUpdate
- ctx autoridad única de estado

**Vinculado a:** [[Index/22 - Arena (S103-S104)]], [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]], [[Index/06 - Player & World]]

**Conexiones:** [[AgentContext]], [[AgentBrain]], [[AgentPhysics]], [[AgentExpedition]], [[AgentClash]], [[AgentAbilities]], [[AgentSenses]], [[AgentSocial]], [[AgentConfinement]], [[MoriMonchiController]], [[CreatureDNA]], [[ArenaOrders]], [[TeamBlackboard]], [[ExitZone]], [[Occupation]], [[AbilitySO]], [[CreatureCueDrawer]], [[ExpeditionStats]]
