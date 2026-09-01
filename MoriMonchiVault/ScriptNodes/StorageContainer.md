---
tags: [script, furniture, storage]
---

# StorageContainer.cs

**Ruta:** `Systems/Store/StorageContainer.cs`

**Responsabilidad:** Caja de almacenamiento físico en el mundo. Componente singleton (`Instance`). Trigger zone que detecta world props caídas (ejects automático). Bridge con `GameManager.CurrentInventory`. Usa `ItemDatabaseSO.GetByID()` para resolver items.

**S93:** Usa `GameManager.CurrentInventory` (propiedad estática) en lugar de `GameManager.Instance.Inventory` directo. Mejora desacoplamiento con inventario.

## Métodos Públicos

- Eject API (no especificada, probablemente `Eject(itemId)` o similar)
- Trigger detection de WorldPropInstance

## Propiedades

- `Instance` (static) — singleton

## Ciclo de Vida

1. Componente se registra como `Instance` singleton en `Awake()`
2. Trigger zone detecta `WorldPropInstance` caída
3. Automáticamente añade al `GameManager.CurrentInventory` vía `AddWorldProp(itemId)`
4. Resuelve item vía `ItemDatabaseSO.GetByID(itemId)`

## Vinculado a

- [[Index/04 - Store & Transactions]]

**Conexiones:** [[PlayerInventorySO]], [[GameManager]], [[ItemDatabaseSO]], [[StoragePanelUITK]], [[WorldPropInstance]]

