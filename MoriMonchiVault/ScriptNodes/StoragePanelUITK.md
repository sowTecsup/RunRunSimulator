---
tags: [script, ui]
---

# StoragePanelUITK.cs

**Ruta:** `UI/StoragePanelUITK.cs`

**Responsabilidad:** Panel de almacenamiento. Depositar/retirar items. `IUINavigable`. **S93:** Toma inventario de payload de `OnInventoryReloaded` + `GameManager.CurrentInventory` en OnEnable; usa `UiPanels.ClampSelection()` y `UiPanels.SetActiveIndex()`.

**Vinculado a:** [[Index/05 - UI System]]

**Conexiones:** [[UIManager]], [[StorageContainer]], [[UiPanels]]
