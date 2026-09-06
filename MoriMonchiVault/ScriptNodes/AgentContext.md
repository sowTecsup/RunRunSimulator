---
tags: [script, world, agent, internal]
---

# AgentContext.cs

**Ruta:** `World/AI/AgentContext.cs`

**Responsabilidad:** Contenedor de estado puro para un MoriMochiAgent (datos compartidos entre colaboradores sin duplicación). Almacena referencias a componentes (NavMeshAgent, Rigidbody, Collider, Transform), datos de DNA/perfil, estado de juego (ubicación actual, velocidad base), máscaras NavMesh (libre vs confinado), banderas de operación (rebake en curso), y lista de percepciones sociales (Percepts) escrita por AgentSenses y leída por AgentSocial, AgentExpedition. Expone helpers para consultas de seguridad: `IsNavMeshControlled()`, `IsBreeding`, `IsMoving`, `PlanarDistanceToPlayer()`; operaciones del agente: `SetStopped()`, `SetDestinationSafe()` (con muestreo de NavMesh), `SetColliderTrigger()`; **S98 NUEVO:** `BaseSpeed` y `ApplyGaitSpeed()` = único dueño de `NavMeshAgent.speed`. **S97:** AgentState enum con `Expedition`. **S100 NUEVO:** AgentState con `Clashing`. **S101 NUEVO:** campos Occupation, HomeExit, GuardPost para ocupaciones de expedición. No tiene lógica de estado.

## Enum AgentState

```csharp
internal enum AgentState { 
    Idle,           // esperando
    Roaming,        // navegando libremente
    Reacting,       // reaccionando al jugador
    Carried,        // en mano del jugador
    Thrown,         // en vuelo (ragdoll)
    Recovering,     // recuperándose post-vuelo
    SeekingNeed,    // navegando a estación
    UsingStation,   // usando estación
    Courting,       // cortejando
    Socializing,    // interacción social (S65)
    HandFeed,       // comiendo de la mano (S69)
    Expedition,     // persiguiendo objetivo recolectable (S97 NUEVO)
    Clashing        // combatiendo físico (S100 NUEVO)
}
```

## Campos Internos

- `Owner` (MoriMochiAgent) — agente propietario
- `Body, Agent, Rb, Col` (Transform, NavMeshAgent, Rigidbody, Collider) — componentes del GO
- **S98 NUEVO:**
  - `BaseSpeed` (float) — velocidad base del NavMeshAgent (cacheada en inicio, modificable). Única fuente de verdad; todas las variaciones de velocidad pasan por `ApplyGaitSpeed()`.
- `State` (AgentState) — estado actual; puede ser Expedition (S97) o Clashing (S100)
- `Dna, Profile` (CreatureDNA, RoleWorldProfile) — datos genéticos y perfil de rol; Profile.RoamSpeedFactor usado por S98
- `Player, HoldAnchor` (Transform) — transforms de referencias externas
- `CurrentContainer` (MoriMochiContainer) — el corral/contenedor que lo confina (null si libre)
- **S101 NUEVO:**
  - `Occupation` (Occupation, default Gather) — estrategia de expedición asignada por ArenaSandbox (Break/Guard/Gather/Decoy/Explore)
  - `HomeExit` (ExitZone) — salida de base a la que retorna tras recolección (inyectado por ArenaSandbox)
  - `GuardPost` (Transform) — puesto de vigilancia si es Guard (inyectado por ArenaSandbox)
- `FreeAreaMask, ConfinedAreaMask` (int) — máscaras NavMesh por área
- `RebakeInProgress` (bool) — bandera de rebake en curso
- `Percepts` (List<Percept>) — S64 lista ordenada por distancia, capped a MaxPercepts. Escrita por AgentSenses, leída por AgentSocial, AgentExpedition, S98 posible uso futuro

## Métodos Públicos

