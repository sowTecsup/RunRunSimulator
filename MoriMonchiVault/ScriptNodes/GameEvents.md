---
tags: [script, core]
---

# GameEvents.cs

**Ruta:** `Core/GameEvents.cs`

**Responsabilidad:** Bus de eventos cross-system estático. 12 eventos: registry/breeding/furniture/inventory/navmesh/customer/expedition/creature-lifecycle. Llama y evento son pares (ej. `OnRegistryChanged` + método `RegistryChanged()`). Patrón: gameplay dispara evento → GameManager persiste + UI refresca.

**S93:** Eliminados `OnCreatureMinted`, `OnCustomerSpawned`, `OnCustomerDecided`, `OnCustomerArrivedAtRegister`, `OnCustomerLeft`. Reducido de 15 a 10 eventos. **S121:** Agregado `OnExpeditionReturned`. **S129:** Agregado `OnCreatureDeparted` (muerte/venta).

## Eventos (12 totales)

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
| `OnExpeditionReturned` | `ExpeditionReturn r` | **(S121)** Retorno de arena: resultado, materiales, energía gastada → UI toast |
| `OnCreatureDeparted` | `CreatureDNA dna` | **(S129)** Criatura partió (muerte/venta) → UI aviso + social graph |

## Contrato de eventos

**Changed vs Reloaded:**
- `Changed`: Mutación gameplay (gameplay code llama el evento) → GameManager persiste + cloud push + UI refresca
- `Reloaded`: Reemplazo desde fuente externa (cloud pull / reset) → UI refresca, NO persist ni cloud push
- `CreatureDeparted` (S129): Criatura ida (IsDead=true o Sold) → UI desaparece ficha + social graph actualiza

**Datos en payload:**
- El evento transporta la data (registry, inventario, resultado, criatura, etc.)
- Suscriptores leen del payload, NUNCA hacen `GameManager.Instance.Xxx`
- Desacoplamiento total: evento es la única comunicación

## S129: OnCreatureDeparted

**Disparado por:** `CreatureLifecycle.Kill()` y `CreatureLifecycle.Adopt()`

**Suscriptores:**
- `InfoOverlayUITK` — fade out criatura de panel + aviso "Partió" en toast
- `SocialGraphService` — actualiza árbol genealógico (padres/hijos vivos)

**Invariantes:** Dispara SIEMPRE después de `registry.Depart(id)` (criatura ya en estante `Departed`).

## Cambios Históricos

**S93:** Poda de 15 → 10 eventos.
**S121:** `OnExpeditionReturned` nuevo.
**S129:** `OnCreatureDeparted` nuevo.

## Vinculado a

- [[Index/07 - Persistence & Identity]]
- [[Index/24 - Puente Tienda-Arena]] (S121)
- [[Index/28 - Cimientos y camino a Game Ready]] (S129)

**Conexiones:** [[GameManager]], [[CloudSyncService]], [[BreedingService]], [[FurnitureService]], [[CreatureRegistrySO]], [[PlayerInventorySO]], [[ExpeditionBridge]], [[InfoOverlayUITK]], [[CreatureLifecycle]], [[SocialGraphService]]
