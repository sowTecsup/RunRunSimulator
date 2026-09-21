---
tags: [script, store, furniture]
---

# DeliveryBox

**Ruta:** `Systems/Store/DeliveryBox.cs`

**Responsabilidad:** Paquete físico de delivery. `IInteractable` → spawna el mueble/item/criaturas comprados.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `Configure(ItemDefinitionSO def)` | Configura item a spawnear (props del mundo) |
| `Configure(CreatureBoxSO box)` | Configura caja de criaturas a abrir (S130 NUEVO) |
| `Interact()` | Abre caja: si CreatureBox, mintea criaturas; si ItemDefinitionSO, spawna prop |

## Flujo Interact: CreatureBox (S130 NUEVO)

```
1. Obtiene GameManager.Instance.Registry
2. Loop 0..CreatureBox.Count:
   - GameManager.MintCreature() → DNA (sin evento)
   - MoriMochiSpawner.RegisterBirthLaunch(id, muzzle) — registra para lanzamiento
   - Incrementa minted
3. Si minted > 0: dispara ÚNICO GameEvents.RegistryChanged(registry)
4. Destroy(gameObject)
```

**Invariante:** Un solo `RegistryChanged` tras N minteos, no N eventos. El cañón de spawn recibe N registros vía `RegisterBirthLaunch(id, position)`.

## Flujo Interact: ItemDefinitionSO

```
1. Validar item.Prefab
2. Instantiate(prefab, position, rotation)
3. Buscar WorldPropInstance en el GO spawneado
4. Configure(item.Id)
5. Destroy(gameObject) — la caja
```

## Campos Privados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `item` | `ItemDefinitionSO` | Item a spawnear (ReadOnly) |
| `creatureBox` | `CreatureBoxSO` | Caja de criaturas (ReadOnly, S130) |

## Integración S130

Parte de la expansión C5 (catálogo de cajas). Las cajas llegan como items entregables (`StoreManager.BuyCreatureBox()` → `SpawnDeliveryBox()` → `Configure(CreatureBoxSO)`). Al interactuar, abre la caja: mintea N criaturas, las registra para lanzamiento por cañón en una sola oleada (`RegisterBirthLaunch`), y dispara un único evento de persistencia.

## Vinculado a

[[Index/04 - Store & Transactions]]
[[Index/10 - Furniture & Building]]

**Conexiones:** [[StoreManager]], [[Interfaces]], [[FurnitureSpawner]], [[MoriMochiSpawner]], [[GameManager]], [[GameEvents]]

