---
tags: [script, world, agent, internal]
---

# AgentContext.cs

**Ruta:** `World/AI/AgentContext.cs`

**Responsabilidad:** Contenedor de estado puro para un MoriMochiAgent (datos compartidos entre colaboradores sin duplicación). Almacena referencias a componentes (NavMeshAgent, Rigidbody, Collider, Transform), datos de DNA/perfil, estado de juego (ubicación actual, velocidad base), máscaras NavMesh (libre vs confinado), banderas de operación (rebake en curso), y NUEVO en S64: lista de percepciones sociales (Percepts) escrita por AgentSenses y leída por AgentSocial. Expone helpers para consultas de seguridad: `IsNavMeshControlled()` (estado controlado por NavMesh), `IsBreeding`, `IsMoving`, `PlanarDistanceToPlayer()`, y operaciones del agente: `SetStopped()`, `SetDestinationSafe()` (con muestreo de NavMesh), `SetColliderTrigger()`. **S69:** Enum `AgentState` ampliado con `HandFeed`. No tiene lógica de estado.

## Enum AgentState

```csharp
public enum AgentState
{
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
    HandFeed      // comiendo de la mano (S69 NUEVO)
}
```

**Cambios S69:**
- **NUEVO:** `HandFeed` — criatura acepta comida de la mano del jugador; manejado por AgentBrain.TickHandFeed()

## Campos internos

- `Owner` (MoriMochiAgent)
- `Body, Agent, Rb, Col` — componentes del GO
- `BaseSpeed` — velocidad cached del NavMeshAgent
- `State` — estado actual (AgentState enum, S69: puede ser HandFeed)
- `Dna, Profile` — datos genéticos y perfil de rol
- `Player, HoldAnchor` — transforms de referencias externas
- `CurrentContainer` — el corral/contenedor que lo confina (null si libre)
- `FreeAreaMask, ConfinedAreaMask` — máscaras NavMesh por área
- `RebakeInProgress` — bandera de rebake en curso
- `Percepts` — **S64 NUEVO** `List<Percept>` ordenada por distancia, capped a MaxPercepts. Escrita por AgentSenses.Tick(), leída por AgentSocial

## Métodos

- `IsNavMeshControlled() → bool` — verdadero si el estado no es Carried/Thrown/Recovering
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
- Incluye Player, Monchi (con afinidad), Customer, Prop
- Nunca null: limpiada si el agente no está en control NavMesh
- Pizarrón compartido con AgentSocial para decisiones sin re-consulta

## Cambios S69

- **Enum AgentState ampliado:** +`HandFeed` state
- AgentBrain.TickHandFeed() controla transiciones desde/hacia HandFeed
- HandFeed es estado de transición: entra vía TryEnterHandFeed() si gates abren (hotbar IsOfferingFood + Health<feedHungerThreshold + dist≤feedNoticeRadius)

## Vinculado a

[[Index/06 - Player & World]], [[Index/02 - Genetics & Breeding]], [[MoriMonchiVault/Index/14 - Social V2]]

## Conexiones

[[MoriMochiAgent]], [[AgentBrain]], [[AgentPhysics]], [[AgentConfinement]], [[AgentSenses]], [[AgentSocial]], [[RoleWorldProfileSO]], [[HotbarController]]
