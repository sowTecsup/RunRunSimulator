---
tags: [script, dev-tools, equipment, combat, expedition]
---

# DevToolsConsole.cs

**Ruta:** `Core/DevToolsConsole.cs`

**Responsabilidad:** Componente dev (MonoBehaviour) para manipular inventario, combate y expediciones en editor/testing sin interfaz de juego (playtesting rápido). Buttons Odin (BoxGroups): Dabloons, Furniture, World Props, Equipment (S33), Combat (S95), **Expedition (S119)**. Cada acción muta `gameManager.Inventory` o `gameManager.Registry` vía su API pública, emite `GameEvents.InventoryChanged()` / `GameEvents.RegistryChanged()`. Refs serializadas [SerializeField]. Solo para desarrollo (no incluir en builds release).

## BoxGroups / Buttons Odin

### Setup
- `GameManager` ref — required (donde vive Inventory/Registry singleton)

### Dev Tools (Dabloons + Furniture + Props)

| Button | Acción | Muta |
|--------|--------|------|
| `Add Dabloons (DEV)` | Suma `devDabloonsAmount` (default 500) | `inventory.AddDabloons()`, dispara InventoryChanged |
| `Reset Dabloons (DEV)` | Vuelve a 0 | `inventory.ResetDabloons()`, dispara InventoryChanged |
| `Clear Furniture Owned (DEV)` | Limpia lista owned | `inventory.ClearFurnitureOwned()`, dispara InventoryChanged |
| `Clear World Props (DEV)` | Limpia props + hotbar | `inventory.ClearWorldPropsStored()`, `ClearHotbar()`, dispara InventoryChanged |

### Equipment (DEV) — S33

**Buttons para equipar ítems sin interfaz:**

| Button | Acción | Refs | Muta |
|--------|--------|------|------|
| `Add Equipment Item (DEV)` | Agrega 1 EquipmentSO a grilla | `devEquipmentItem` (insp), `equipmentDatabase` | `inventory.AddEquipment(slot, id)`, dispara InventoryChanged |
| `Add Full Equipment Catalog (DEV)` | Agrega TODOS los items catalog | `equipmentDatabase` | Itera todos IDs, llama AddEquipment x cada uno, dispara InventoryChanged |
| `Clear Equipment (DEV)` | Limpia todas grillas equipo | — | `inventory.ClearEquipmentOwned()`, dispara InventoryChanged |

### Combat (DEV) — S95

**Buttons para abrir panel y simular combate:**

| Button | Acción | Refs | Muta |
|--------|--------|------|------|
| `Open Combat Panel (DEV)` | Abre panel combate | — | `UIManager.RequestPanelSet(UIPanelType.Combat, true)` |
| `Reroll Potentials (DEV)` | Re-genera potenciales todas criaturas vivas | `combatTuning` (fallback CreateInstance) | Itera registry, asigna RandomMintPotential a cada DNA, dispara RegistryChanged |

### Expedition (DEV) — S119

**Buttons para probar transiciones y flujo de expedición:**

| Button | Acción | Refs | Muta |
|--------|--------|------|------|
| `Salir de expedición (DEV)` | **S119 NUEVO** Dispara ExpeditionBridge.Depart() | `expeditionBridge` (ref) | Llama `gameManager.FlushToCloud()`, `ExpeditionHandoff.GoToArena()`, carga ArenaSandbox |

## Campos Serializados

| Campo | BoxGroup | Tipo | Descripción |
|-------|----------|------|-------------|
| `gameManager` | Setup | `GameManager` | Required ref al orquestador |
| `devDabloonsAmount` | Dev Tools | `int` | Amount a agregar (default 500) |
| — | — | — | — |
| `devEquipmentItem` | Equipment (DEV) | `EquipmentSO` | **S33** Item individual a agregar |
| `equipmentDatabase` | Equipment (DEV) | `EquipmentDatabaseSO` | **S33** Para catalog completo |
| — | — | — | — |
| `combatTuning` | Combat (DEV) | `CombatTuningSO` | **S95** Tuning (cooldown/material/etc); fallback CreateInstance si null |
| — | — | — | — |
| **`expeditionBridge`** | **Expedition (DEV)** | **ExpeditionBridge** | **S119 NUEVO** Ref al componente de puente (para llamar Depart()) |

## Flujo Típico (Playtesting)

### Equipo
1. **Populate inventory rápido:** Click "Add Full Equipment Catalog" → todos los items en grillas
2. **Open detail panel:** Click MM en grid
3. **Tab Equipo → click equip-card:** Abre backpack popup
4. **Drag/click items:** Equipa, desequipa, mueve en grid

### Combate (S95)
1. **Reroll Potentials:** Da a todas criaturas potenciales 1-3 aleatorios
2. **Open Combat Panel:** Abre UI combate para jugar manualmente

### Expedición (S119)
1. **Salir de expedición:** Click button → Parte a arena (ArenaSandbox)
2. **Juega ronda en arena:** Combate, recolecta minerales
3. **Retorna:** Botón "Volver a tienda" en ArenaPlanPanel → ExpeditionHandoff.ReturnToStore() → vuelve a tienda con rewards

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
- [[Index/21 - Combate v3 - Dragon RPS]]
- [[Index/24 - Puente Tienda-Arena]]
- [[GameManager]] — ref, obtiene Inventory/Registry singleton
- [[PlayerInventorySO]] — muta via API pública
- [[CreatureRegistrySO]] — **S95** muta DNA potenciales
- [[GameEvents]] — dispara InventoryChanged/RegistryChanged
- [[UIManager]] — **S95** solicita abrir panel combate

## Conexiones

**Entrada:**
- Inspector buttons (Odin, MonoBehaviour inspector)

**Salida:**
- `PlayerInventorySO.AddEquipment()`, `RemoveEquipmentAt()`, etc.
- `CreatureRegistrySO.GetAll()`, muta potenciales
- `GameEvents.InventoryChanged(inventory)`, `GameEvents.RegistryChanged(registry)`
- `UIManager.RequestPanelSet(UIPanelType.Combat, true)` **S95**
- `ExpeditionBridge.Depart()` **S119**
- `ExpeditionHandoff.GoToArena()` **S119**

## Notas

- **S33 Equipment:** Buttons para dev-test la grilla libre sin UI.
- **S95 Combat:** Button para potencial reroll y panel directo.
- **S119 Expedition:** Botón rápido para testear flujo de navegación arena/tienda.
- **combatTuning fallback:** Si no asigna SO en inspector, crea CreateInstance con defaults.
- **Safety:** Logguea warnings si refs null — no crashea.

