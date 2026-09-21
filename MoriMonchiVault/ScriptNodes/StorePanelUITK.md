---
tags: [script, ui, store]
---

# StorePanelUITK

**Ruta:** `UI/StorePanelUITK.cs`

**Responsabilidad:** Panel de tienda. Expone 4 pestañas (Furniture, WorldProps, Consumables, Creatures). Catálogo, precios, descuentos, compra. `IUINavigable`. Valida saldo vía [[Wallet]], no mutaciones directas. **S128:** Usa `Wallet.Balance()` para verificar saldo; `Wallet.TrySpend()` para cobrar (puerta única). **S130:** 317 líneas, integra cajas de criaturas en tab 4, usa `StoreRows` como abstracción agnóstica para armar rows de cualquier tipo.

## Tabulación (S130)

```csharp
enum StoreRows.Tab { Furniture, WorldProps, Consumables, Creatures }
```

Dinámicas desde `Enum.GetValues()`. Cada tab repurposa `StoreRows.Collect()` para armar filas.

## Métodos Públicos (IUINavigable)

| Método | Descripción |
|--------|-------------|
| `OnUINavigate(Vector2 dir)` | Navega: X → tabs, Y → rows (con wrap) |
| `OnUISubmit()` | Ejecuta compra en row seleccionado |
| `OnUICancel()` | Retorna false (no intercepta) |

## Flujo Rebuild (S130)

```
1. StoreRows.Collect(activeTab, catalog, store, rows)
   - Si Creatures: itera catalog.CreatureBoxListings
   - Si Furniture: itera furniture listings
   - Si WorldProps/Consumables: itera items con filtro de categoría
2. Limpia rowEls, crea nuevos VisualElement por row
3. BuildRow(row):
   - Nombre, precio (con descuento si aplica), stock
   - Button "Buy" → Purchase(row) → row.Buy.Invoke()
4. Muestra emptyLabel si no hay rows
5. Select primer row si hay
```

## Flujo Purchase (S130)

```
1. row.Buy() retorna BuyResult enum
2. Según resultado:
   - Success → Rebuild() (refresca stock y lista)
   - OutOfStock → ShowNotify("out_of_stock")
   - InsufficientFunds → ShowNotify("insufficient_funds")
   - AlreadyOwned → ShowNotify("already_owned")
3. ShowNotify() dispara Coroutine que oculta en 2.5 s
```

## Suscripciones (OnEnable/OnDisable)

| Evento | Handler |
|--------|---------|
| `UIManager.OnPanelToggleRequested` | `OnPanelToggle()` — reset/rebuild si es panel Store |
| `UIManager.OnPanelSetRequested` | `OnPanelSet()` — rebuild si show=true |
| `GameEvents.OnInventoryChanged` | `OnInventoryChanged()` — refresca saldo |
| `GameEvents.OnInventoryReloaded` | `OnInventoryChanged()` — refresca saldo |

## Balance Label (S128+)

```csharp
balanceLabel.text = inv != null 
    ? Loc.Tr("ui.store.balance", inv.Balance(Currency.Dabloons)) 
    : "";
```

Actualizado en `RefreshBalance()` vía eventos de inventario.

## Interfaz de Precio y Stock

- **Descuento:** `IsDiscountActive(now)` en ShopCatalogSO → BuildPrice muestra anterior + tachado
- **Stock:** Label de color según `CurrentStock <= 0` (vacío) o `<= 2` (bajo)
- **Saldo:** Label de divisas `ui.store.balance`

## Integración S130

**Criaturas tab (S130 NUEVO):**
- `StoreRows.Tab.Creatures` abre listado de `ShopCatalogSO.CreatureBoxListings`
- Cada fila: nombre, precio, stock, botón compra
- `row.Buy()` → `StoreManager.BuyCreatureBox(box, shop)` → `DeliveryBox` con caja → Interact abre

**Invariante:** `StoreRows` desacopla lógica de data (rows) de UI (layout). StorePanelUITK solo renderiza; `StoreManager` valida y cobra.

## Métodos/Flujo (S128+)

- Mostrar catálogo por tab (filtros FurnitureCategory, Item.Category)
- Calcular precio final (ShopCatalogSO, descuentos)
- Validar saldo: `Wallet.Balance(Currency)` — no directo a SO
- Ejecutar compra: `StoreManager.BuyFurniture()` / `BuyWorldProp()` / `BuyCreatureBox()` (S130)

## Integración S128

- No acceso directo a monedas
- Todo vía [[Wallet]] para registro y eventos automáticos
- Saldo leído: `Wallet.Balance(Currency.Dabloons)` y `Wallet.Balance(Currency.Minerita)` si aplica

## Vinculado a

[[Index/04 - Store & Transactions]]
[[Index/05 - UI System]]
[[Index/28 - Cimientos y camino a Game Ready]]

**Conexiones:** [[UIManager]], [[StoreManager]], [[Wallet]], [[ShopCatalogSO]], [[UiPanels]], [[StoreRows]], [[CreatureBoxSO]]

