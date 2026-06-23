---
tags: [script, world]
---

# MoriMochiAgent.Brain.cs

**Ruta:** `World/AI/MoriMochiAgent.Brain.cs`

**Responsabilidad:** Partial class con la máquina de estados (FSM) de comportamiento: idle/roaming/reacting/seeking needs/using stations/courting. Tick methods + decay de necesidades por frame + lógica de reacción al jugador. Sin comentarios; la documentación vive en el vault.

**Propiedades públicas:**
- `IsHeld` (bool) → True si en state Carried.
- `IsAirborne` (bool) → True si en state Thrown (lanzado, no mientras está agarrado).
- `IsPenned` (bool) → True si `currentContainer != null`.
- `IsForSale` (bool) → True si `currentContainer is StoreContainer`. NameTag la usa para swapear a layout "tienda".
- `IsCourting` (bool) → True si en state Courting.
- `IsRecovering` (bool) → True si en state Recovering.
- `IsInFriendlyReaction` (bool) → True si Reacting pero no Flee.
- `IsBeingPetted` (bool) → True mientras `pettingDisplayTimer > 0`.
- `CanBePetted` (bool) → Reacting + amistosa + player facing.
- `Intent` (CreatureIntent) → lectura viva del intent actual para NameTag.

**Estados (AgentState enum):**
- `Idle` — quieto, espera `idleDuration` luego roaming.
- `Roaming` — navega a waypoint random, llega y idle/roaming random.
- `Reacting` — sigue/se acerca/retrocede/huye del jugador.
- `Carried` → sigue a `IsHeld`.
- `Thrown` → sigue a `IsAirborne`.
- `Recovering` — después de lanzado.
- `SeekingNeed` — navega a `NeedStation` reservada.
- `UsingStation` — dentro del station, consume necesidad.
- `Courting` — orbita/tiende con pareja.

**Tick methods:**
- `TickIdle()` → espera timer o entra seeking/reacción.
- `TickRoaming()` → navega, llega y tranisiciona.
- `TickReacting()` → sigue jugador, timeout, sale si necesidad crítica.
- `TickSeekingNeed()` → navega al use point del station.
- `TickUsingStation()` → refill necesidades, sale cuando full.
- `TickCourting()` → orbita o tiende según rol.
- `TickNeeds()` → decay Health/Energy/Affect por frame (solo si spawned, no en registry puro).

**Transiciones:**
- `EnterIdle()`, `EnterRoaming()`, `BeginReaction()`, etc.
- `ReleaseStation()` → limpia reservación de `NeedStation`.

**Helpers:**
- `TryEnterNeedSeeking()` → detecta necesidad crítica, reserva station, transiciona.
- `TryGetCriticalNeed()` → prioridad Health > Energy > Affect.
- `ReactIfPlayerNear()` → chequea proximidad, cooldown, estado penned, emite reacción.
- `IsPlayerFacingMe()` → dot product de player.forward · to-creature (xz), rango `petRadius`, cone `petLookAngle`.
- `NextRoamDestination()` → si penned, punto en bounds; si libre, area preference + random.
- `TryGetPreferredPoint()` → samplea NavMesh area preferida.
- `Interact()` → IInteractable para acariciar; boost Affect, cooldown reacción, show "Petting..." timer.

**Vinculado a:** [[Index/06 - Player & World]], [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[MoriMochiAgent]], [[NeedsState]], [[CreatureDNA]], [[PersonalityProfileSO]], [[NeedStation]], [[NeedStationRegistry]], [[MoriMochiContainer]]
