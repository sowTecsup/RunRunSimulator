---
tags: [script, core]
---

# GameEvents.cs

**Ruta:** `Core/GameEvents.cs`

**Responsabilidad:** Bus de eventos cross-system estático. Eventos registry/breeding/furniture/inventory: `OnRegistryChanged`, `OnRegistryReloaded`, `OnCreatureMinted`, `OnBreedingCompleted`, `OnFurnitureChanged`, `OnFurnitureReloaded`, `OnInventoryChanged`, `OnInventoryReloaded`, `OnNavMeshWillRebake`, `OnNavMeshRebaked`. Eventos cliente/NPC: `OnCustomerSpawned(NpcAgent)`, `OnCustomerDecided(NpcAgent, CreatureDNA)`, `OnCustomerArrivedAtRegister(NpcAgent)`, `OnCustomerSold(NpcAgent, CreatureDNA, int)`, `OnCustomerLeft(NpcAgent, bool)`.

**S75 CAMBIOS:** Eliminados `OnCombatCompleted` y `OnCombatLogged` (demolición del combate). Agregados `OnInventoryChanged` e `OnInventoryReloaded` (sistema de inventario de recursos).

## Eventos principales

| Evento | Parámetros | Descripción |
|--------|-----------|-------------|
| `OnRegistryChanged` | `CreatureRegistrySO registry` | Mutación gameplay de creatures → persist + UI |
| `OnRegistryReloaded` | `CreatureRegistrySO registry` | Reemplazo wholesale (cloud pull) → UI only |
| `OnCreatureMinted` | `CreatureDNA creature` | Criatura nueva creada |
| `OnBreedingCompleted` | `(mother, father, child)` | Breeding finalizado |
| `OnFurnitureChanged` | `FurnitureRegistrySO registry` | Mutación de muebles → persist + UI |
| `OnFurnitureReloaded` | `FurnitureRegistrySO registry` | Reemplazo wholesale → UI only |
| `OnNavMeshWillRebake` | `()` | Bracket pre-rebake (agentes se congelan) |
| `OnNavMeshRebaked` | `()` | Bracket post-rebake (agentes se re-anclan) |
| `OnInventoryChanged` | `PlayerInventorySO inventory` | Mutación inventario → persist + UI |
| `OnInventoryReloaded` | `PlayerInventorySO inventory` | Reemplazo wholesale → UI only |
| `OnCustomerSpawned` | `NpcAgent agent` | NPC entra a tienda |
| `OnCustomerDecided` | `(NpcAgent, CreatureDNA target)` | NPC eligió criatura |
| `OnCustomerArrivedAtRegister` | `NpcAgent agent` | NPC llegó a caja |
| `OnCustomerSold` | `(NpcAgent, CreatureDNA, int price)` | Venta completada |
| `OnCustomerLeft` | `(NpcAgent, bool sold)` | NPC se va (compró o no) |

## Contrato de eventos

**Changed vs Reloaded:**
- `Changed`: Mutación gameplay (gameplay code llama el evento) → GameManager persiste + cloud push + UI refresca
- `Reloaded`: Reemplazo desde fuente externa (cloud pull / reset) → UI refresca, NO persist ni cloud push

**Datos en payload:**
- El evento transporta la data (registry, inventario, etc.)
- Suscriptores leen del payload, NUNCA hacen `GameManager.Instance.Xxx`
- Desacoplamiento total: evento es la única comunicación

## Vinculado a

- [[Index/07 - Persistence & Identity]]

**Conexiones:** [[GameManager]], [[CloudSyncService]], [[BreedingService]], [[FurnitureService]], [[CreatureRegistrySO]], [[PlayerInventorySO]]
