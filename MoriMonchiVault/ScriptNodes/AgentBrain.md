---
tags: [script, world, agent, internal]
---

# AgentBrain.cs

**Ruta:** `World/AI/AgentBrain.cs`

**Responsabilidad:** Orquestación de la máquina de estados NavMesh (comportamiento autónomo del MoriMochiAgent). Maneja transiciones entre Idle/Roaming/Reacting/SeekingNeed/UsingStation/Courting. Tick per-frame decay de necesidades (Health/Energy/Affect), búsqueda de estaciones críticas, interacción con el jugador (petting), e Intención expuesta (para NameTag). Expone `Intent` (CreatureIntent derivado del estado actual) e interacción de petting (`IsBeingPetted`, `CanBePetted`, `Interact()`).

**Propiedades consultables:**
- `IsBeingPetted → bool` — verdadero 1.5s tras petting
- `IsInFriendlyReaction → bool` — en Reacting pero no fleeing
- `CanBePetted → bool` — Reacting + friendly + player facing
- `Intent → CreatureIntent` — mapping de estado → intención textual (Idle, Wandering, Following, Eating, etc.)
- `IsPlayerFacingMe() → bool` — player a petRadius y dentro de petLookAngle

**Tick methods (llamados por MoriMochiAgent.Update):**
- `TickIdle()` — espera idleMin-idleMax, luego roaming o busca necesidades
- `TickRoaming()` — navega a destino aleatorio, puede entrar en Idle por personalidad
- `TickReacting()` — persigue al jugador (follow/approach/retreat/flee) con timeout
- `TickSeekingNeed()` — avanza hacia estación reservada
- `TickUsingStation()` — consume de la estación hasta llenar
- `TickAlways(dt)` — decay de necesidades cada frame (solo si spawned)

**Interacción:**
- `Interact()` — petting desde el jugador: boost Affect, cooldown, reset a Roaming
- `ReleaseStation()` — libera reserva de necesidad (llamado al cambiar de estado)
- `EnterRoaming()` — transición forzada a roaming (switchboard público)

**State internals:**
- `activeReaction` (ProximityReaction) — tipo de reacción en curso
- `reservedStation` (NeedStation) — estación ocupada
- `idleTimer, idleDuration, reactingTimer, reactCooldownTimer` — temporizadores
- `stateBeforeReact` — estado previo para restaurar post-reacción

**Métodos privados de lógica:**
- `TryEnterNeedSeeking() → bool` — intenta buscar estación crítica
- `TryGetCriticalNeed(out NeedType) → bool` — prioridad Health > Energy > Affect
- `ReactIfPlayerNear() → bool` — arranca reacción si player a rango y personality lo permite
- `BeginReaction()` — transición a Reacting
- `NextRoamDestination()` — destino aleatorio o con preferencia de área
- `TryGetPreferredPoint(out Vector3)` — muestra punto en área preferida por rol

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[MoriMochiAgent]], [[AgentContext]], [[AgentPhysics]], [[RoleWorldProfileSO]], [[NeedStationRegistry]], [[NeedStation]]
