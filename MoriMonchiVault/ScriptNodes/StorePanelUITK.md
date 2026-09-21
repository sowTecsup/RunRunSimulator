---
tags: [script, ui, store]
---

# StorePanelUITK

**Ruta:** `UI/StorePanelUITK.cs`

**Responsabilidad:** Panel de tienda. Catálogo, precios, descuentos, compra. `IUINavigable`. Valida saldo via [[Wallet]], no mutaciones directas.

**S128:** Usa `Wallet.Balance()` para verificar saldo; `Wallet.TrySpend()` para cobrar (puerta única).

## Métodos/Flujo

- Mostrar catálogo (filtros FurnitureCategory)
- Calcular precio final (ShopCatalogSO, descuentos)
- Validar saldo: `Wallet.Balance(Currency)` — no directo a SO
- Ejecutar compra: `StoreManager.BuyFurniture()` / `BuyWorldProp()`

## Integración S128

- No acceso directo a monedas
- Todo vía [[Wallet]] para registro y eventos automáticos
- Saldo leído: `Wallet.Balance(Currency.Dabloons)` y `Wallet.Balance(Currency.Minerita)` si aplica

## Vinculado a

[[Index/04 - Store & Transactions]]

**Conexiones:** [[UIManager]], [[StoreManager]], [[Wallet]], [[ShopCatalogSO]], [[UiPanels]]