- `IsNavMeshControlled() → bool` — verdadero si el estado NO es Carried/Thrown/Recovering. **S97:** ahora incluye Expedition. **S100:** ahora incluye Clashing.
- `IsBreeding → bool` — verdadero si DNA.BusyState == Breeding
- `IsMoving → bool` — verdadero si agente está en movimiento físico (enabled, on mesh, not stopped, velocity > 0.01)
- `SetStopped(bool stopped)` — pausa/reanuda el NavMeshAgent
- `SetColliderTrigger(bool isTrigger)` — toggle entre trigger (roaming) y solid (física)
- `SetDestinationSafe(Vector3 desired)` — muestrea punto en NavMesh antes de asignar destino; fallback seguro si no sampleable
- `PlanarDistanceToPlayer() → float` — distancia horizontal al jugador (ignorando Y); MaxValue si sin Player ref
- `RandomPointInBounds(Bounds b) → static Vector3` — punto aleatorio dentro de límites (Y fijo)

## Métodos S98 NUEVOS

- **`ApplyGaitSpeed()`** — **S98 NUEVO, S100 ACTUALIZADO.** Único dueño de `NavMeshAgent.speed`. Lógica:
  - Si Agent == null, retorna temprano
  - Si State == Courting, retorna sin tocar (courting pace se maneja aparte)
  - **S100:** Si State == Clashing, retorna sin tocar (clash mantiene su propia lógica de NavMeshAgent override)
  - Sino: calcula `factor = (State == Roaming && Profile != null) ? Profile.RoamSpeedFactor : 1f`
  - Asigna `Agent.speed = BaseSpeed × factor` (solo si cambió, evita dirty)
  - Llamado cada frame por `MoriMochiAgent.Update()` para mantener speed sincronizada con estado y profile

## Notas sobre Percepts

- Poblada por AgentSenses.Tick() cada ScanInterval (2–4s throttled)
- Ordenada por sqrDistance (más cerca primero)
- Capeada a SocialTuningSO.MaxPercepts (default 8)
- Incluye Player, Monchi (con afinidad), Customer, Prop, Material (S97)
- Nunca null: limpiada si el agente no está en control NavMesh
- Pizarrón compartido con colaboradores (AgentSocial, AgentExpedition) para decisiones sin re-consulta

## Cambios S101: Ocupaciones

**Línea 25-27: Campos nuevos**

```csharp
internal Occupation Occupation = Occupation.Gather;
internal ExitZone HomeExit;
internal Transform GuardPost;
```

- `Occupation` — estrategia de expedición (None/Gather/Guard/Break/Decoy/Explore); inyectado por ArenaSandbox desde ArenaRosterSO
- `HomeExit` — salida de base (si Gather o Guard, retorna acá); inyectado por ArenaSandbox.SpawnCreature()
- `GuardPost` — puesto de vigilancia (si Guard, se planta acá); inyectado por ArenaSandbox.SpawnCreature()

**Cómo se inyectan:**
```csharp
// En ArenaSandbox.SpawnCreature()
var entry = roster.Entries[i];
controller.Agent.SetOccupation(entry.Occupation);
controller.Agent.SetHomeExit(exit);
if (entry.Occupation == Occupation.Guard)
    controller.Agent.SetGuardPost(guardPos);
```

## Cambios S100: Clashing

**Línea 7:** AgentState enum actualizado:
```csharp
internal enum AgentState { ..., Clashing }  // valor 13
```

**Línea 43:** IsNavMeshControlled() ahora incluye Clashing:
```csharp
internal bool IsNavMeshControlled() =>
    State == AgentState.Idle        || State == AgentState.Roaming      || State == AgentState.Reacting ||
    State == AgentState.SeekingNeed || State == AgentState.UsingStation || State == AgentState.Courting ||
    State == AgentState.Socializing || State == AgentState.Expedition  || State == AgentState.Clashing;
```

**Línea 59:** ApplyGaitSpeed() ahora excluye Clashing junto con Courting:
```csharp
internal void ApplyGaitSpeed()
{
    if (Agent == null) return;
    if (State == AgentState.Courting || State == AgentState.Clashing) return;  // S100: Clashing maneja su propia velocidad
    // ... resto de lógica
}
```

