---
tags: [script, world, agent, internal]
---

# AgentContext.cs

**Ruta:** `World/AI/AgentContext.cs`

**Responsabilidad:** Contenedor de estado puro para un MoriMochiAgent (datos compartidos entre colaboradores sin duplicación). Almacena referencias a componentes (NavMeshAgent, Rigidbody, Collider, Transform), datos de DNA/perfil, estado de juego (ubicación actual, velocidad base), máscaras NavMesh (libre vs confinado), y banderas de operación (rebake en curso). Expone helpers para consultas de seguridad: `IsNavMeshControlled()` (estado controlado por NavMesh), `IsBreeding`, `IsMoving`, `PlanarDistanceToPlayer()`, y operaciones del agente: `SetStopped()`, `SetDestinationSafe()` (con muestreo de NavMesh), `SetColliderTrigger()`. No tiene lógica de estado.

**Campos internos:**
- `Owner` (MoriMochiAgent)
- `Body, Agent, Rb, Col` — componentes del GO
- `BaseSpeed` — velocidad cached del NavMeshAgent
- `State` — estado actual (AgentState enum)
- `Dna, Profile` — datos genéticos y perfil de rol
- `Player, HoldAnchor` — transforms de referencias externas
- `CurrentContainer` — el corral/contenedor que lo confina (null si libre)
- `FreeAreaMask, ConfinedAreaMask` — máscaras NavMesh por área
- `RebakeInProgress` — bandera de rebake en curso

**Métodos:**
- `IsNavMeshControlled() → bool` — verdadero si el estado no es Carried/Thrown/Recovering
- `IsBreeding → bool` — verdadero si DNA.BusyState == Breeding
- `IsMoving → bool` — verdadero si el agente está en movimiento físico
- `SetStopped(bool)` — pausa/reanuda el agente
- `SetColliderTrigger(bool)` — toggle entre trigger (roaming) y solid (física)
- `SetDestinationSafe(Vector3)` — muestrea punto en NavMesh antes de asignar destino
- `PlanarDistanceToPlayer() → float` — distancia horizontal al jugador (ignorando Y)
- `RandomPointInBounds(Bounds) → static Vector3` — punto aleatorio dentro de límites (Y fijo)

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[MoriMochiAgent]], [[AgentBrain]], [[AgentPhysics]], [[AgentConfinement]], [[RoleWorldProfileSO]]
