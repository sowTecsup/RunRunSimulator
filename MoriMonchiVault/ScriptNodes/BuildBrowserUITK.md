---
tags: [script, ui, build]
---

# BuildBrowserUITK

**Ruta:** `UI/BuildBrowserUITK.cs`

**Responsabilidad:** Panel build browser. Lista muebles poseídos para colocar. `IUINavigable`. **S93:** Usa `UiPanels.SetActiveIndex()` y `UiPanels.RootOf()`. **S130:** Solo muestra muebles en inventario (propiedad `PlayerInventorySO.HasFurniture()`), no el catálogo completo.

## Tabulación

Pestañas por `FurnitureCategory` (dinámicas desde `FurnitureDatabaseSO`). Cada tab filtra piezas por categoría **y** posesión.

## Métodos Públicos (IUINavigable)

| Método | Descripción |
|--------|-------------|
| `OnUINavigate(Vector2 dir)` | Navega: X → tabs, Y → piezas (con wrap) |
| `OnUISubmit()` | Confirma selección: `BuySelected()` |
| `OnUICancel()` | Retorna false (no intercepta) |

## Flujo RefreshPieces (S130)

```
1. Obtiene categoría activa → Categories[activeCategory]
2. Loop database.All:
   - if (def == null || def.Category != cat) continue;
   - if (!GameManager.CurrentInventory.HasFurniture(def.Id)) continue;  [S130]
   - pieces.Add(def)
3. Build UI rows para cada piece poseído
4. Select(0) si hay piezas
```

**Cambio S130:** Invierte el filtro — antes mostraba TODO el catálogo; ahora solo muebles en `PlayerInventorySO.Furniture` (posesión actualizada en `OnInventoryChanged` / `OnInventoryReloaded`).

## Suscripciones (OnEnable/OnDisable)

| Evento | Handler |
|--------|---------|
| `BuildingInputs.BrowseToggled` | `Toggle()` — open/close panel |
| `BuildModeController.OnBuildModeChanged` | `OnBuildModeChanged()` — cierra al salir de BuildMode |
| `GameEvents.OnInventoryChanged` | `OnInventoryChanged()` — refresca piezas si abierto |
| `GameEvents.OnInventoryReloaded` | `OnInventoryChanged()` — refresca piezas si abierto (S130) |

## Invariante S130

**SSOT:** Posesión vive en `PlayerInventorySO.Furniture` (Set<string> de IDs). BuildBrowser refleja ese estado exacto. Cambios vía `StoreManager.BuyFurniture()` → evento → inventario muta → UI refresca.

## Vinculado a

[[Index/05 - UI System]]
[[Index/10 - Furniture & Building]]

**Conexiones:** [[UIManager]], [[BuildModeController]], [[FurnitureDatabaseSO]], [[PlayerInventorySO]], [[GameEvents]], [[UiPanels]], [[GameManager]]

