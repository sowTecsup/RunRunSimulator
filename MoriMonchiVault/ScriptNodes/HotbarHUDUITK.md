---
tags: [script, ui]
---

# HotbarHUDUITK.md

**Ruta:** `UI/HotbarHUDUITK.cs`

**Responsabilidad:** HUD del hotbar. Overlay permanente en gameplay, no navegable. **S93:** Usa `UiPanels.RootOf()` para resolver root; toma inventario de payload de `OnInventoryReloaded` + `GameManager.CurrentInventory` en OnEnable.

**Vinculado a:** [[Index/05 - UI System]]

**Conexiones:** [[HotbarController]], [[UiPanels]], [[GameManager]]
