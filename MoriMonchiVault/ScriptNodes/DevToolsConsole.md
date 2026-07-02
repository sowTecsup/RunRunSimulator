---
tags: [script, dev-tools, equipment]
---

# DevToolsConsole.cs

**Ruta:** `Core/DevToolsConsole.cs`

**Responsabilidad:** Componente dev (MonoBehaviour) para manipular inventario en editor/testing sin interfaz de juego (playtesting rápido). Buttons Odin (BoxGroups): Dabloons, Furniture, World Props, **Equipment (S33)**. Cada acción muta `gameManager.Inventory` vía su API pública, emite `GameEvents.InventoryChanged(inventory)`. Refs serializadas [SerializeField]. Solo para desarrollo (no incluir en builds release).

## BoxGroups / Buttons Odin

### Setup
- `GameManager` ref — required (donde vive Inventory singleton)

### Dev Tools (Dabloons + Furniture + Props)

| Button | Acción | Muta |
|--------|--------|------|
| `Add Dabloons (DEV)` | Suma `devDabloonsAmount` (default 500) | `inventory.AddDabloons()`, dispara InventoryChanged |
| `Reset Dabloons (DEV)` | Vuelve a 0 | `inventory.ResetDabloons()`, dispara InventoryChanged |
| `Clear Furniture Owned (DEV)` | Limpia lista owned | `inventory.ClearFurnitureOwned()`, dispara InventoryChanged |
| `Clear World Props (DEV)` | Limpia props + hotbar | `inventory.ClearWorldPropsStored()`, `ClearHotbar()`, dispara InventoryChanged |

### Equipment (DEV) — S33

**Nuevos buttons para equipar ítems sin interfaz:**

| Button | Acción | Refs | Muta |
|--------|--------|------|------|
| `Add Equipment Item (DEV)` | Agrega 1 EquipmentSO a grilla | `devEquipmentItem` (insp), `equipmentDatabase` | `inventory.AddEquipment(slot, id)`, dispara InventoryChanged |
| `Add Full Equipment Catalog (DEV)` | Agrega TODOS los items catalog | `equipmentDatabase` | Itera todos IDs, llama AddEquipment x cada uno, dispara InventoryChanged |
| `Clear Equipment (DEV)` | Limpia todas grillas equipo | — | `inventory.ClearEquipmentOwned()`, dispara InventoryChanged |

## Campos Serializados

| Campo | BoxGroup | Tipo | Descripción |
|-------|----------|------|-------------|
| `gameManager` | Setup | `GameManager` | Required ref al orquestador |
| `devDabloonsAmount` | Dev Tools | `int` | Amount a agregar (default 500) |
| — | — | — | — |
| `devEquipmentItem` | Equipment (DEV) | `EquipmentSO` | **S33** Item individual a agregar |
| `equipmentDatabase` | Equipment (DEV) | `EquipmentDatabaseSO` | **S33** Para catalog completo |

## Flujo Típico (Playtesting)

1. **Populate inventory rápido:** Click "Add Full Equipment Catalog" → todos los items en grillas
2. **Open detail panel:** Click MM en grid
3. **Tab Equipo → click equip-card:** Abre backpack popup (EquipmentBackpackUITK)
4. **Drag/click items:** Equipa, desequipa, mueve en grid

O directo sin UI:
1. **Assignar `devEquipmentItem` en inspector**
2. **Click "Add Equipment Item (DEV)"** → item en grilla Weapon (u otro según `item.Slot`)

## Mensajes Debug

Cada button logguea a console:
```
[DevToolsConsole] +500 Dabloons → total: 1234
[DevToolsConsole] Equipment owned list cleared.
[DevToolsConsole] +1 Sword (EQ_SWORD_01)
[DevToolsConsole] Added 23 equipment items from catalog.
```

## Vinculado a

- [[Index/09 - Dev Tools]]
- [[GameManager]] — ref, obtiene Inventory singleton
- [[PlayerInventorySO]] — muta via API pública
- [[EquipmentBackpackUITK]] — S33, para interacción full UI post-equipar
- [[GameEvents]] — dispara InventoryChanged
- [[EquipmentDatabaseSO]] — catalog browse para S33

## Conexiones

**Entrada:**
- Inspector buttons (Odin, MonoBehaviour inspector)

**Salida:**
- `PlayerInventorySO.AddEquipment()`, `RemoveEquipmentAt()`, etc.
- `GameEvents.InventoryChanged(inventory)` — GameManager escucha y persiste

## Notas

- **S33 Equipment:** Nuevos buttons para dev-test la grilla libre de equipo sin pasar por UI (rápido para playtesting exploratorio).
- **Catalog button:** `equipmentDatabase.GetAllIDs()` → itera todos, resuelve cada uno, agrega a grilla según `item.Slot`. Puede llenar grillas con muchos items.
- **devNoConsume:** Si quiero equipar sin consumir items del inventory, ir a `EquipmentBackpackUITK` inspector y toggle `devNoConsume`.
- **Safety:** Falta validation si gameManager es null, pero logguea warning — no crashea.
