---
tags: [script, dev-tools, utility]
---

# DevToolsConsole

**Ruta:** `Core/DevToolsConsole.cs`

**Responsabilidad:** Componente dev (MonoBehaviour) para manipular inventario y expediciones en editor/playtesting sin interfaz de juego. Buttons Odin (BoxGroups): Dabloons, Furniture, World Props, Genetics, Expedition. Cada acción muta `gameManager.Inventory` o `gameManager.Registry` vía su API pública, emite `GameEvents.InventoryChanged()` / `GameEvents.RegistryChanged()`. Solo para desarrollo.

**S128:** Se borran **todos los buttons de combate** (Open Combat Panel, refs a CombatTuningSO). Mantiene Genetics (Reroll Potentials).

**S129:** Se eliminan **todos los buttons de equipo** (Add Equipment Item, Add Full Equipment Catalog, Clear Equipment) y sus campos (`devEquipmentItem`, `equipmentDatabase`). Mantiene Genetics.

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

### Genetics (DEV)

| Button | Acción |
|--------|--------|
| `Reroll Potentials (DEV)` | Cambia HornPotential, BackPotential, WingPotential aleatorio en vivas no vendidas |

### Expedition (DEV) — S119

| Button | Acción |
|--------|--------|
| `Salir de expedición (DEV)` | Dispara ExpeditionBridge.Depart() |

## Campos Serializados

| Campo | BoxGroup | Tipo | Descripción |
|-------|----------|------|-------------|
| `gameManager` | Setup | `GameManager` | Required |
| `devDabloonsAmount` | Dev Tools | `int` | Monto a agregar (default 500) |
| `expeditionBridge` | Expedition | `ExpeditionBridge` | Ref al puente |

## Integración S128-S129

- Wallet.Add() en lugar de inventory.AddDabloons() directa
- Botones de combate removidos completamente (S128)
- Botones de equipo removidos completamente (S129)
- Mantiene Genetics (Reroll Potentials)
- Mantiene Expedition (testeo de flujo tienda/arena)

## Vinculado a

[[Index/09 - Dev Tools]]

**Conexiones:** [[GameManager]], [[PlayerInventorySO]], [[CreatureRegistrySO]], [[GameEvents]], [[Wallet]], [[ExpeditionBridge]], [[CreatureGenerator]]
