---
tags: [script, store, transactions]
---

# StoreManager.cs

**Ruta:** `Systems/Store/StoreManager.cs`

**Responsabilidad:** Lógica de transacciones de compra. Valida fondos via `GameManager.CurrentInventory`, chequea stock/ownership (ShopCatalogSO), modifica inventario, dispara eventos, crea DeliveryBox. Usa `GameManager.Now` para timestamps.

**S93:** Usa `GameManager.CurrentInventory` (propiedad estática) y `GameManager.Now` en lugar de acceso directo a Instance. Mejora desacoplamiento.

## Métodos Principales

- `BuyItem(itemId, catalogEntry)` — Valida + mutates inventory + crea delivery
- Stock tracking — vía ShopCatalogSO

## Referencias

- `GameManager.CurrentInventory` — access a inventario (estático)
- `GameManager.Now` — timestamp actual con offset servidor
- `ShopCatalogSO` — catálogo y restock logic

## Vinculado a

- [[Index/04 - Store & Transactions]]

**Conexiones:** [[GameManager]], [[PlayerInventorySO]], [[ShopCatalogSO]], [[StoreShopData]], [[DeliveryBox]], [[StorePanelUITK]]

