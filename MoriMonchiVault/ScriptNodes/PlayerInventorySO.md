---
tags: [script, inventory, equipment, persistence]
---

# PlayerInventorySO

**Ruta:** `Data/Player/PlayerInventorySO.cs`

**Responsabilidad:** Inventario persistente del jugador (SO único, singleton runtime via GameManager). Gestiona cuatro categorías: furniture (F# set), world props (I# list dupes), equipment (EQ# free-placement grids per slot), hotbar (6 I# slots). Dueno de verdad de "qué posee el jugador". Muta solo a través de métodos explícitos; cada mutación llama `MarkDirty()` + es seguida por `GameEvents.InventoryChanged(inventory)` disparado por el caller (GameManager, EquipmentBackpackUITK, DevToolsConsole, etc.). Persiste via JSON local + Cloud Save.

## Estructura de Datos

| Categoría | Tipo | ID Prefix | Descripción |
|-----------|------|-----------|-------------|
| **Furniture owned** | `List<string>` (set) | F# | FurnitureDefinitionSO; ownership = posesión, cada pieza se puede colocar ilimitadamente |
| **World props** | `List<string>` (list+dupes) | I# | ItemDefinitionSO; world props son instances únicas, dupe = múltiples objetos físicos |
| **Equipment grids** | `Dict<EquipmentSlot, List<string>>` | EQ# | EquipmentSO; **free-placement grid por slot**, null entry = celda vacía, índice = cell index |
| **Hotbar slots** | `string[6]` | I# | Refs a world props puestos en hotbar, persisten entre sesiones; null = slot vacío |
| **Dabloons** | `int` | — | Moneda única |

## API Pública — Furniture

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `AddFurniture(id)` | `bool` | Agrega F# a owned set (no dupe). Marca dirty. True si éxito. |
| `RemoveFurniture(id)` | `bool` | Remueve F# de owned set. Marca dirty. True si éxito. |
| `HasFurniture(id)` | `bool` | Checka si posee F# |
| `FurnitureOwned` | `IReadOnlyList<string>` | Readonly snapshot set |

## API Pública — World Props

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `AddWorldProp(id)` | `void` | Agrega I# a lista (dupe OK). Marca dirty. |
| `RemoveWorldProp(id)` | `bool` | Remueve UNA instancia de I#. Marca dirty. True si éxito. |
| `WorldPropsStored` | `IReadOnlyList<string>` | Readonly snapshot lista |

## API Pública — Equipment (S33)

**Grilla libre por slot: null entry = celda vacía, índice = cell index. No hay compacting.**

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `AddEquipment(slot, id)` | `void` | Agrega EQ# a grilla de slot: inserta en primer hueco, o append. Marca dirty. |
| `RemoveEquipmentAt(slot, index)` | `bool` | Limpia celda index (= null), TrimTrailing (no compact). Marca dirty. True si éxito. |
| `MoveEquipment(slot, from, to)` | `void` | Drag&drop: si `to` vacío = mueve; si ocupada = swapea. Expande grid si needed. TrimTrailing. Marca dirty. |
| `GetEquipment(slot)` | `IReadOnlyList<string>` | Readonly grid cells (puede contener nulls) |
| `ClearEquipmentOwned()` | `void` | Limpia todas las grillas de equipment. Marca dirty. **S33** |

## API Pública — Hotbar

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `AddHotbarItem(slot, id)` | `void` | Asigna I# a hotbar[slot] (0-5). Marca dirty. |
| `RemoveHotbarItem(slot)` | `bool` | Limpia hotbar[slot] (= null). Marca dirty. True si éxito. |
| `GetHotbarItem(slot)` | `string` | Lee hotbar[slot] |
| `ClearHotbar()` | `void` | Limpia todos hotbar slots. Marca dirty. |

## API Pública — Dabloons

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Dabloons` | `int` | Getter (readonly) |
| `AddDabloons(amount)` | `void` | Suma (si > 0). Marca dirty. |
| `SpendDabloons(amount)` | `bool` | Resta (si amount > 0 y dabloons >= amount). Marca dirty. True si éxito. |
| `ResetDabloons()` | `void` | Vuelve a 0. Marca dirty. |

## API Pública — Clear Helpers (DEV/reset)

| Método | Descripción |
|--------|-------------|
| `ClearFurnitureOwned()` | Limpia all furniture. Marca dirty. |
| `ClearWorldPropsStored()` | Limpia all world props. Marca dirty. |
| `ClearEquipmentOwned()` | **S33** Limpia all equipment grids. Marca dirty. |
| `ClearHotbar()` | Limpia all hotbar slots. Marca dirty. |

## Equipment Grids — Free Placement (S33)

Grid por `EquipmentSlot` (Weapon, Armor, Amulet):
- `List<string>` cells, null = vacío, ID = item presente
- **No compacting:** RemoveEquipmentAt(slot, i) = `grid[i] = null`, TrimTrailing() solo remueve trailing nulls
- **MoveEquipment(slot, from, to):** si `to` vacío = mueve (shift), si ocupada = swapea
- **AddEquipment(slot, id):** busca primer hueco, inserta; si no hay = append
- **Cell index = physical grid position** — determinista para UI (9-cell 3×3 pages)

Helper `GridFor(slot)` lazy-initializa grilla por slot si no existe.

## Persistencia

- **MarkDirty():** Marca el SO como modificado (serialization framework)
- **InventoryData (Cloud):** Estructura serializable con 4 campos:
  - `furnitureOwned`
  - `worldPropsStored`
  - `equipmentGrids` — deep copy de dict completo para Cloud Save
  - `hotbarSlots`
  - `dabloons`
- GameManager escucha `GameEvents.InventoryChanged(inventory)` y dispara save

## Vinculado a

- [[Index/07 - Persistence & Identity]]
- [[Index/06 - Equipment System]] — S33
- [[GameManager]] — orquestador, escucha InventoryChanged
- [[GameEvents]] — InventoryChanged disparado por mutadores
- [[EquipmentBackpackUITK]] — S33, muta grillas de equipment
- [[DevToolsConsole]] — dev buttons para muta inventory
- [[StoreManager]] — vende furniture/props
- [[StorageContainer]] — pickup/drop world props
- [[Hotbar]]? — hotbar persist

## Conexiones

**Entrada:**
- `EquipmentBackpackUITK.EquipItem()` → `AddEquipment(slot, id)`, `RemoveEquipmentAt(slot, i)`, `MoveEquipment(slot, from, to)` + `GameEvents.InventoryChanged`
- `DevToolsConsole` buttons → `AddDabloons()`, `ClearFurnitureOwned()`, `AddEquipment()`, `ClearEquipmentOwned()` + `GameEvents.InventoryChanged`
- `StoreManager.BuyFurniture()` → `AddFurniture()` + `SpendDabloons()` + `GameEvents.InventoryChanged`

**Salida:**
- Persistencia via `GameManager` escuchando `GameEvents.InventoryChanged`
- `CreatureGridUITK` lee grillas via `GetEquipment(slot)` para mostrar equip-row 3 iconos
- `MorimonchiDetailInfoUITK` lee grillas para tab Equipo + backpack popup

## Notas

- **No compacting en remove:** IndexOf búsqueda sigue siendo O(n) pero operaciones remove son O(1) conceptualmente (set cell = null). Trim solo limpia trailing para no tener gaps al final.
- **S33:** Estructuras `equipmentGrids` y método `ClearEquipmentOwned()` son nuevos; equipmentGrids es dict<EquipmentSlot, List<string>>.
- **Lazy grids:** GridFor() crea grilla per slot on first AddEquipment — vacío al inicio.
- **DevNoConsume:** En EquipmentBackpackUITK, flag que bypassa la lógica normal de inventory — solo para testing rápido.
