---
tags: [script, core]
---

# GameEvents.cs

**Ruta:** `Core/GameEvents.cs`

**Responsabilidad:** Bus de eventos cross-system estático. 10 eventos: registry/breeding/furniture/inventory/navmesh/customer. Llama y evento son pares (ej. `OnRegistryChanged` + método `RegistryChanged()`). Patrón: gameplay dispara evento → GameManager persiste + UI refresca.

**S93:** Eliminados `OnCreatureMinted`, `OnCustomerSpawned`, `OnCustomerDecided`, `OnCustomerArrivedAtRegister`, `OnCustomerLeft`. Reducido de 15 a 10 eventos.

## Eventos (10 totales)

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

