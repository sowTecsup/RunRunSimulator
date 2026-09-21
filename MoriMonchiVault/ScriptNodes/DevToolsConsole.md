---
tags: [script, dev-tools, utility]
---

# DevToolsConsole

**Ruta:** `Core/DevToolsConsole.cs`

**Responsabilidad:** Componente dev (MonoBehaviour) para manipular inventario y expediciones en editor/playtesting sin interfaz de juego. Buttons Odin (BoxGroups): Dabloons, Furniture, World Props, Equipment, **Expedition (S119)**. Cada acción muta `gameManager.Inventory` o `gameManager.Registry` vía su API pública, emite `GameEvents.InventoryChanged()` / `GameEvents.RegistryChanged()`. Solo para desarrollo.

**S128:** Se borran **todos los buttons de combate** (Open Combat Panel, Reroll Potentials, refs a CombatTuningSO). Mantiene Equipment y Expedition.

## BoxGroups / Buttons

### Setup
- `GameManager` ref — required

### Dev Tools (Dabloons + Furniture + Props)

| Button | Acción |
|--------|--------|
| `Add Dabloons (DEV)` | Suma `devDabloonsAmount` vía Wallet |
| `Reset Dabloons (DEV)` | Vuelve a 0 |
| `Clear Furniture Owned (DEV)` | Limpia lista |
| `Clear World Props (DEV)` | Limpia props + hotbar |

### Equipment (DEV)

| Button | Acción |
|--------|--------|
| `Add Equipment Item (DEV)` | Agrega 1 EquipmentSO a grilla |
| `Add Full Equipment Catalog (DEV)` | Agrega TODOS los items |
| `Clear Equipment (DEV)` | Limpia todas grillas |

### Combat (DEV) — **S128 ELIMINADO**

~~`Open Combat Panel (DEV)`~~
~~`Reroll Potentials (DEV)`~~
~~`combatTuning` ref~~

**Botones y ref eliminados con demolición RPS.**

### Expedition (DEV) — S119

| Button | Acción |
|--------|--------|
| `Salir de expedición (DEV)` | Dispara ExpeditionBridge.Depart() |

## Campos Serializados

| Campo | BoxGroup | Tipo | Descripción |
|-------|----------|------|-------------|
| `gameManager` | Setup | `GameManager` | Required |
| `devDabloonsAmount` | Dev Tools | `int` | Monto a agregar (default 500) |
| `devEquipmentItem` | Equipment | `EquipmentSO` | Item individual |
| `equipmentDatabase` | Equipment | `EquipmentDatabaseSO` | Para catalog |
| `expeditionBridge` | Expedition | `ExpeditionBridge` | Ref al puente |

## Integración S128

- Wallet.Add() en lugar de inventory.AddDabloons() directa
- Botones de combate removidos completamente
- Mantiene Equipment (desarrollo de grillas)
- Mantiene Expedition (testeo de flujo tienda/arena)

## Vinculado a

[[Index/09 - Dev Tools]]

**Conexiones:** [[GameManager]], [[PlayerInventorySO]], [[CreatureRegistrySO]], [[GameEvents]], [[Wallet]], [[ExpeditionBridge]]

