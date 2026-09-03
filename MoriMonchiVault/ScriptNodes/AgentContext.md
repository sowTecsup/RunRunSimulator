---
tags: [script, world, agent, internal]
---

# AgentContext.cs

**Ruta:** `World/AI/AgentContext.cs`

**Responsabilidad:** Contenedor de estado puro para un MoriMochiAgent (datos compartidos entre colaboradores sin duplicación). Almacena referencias a componentes (NavMeshAgent, Rigidbody, Collider, Transform), datos de DNA/perfil, estado de juego (ubicación actual, velocidad base), máscaras NavMesh (libre vs confinado), banderas de operación (rebake en curso), y NUEVO en S64: lista de percepciones sociales (Percepts) escrita por AgentSenses y leída por AgentSocial. Expone helpers para consultas de seguridad: `IsNavMeshControlled()` (estado controlado por NavMesh), `IsBreeding`, `IsMoving`, `PlanarDistanceToPlayer()`, y operaciones del agente: `SetStopped()`, `SetDestinationSafe()` (con muestreo de NavMesh), `SetColliderTrigger()`. **S97:** AgentState enum ampliado con `Expedition`; incluido en `IsNavMeshControlled()`. No tiene lógica de estado.

## Enum AgentState

```csharp
public enum AgentState { 
    Idle,         // esperando
    Roaming,      // navegando
    Reacting,     // reaccionando al jugador
    Carried,      // en mano del jugador
    Thrown,       // en vuelo (ragdoll)
    Recovering,   // recuperándose post-vuelo
    SeekingNeed,  // navegando a estación
    UsingStation, // usando estación
    Courting,     // cortejando
    Socializing,  // interacción social (S65)
    HandFeed,     // comiendo de la mano (S69)
    Expedition    // persiguiendo objetivo recolectable (S97 NUEVO)
}
```

**Cambios S97:**
- **NUEVO:** `Expedition` — agente está persiguiendo material recolectable; manejado por AgentExpedition.TickExpedition(). Incluido en `IsNavMeshControlled()`.

## Campos internos

- `Owner` (MoriMochiAgent)
- `Body, Agent, Rb, Col` — componentes del GO
- `BaseSpeed` — velocidad cached del NavMeshAgent
- `State` — estado actual (AgentState enum, S97: puede ser Expedition)
- `Dna, Profile` — datos genéticos y perfil de rol
- `Player, HoldAnchor` — transforms de referencias externas
- `CurrentContainer` — el corral/contenedor que lo confina (null si libre)
- `FreeAreaMask, ConfinedAreaMask` — máscaras NavMesh por área
- `RebakeInProgress` — bandera de rebake en curso
- `Percepts` — **S64 NUEVO** `List<Percept>` ordenada por distancia, capped a MaxPercepts. Escrita por AgentSenses.Tick(), leída por AgentSocial y **S97:** AgentExpedition

## Métodos

- `IsNavMeshControlled() → bool` — verdadero si el estado NO es Carried/Thrown/Recovering. **S97:** ahora incluye Expedition (línea 43).
- `IsBreeding → bool` — verdadero si DNA.BusyState == Breeding
- `IsMoving → bool` — verdadero si el agente está en movimiento físico
- `SetStopped(bool)` — pausa/reanuda el agente
- `SetColliderTrigger(bool)` — toggle entre trigger (roaming) y solid (física)
- `SetDestinationSafe(Vector3)` — muestrea punto en NavMesh antes de asignar destino
- `PlanarDistanceToPlayer() → float` — distancia horizontal al jugador (ignorando Y)
- `RandomPointInBounds(Bounds) → static Vector3` — punto aleatorio dentro de límites (Y fijo)

## Notas sobre Percepts

- Poblada por AgentSenses.Tick() cada ScanInterval (2–4s throttled)
- Ordenada por sqrDistance (más cerca primero)
- Capeada a SocialTuningSO.MaxPercepts (default 8)
- Incluye Player, Monchi (con afinidad), Customer, Prop, **S97:** Material
- Nunca null: limpiada si el agente no está en control NavMesh
- Pizarrón compartido con AgentSocial y **S97:** AgentExpedition para decisiones sin re-consulta

## Cambios S97

**Enum AgentState ampliado:**
- `+Expedition` state (línea 7)

**IsNavMeshControlled() actualizado:**
- Línea 43: `State == AgentState.Expedition` agregado a la condición OR
- Ahora devuelve true para Idle, Roaming, Reacting, SeekingNeed, UsingStation, Courting, Socializing, **Expedition**

**Uso por AgentExpedition:**
- `TryEngage()` asigna `ctx.State = AgentState.Expedition`
- `TickExpedition()` consulta `ctx.Percepts`, usa `SetDestinationSafe()` para navegar
- `Abort()` llama `owner.RequestRoam()` → vuelve a Roaming

## Invariantes S93 + S97

- **Pizarrón compartido:** Percepts evita que cada colaborador re-consulte PerceivableRegistry; ahorro de iteraciones.
- **AgentState enum centralizado:** todas las máquinas de estado del agente usan este enum; fácil agregar estados nuevos (solo enum + case en Update).
- **IsNavMeshControlled determinista:** agrupa estados lógicos "en control del NavMesh" vs "en física pura". S97 agrega Expedition al grupo NavMesh porque usa SetDestination.
- **SetDestinationSafe idempotent:** si la posición no es sampleable, no asigna (safe fallback vs crash).

## Vinculado a

[[Index/06 - Player & World]], [[Index/02 - Genetics & Breeding]], [[Index/23 - Arena Sandbox y Expedicion]] (S97)

## Conexiones

[[MoriMochiAgent]], [[AgentBrain]], [[AgentPhysics]], [[AgentConfinement]], [[AgentSenses]], [[AgentSocial]], **S97:** [[AgentExpedition]], [[RoleWorldProfileSO]], [[HotbarController]], **S97:** [[ExpeditionRulesSO]]