## Cambios S98

**BaseSpeed centralizado:**
- Campo `BaseSpeed` (línea 17) almacena la velocidad base
- Único dueño de `NavMeshAgent.speed` vía `ApplyGaitSpeed()`
- Roaming → base × `Profile.RoamSpeedFactor` (más lento para explorar)
- Courting → sin cambio (mantiene su propia lógica)
- Clashing → sin cambio (mantiene su propia lógica de override)
- Estados restantes → base sin factor (velocidad normal)
- Llamada cada frame desde `MoriMochiAgent.Update()` (línea 56-64)

**Cambio de responsabilidad:**
- **ANTES S98:** `AgentBrain.EnterRoaming()` seteaba `Agent.speed` directamente
- **DESDE S98:** `AgentBrain.EnterRoaming()` ya NO toca `Agent.speed`; la velocidad la aplica `AgentContext.ApplyGaitSpeed()` cada frame

## Cambios S97

**Enum AgentState ampliado:**
- `+Expedition` state — agente persiguiendo material recolectable

**IsNavMeshControlled() actualizado:**
- Ahora devuelve true para Idle, Roaming, Reacting, SeekingNeed, UsingStation, Courting, Socializing, **Expedition**

## Invariantes S101 + S100 + S98 + S97

- **Único dueño de NavMeshAgent.speed:** `ApplyGaitSpeed()` es la única vía para cambiar speed (centraliza lógica, evita conflictos). **S100:** Clashing maneja override interno (no interfiere con ApplyGaitSpeed).
- **Pizarrón compartido:** Percepts evita que cada colaborador re-consulte PerceivableRegistry; ahorro de iteraciones.
- **AgentState enum centralizado:** todas las máquinas de estado del agente usan este enum; fácil agregar estados nuevos.
- **IsNavMeshControlled determinista:** agrupa estados lógicos "en control del NavMesh" vs "en física pura". **S100:** Clashing es NavMesh-controlled (agente aún tiene NavMeshAgent enabled, aunque con override de velocidad/aceleración).
- **SetDestinationSafe idempotent:** si la posición no es sampleable, no asigna (safe fallback vs crash).
- **Courting/Clashing carve-out:** ambos no cambian speed porque siguen su propia lógica (cortejo y combate respectivamente).
- **S101:** Occupation inmutable durante sesión (asignado una vez al spawn, no cambia); HomeExit/GuardPost dinámicos (pueden ser nulos si ocupación no lo requiere)

## Vinculado a

- [[Index/06 - Player & World]]
- [[Index/02 - Genetics & Breeding]]
- [[Index/23 - Arena Sandbox y Expedicion]] (S97, S98, S100, S101: velocidades, clash y ocupaciones en arena)

## Conexiones

- [[MoriMochiAgent]] — propietario, llama `ApplyGaitSpeed()` en Update (S98)
- [[AgentBrain]] — S98: YA NO toca speed (delegó a ApplyGaitSpeed)
- [[AgentPhysics]]
- [[AgentConfinement]]
- [[AgentSenses]] — escribe Percepts
- [[AgentSocial]] — lee Percepts
- [[AgentExpedition]] — S97 lee Percepts, usa SetDestinationSafe; **S101:** lee Occupation, HomeExit, GuardPost
- **S100:** [[AgentClash]] — maneja override de velocidad internamente durante Clashing
- [[RoleWorldProfileSO]] — Profile.RoamSpeedFactor leído por ApplyGaitSpeed (S98)
- [[HotbarController]]
- [[ExpeditionRulesSO]] — S97
- **S100:** [[ClashTuningSO]] — consultado por AgentClash
- **S101:** [[ArenaSandbox]] — inyecta Occupation, HomeExit, GuardPost vía SetOccupation/SetHomeExit/SetGuardPost
- **S101:** [[Occupation]] enum
