---
tags: [script, core]
---

# DevToolsConsole.cs

**Ruta:** `Core/DevToolsConsole.cs`

**Responsabilidad:** Componente dev (MonoBehaviour) para manipular inventario en editor/testing: Add Dabloons, Reset Dabloons, Clear Furniture Owned, Clear World Props (storage + hotbar). Cada acción emite `GameEvents.InventoryChanged(inventory)` para sincronización. Ref serializada [SerializeField] a GameManager. Solo para desarrollo (no se incluye en build release).

**Vinculado a:** [[Index/09 - Dev Tools]]

**Conexiones:** [[GameManager]], [[PlayerInventorySO]], [[GameEvents]]

**Uso en escena:** Adjuntar a un GameObject con acceso a GameManager. Inspect, configura GameManager ref y usa botones.
