---
tags: [script, world, agent, internal]
---

# AgentContext.cs

**Ruta:** `World/AI/AgentContext.cs`

**Responsabilidad:** Contenedor de estado puro para un MoriMochiAgent (datos compartidos entre colaboradores sin duplicación). Almacena referencias a componentes (NavMeshAgent, Rigidbody, Collider, Transform), datos de DNA/perfil, estado de juego (ubicación actual, velocidad base), máscaras NavMesh (libre vs confinado), banderas de operación (rebake en curso), y lista de percepciones sociales (Percepts) escrita por AgentSenses y leída por AgentSocial, AgentExpedition. Expone helpers para consultas de seguridad: `IsNavMeshControlled()`, `IsBreeding`, `IsMoving`, `PlanarDistanceToPlayer()`; operaciones del agente: `SetStopped()`, `SetDestinationSafe()` (con muestreo de NavMesh), `SetColliderTrigger()`; **S98 NUEVO:** `BaseSpeed` y `ApplyGaitSpeed()` = único dueño de `NavMeshAgent.speed`. **S97:** AgentState enum con `Expedition`. No tiene lógica de estado.

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
    Expedition      // persiguiendo objetivo recolectable (S97 NUEVO)
}
```

## Campos Internos

- `Owner` (MoriMochiAgent) — agente propietario
- `Body, Agent, Rb, Col` (Transform, NavMeshAgent, Rigidbody, Collider) — componentes del GO
- **S98 NUEVO:**
  - `BaseSpeed` (float) — velocidad base del NavMeshAgent (cacheada en inicio, modificable). Única fuente de verdad; todas las variaciones de velocidad pasan por `ApplyGaitSpeed()`.
- `State` (AgentState) — estado actual; puede ser Expedition (S97)
- `Dna, Profile` (CreatureDNA, RoleWorldProfile) — datos genéticos y perfil de rol; Profile.RoamSpeedFactor usado por S98
- `Player, HoldAnchor` (Transform) — transforms de referencias externas
- `CurrentContainer` (MoriMochiContainer) — el corral/contenedor que lo confina (null si libre)
- `FreeAreaMask, ConfinedAreaMask` (int) — máscaras NavMesh por área
- `RebakeInProgress` (bool) — bandera de rebake en curso
- `Percepts` (List<Percept>) — S64 lista ordenada por distancia, capped a MaxPercepts. Escrita por AgentSenses, leída por AgentSocial, AgentExpedition, S98 posible uso futuro

## Métodos Públicos

- `IsNavMeshControlled() → bool` — verdadero si el estado NO es Carried/Thrown/Recovering. **S97:** ahora incluye Expedition.
- `IsBreeding → bool` — verdadero si DNA.BusyState == Breeding
- `IsMoving → bool` — verdadero si agente está en movimiento físico (enabled, on mesh, not stopped, velocity > 0.01)
- `SetStopped(bool stopped)` — pausa/reanuda el NavMeshAgent
- `SetColliderTrigger(bool isTrigger)` — toggle entre trigger (roaming) y solid (física)
- `SetDestinationSafe(Vector3 desired)` — muestrea punto en NavMesh antes de asignar destino; fallback seguro si no sampleable
- `PlanarDistanceToPlayer() → float` — distancia horizontal al jugador (ignorando Y); MaxValue si sin Player ref
- `RandomPointInBounds(Bounds b) → static Vector3` — punto aleatorio dentro de límites (Y fijo)

## Métodos S98 NUEVOS

- **`ApplyGaitSpeed()`** — **S98 NUEVO.** Único dueño de `NavMeshAgent.speed`. Lógica:
  - Si Agent == null, retorna temprano
  - Si State == Courting, retorna sin tocar (courting pace se maneja aparte)
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

## Cambios S98

**BaseSpeed centralizado:**
- Campo `BaseSpeed` (línea 17) almacena la velocidad base
- Único dueño de `NavMeshAgent.speed` vía `ApplyGaitSpeed()`
- Roaming → base × `Profile.RoamSpeedFactor` (más lento para explorar)
- Courting → sin cambio (mantiene su propia lógica)
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

## Invariantes S98 + S97

- **Único dueño de NavMeshAgent.speed:** `ApplyGaitSpeed()` es la única vía para cambiar speed (centraliza lógica, evita conflictos).
- **Pizarrón compartido:** Percepts evita que cada colaborador re-consulte PerceivableRegistry; ahorro de iteraciones.
- **AgentState enum centralizado:** todas las máquinas de estado del agente usan este enum; fácil agregar estados nuevos.
- **IsNavMeshControlled determinista:** agrupa estados lógicos "en control del NavMesh" vs "en física pura". S97 agrega Expedition (usa SetDestination).
- **SetDestinationSafe idempotent:** si la posición no es sampleable, no asigna (safe fallback vs crash).
- **Courting carve-out:** Courting no cambia speed porque sigue su propia lógica de cortejo (no es Roaming).

## Vinculado a

- [[Index/06 - Player & World]]
- [[Index/02 - Genetics & Breeding]]
- [[Index/23 - Arena Sandbox y Expedicion]] (S97, S98: velocidades en arena)

## Conexiones

- [[MoriMochiAgent]] — propietario, llama `ApplyGaitSpeed()` en Update (S98)
- [[AgentBrain]] — S98: YA NO toca speed (delegó a ApplyGaitSpeed)
- [[AgentPhysics]]
- [[AgentConfinement]]
- [[AgentSenses]] — escribe Percepts
- [[AgentSocial]] — lee Percepts
- [[AgentExpedition]] — S97 lee Percepts, usa SetDestinationSafe
- [[RoleWorldProfileSO]] — Profile.RoamSpeedFactor leído por ApplyGaitSpeed (S98)
- [[HotbarController]]
- [[ExpeditionRulesSO]] — S97
