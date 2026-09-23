---
tags: [script, core, events-bus]
---

# GameEvents.cs

**Ruta:** `Core/GameEvents.cs`

**Responsabilidad:** Bus de eventos cross-system estático. 16 eventos: registry/breeding/furniture/inventory/navmesh/customer/expedition/creature-lifecycle/world-state/day-cycle. Llama y evento son pares (ej. `OnRegistryChanged` + método `RegistryChanged()`). Patrón: gameplay dispara evento → GameManager persiste + UI refresca. S131: agregados 4 eventos de reloj (WorldState changed/reloaded, Day started, DayBlock changed).

## Eventos (16 totales)

| Evento | Parámetros | Descripción |
|--------|-----------|-------------|
| `OnRegistryChanged` | `CreatureRegistrySO registry` | Mutación gameplay de creatures → persist + UI |
| `OnRegistryReloaded` | `CreatureRegistrySO registry` | Reemplazo wholesale (cloud pull) → UI only |
| `OnBreedingCompleted` | `(mother, father, child)` | Breeding finalizado |
| `OnFurnitureChanged` | `FurnitureRegistrySO registry` | Mutación de muebles → persist + UI |
| `OnFurnitureReloaded` | `FurnitureRegistrySO registry` | Reemplazo wholesale → UI only |
| `OnNavMeshWillRebake` | `()` | Bracket pre-rebake (agentes se congelan) |
| `OnNavMeshRebaked` | `()` | Bracket post-rebake (agentes se re-anclan) |
| `OnInventoryChanged` | `PlayerInventorySO inventory` | Mutación inventario → persist + UI |
| `OnInventoryReloaded` | `PlayerInventorySO inventory` | Reemplazo wholesale → UI only |
| `OnCustomerSold` | `(NpcAgent, CreatureDNA, int price)` | Venta completada |
| `OnExpeditionReturned` | `ExpeditionReturn r` | Retorno de arena: resultado, materiales, energía gastada → UI toast |
| `OnCreatureDeparted` | `CreatureDNA dna` | Criatura partió (muerte/venta) → UI aviso + social graph |
| `OnWorldStateChanged` | `WorldStateSO state` | **(S131)** Mutación de estado mundo (día/minuto/tutorial) → persist + cloud push |
| `OnWorldStateReloaded` | `WorldStateSO state` | **(S131)** Reemplazo desde cloud → UI only |
| `OnDayStarted` | `int day` | **(S131)** Nuevo día comenzó (día incrementado) |
| `OnDayBlockChanged` | `DayBlockDef block` | **(S131)** Bloque horario cambió → UI reloj, habilita/deshabilita sistemas |

## Contrato de eventos

**Changed vs Reloaded:**
- `Changed`: Mutación gameplay (gameplay code llama el evento) → GameManager persiste + cloud push + UI refresca
- `Reloaded`: Reemplazo desde fuente externa (cloud pull / reset) → UI refresca, NO persist ni cloud push
- `CreatureDeparted`: Criatura ida (IsDead=true o Sold) → UI desaparece ficha + social graph actualiza
- `WorldStateChanged`: Muta Day/MinuteOfDay → GameManager.PersistWorldState + cloud push (integrado S131)
- `DayBlockChanged`: Bloque cambió (automático en GameClock) → sistemas reaccionan (clientes open/close, expediciones open/close)

**Datos en payload:**
- El evento transporta la data (registry, inventario, resultado, criatura, estado, bloque, etc.)
- Suscriptores leen del payload, NUNCA hacen `GameManager.Instance.Xxx`
- Desacoplamiento total: evento es la única comunicación

## S131: Eventos de Reloj (HC-4)

### OnWorldStateChanged / WorldStateChanged(WorldStateSO state)

**Disparado por:** GameClock cuando cambia Day o MinuteOfDay (cada frame si hay movimiento de tiempo).

**Suscriptores:**
- `GameManager.PersistWorldState()` → `SaveSystem.SaveWorldState(state)` + RequestPush

**Invariantes:** Dispara SIEMPRE después de que GameClock.Update() mute state. Si Paused, no dispara.

### OnWorldStateReloaded / WorldStateReloaded(WorldStateSO state)

**Disparado por:** `CloudSyncService.SyncGameWorld()` después de pull de cloud.

**Suscriptores:**
- `GameClock.HandleWorldStateReloaded()` → fija `state = reloaded`, `blockIndex`, `loaded = true`, dispara DayBlockChanged

**Invariantes:** Reemplazo wholesale (no mergea local).

### OnDayStarted / DayStarted(int day)

**Disparado por:** GameClock cuando Day incrementa.

**Suscriptores:**
- (Actualmente ninguno, reservado para expansión: NPC diarios, events, reset de cooldowns)

**Invariantes:** Dispara 1x por día (cuando MinuteOfDay wraps ≥ 1440).

### OnDayBlockChanged / DayBlockChanged(DayBlockDef block)

**Disparado por:** GameClock cuando BlockIndex cambia.

**Suscriptores:**
- UI reloj (muestra nombre del bloque actual)
- Sistemas: `NpcController.OnDayBlockChanged()` (activa/desactiva spawning si CustomersOpen)
- (Próximo: ExpeditionManager habilita si ExpeditionOpen)

**Invariantes:** Dispara 1x por cambio de bloque (puede ser múltiples x día).

## Cambios Históricos

**S93:** Poda de 15 → 10 eventos.
**S121:** `OnExpeditionReturned` nuevo.
**S129:** `OnCreatureDeparted` nuevo.
**S131:** 4 eventos de reloj agregados (WorldStateChanged, WorldStateReloaded, DayStarted, DayBlockChanged).

## Vinculado a

- [[Index/07 - Persistence & Identity]]
- [[Index/24 - Puente Tienda-Arena]]
- [[Index/28 - Cimientos y camino a Game Ready]]
- [[Index/09 - Active Context]]

## Conexiones

**Gameplay:**
- [[GameManager]] — suscribe Changed para persistencia
- [[CloudSyncService]] — dispara Reloaded
- [[BreedingService]], [[FurnitureService]] — disparan Changed
- [[CreatureLifecycle]] — dispara CreatureDeparted

**Tiempo (S131):**
- [[GameClock]] — dispara DayStarted, DayBlockChanged, suscribe WorldStateReloaded
- [[WorldStateSO]] — payload de WorldState events
- [[DayScheduleSO]] — payload de DayBlockDef

**UI/Systems:**
- [[InfoOverlayUITK]], [[CreatureGridUI]] — reaccionan a RegistryChanged, RegistryReloaded
- [[NpcController]] — reacciona a DayBlockChanged para spawn customer
- [[UIManager]] — reacciona a múltiples para refresh

## Notas (S131 HC-4)

- **Densidad:** 16 eventos, cada uno muy específico. No hay eventos "catch-all".
- **Changed→Persist:** Patrón: gameplay dispara Changed → GameManager suscriptor persiste. No hay gaming code que calle SaveSystem directo.
- **World state:** Ahora unificado en SO único, eventos claros. Antes era desacoplado.
- **BlockDef en payload:** `DayBlockDef` es null-safe en GameClock.Block; UI y systems null-check.
