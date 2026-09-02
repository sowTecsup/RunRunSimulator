---
tags: [script, dev-tools, equipment, combat]
---

# DevToolsConsole.cs

**Ruta:** `Core/DevToolsConsole.cs`

**Responsabilidad:** Componente dev (MonoBehaviour) para manipular inventario y combate en editor/testing sin interfaz de juego (playtesting rápido). Buttons Odin (BoxGroups): Dabloons, Furniture, World Props, Equipment (S33), **Combat (S95)**. Cada acción muta `gameManager.Inventory` o `gameManager.Registry` vía su API pública, emite `GameEvents.InventoryChanged()` / `GameEvents.RegistryChanged()`. Refs serializadas [SerializeField]. Solo para desarrollo (no incluir en builds release).

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
| `Simulate Combat (DEV)` | Simula 5 combates secuenciales | `combatTuning`, seed+RNG | Crea sesiones, juega hasta fin (Play(0) repeat), logguea resultado/material/cooldown |

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

## Flujo Típico (Playtesting)

### Equipo
1. **Populate inventory rápido:** Click "Add Full Equipment Catalog" → todos los items en grillas
2. **Open detail panel:** Click MM en grid
3. **Tab Equipo → click equip-card:** Abre backpack popup
4. **Drag/click items:** Equipa, desequipa, mueve en grid

### Combate (S95)
1. **Reroll Potentials:** Da a todas criaturas potenciales 1-3 aleatorios
2. **Open Combat Panel:** Abre UI combate para jugar manualmente
3. **Simulate Combat:** Corre 5 combates automáticos (Play(0)=elección aleatoria cada turno), logguea resultados

## Mensajes Debug

Cada button logguea a console:
```
[DevToolsConsole] +500 Dabloons → total: 1234
[DevToolsConsole] Equipment owned list cleared.
[DevToolsConsole] +1 Sword (EQ_SWORD_01)
[DevToolsConsole] Added 23 equipment items from catalog.
[DevToolsConsole] Combat panel requested.
[DevToolsConsole] Rerolled potentials on 12 creatures.
[DevToolsConsole] Combat 1: Monchi1 (budget 6) vs Salvaje Monchi2 (budget 7) → WIN 2-1 in 3 rounds | material=36 cooldownUntil=0
```

## Vinculado a

- [[Index/09 - Dev Tools]]
- [[Index/21 - Combate v3 - Dragon RPS]]
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
- `DragonRpsService.Seed()`, `Start()`, `Resolve()` **S95**

## Notas

- **S33 Equipment:** Buttons para dev-test la grilla libre sin UI.
- **S95 Combat:** 3 nuevos buttons para combate: panel directo, potencial reroll, simulación automática.
- **combatTuning fallback:** Si no asigna SO en inspector, crea CreateInstance con defaults (20min cooldown / 3 material).
- **Simulate Combat:** Play(0) = elección aleatoria (índice 0 siempre); ideal para testear balanceo 5-combates rápidos.
- **Safety:** Logguea warnings si refs null — no crashea.

