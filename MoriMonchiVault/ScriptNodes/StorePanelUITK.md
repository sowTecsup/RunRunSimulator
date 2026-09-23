---
tags: [script, ui, store]
---

# StorePanelUITK

**Ruta:** `UI/StorePanelUITK.cs`

**Responsabilidad:** Panel de tienda (4 pestañas: Furniture/WorldProps/Consumables/Creatures). S131: Precio por día de juego (descuentos dinámicos via `ShopCatalogSO.IsDiscountActive(day)` + `FinalPrice(shop, day)`). Reemplazó `GameManager.Now` (UTC) → `GameClock.Instance.Day` (día de juego).

## Cambio S131

**Cálculo de precio dinámico:**
```csharp
int today = GameClock.Instance?.Day ?? 1;
int price = ShopCatalogSO.Instance.FinalPrice(shop, today);
priceLabel.text = price.ToString();
```

**Descuentos módulo:**
- `IsDiscountActive(day)` → `day % DiscountEveryDays == 0`
- `FinalPrice(shop, day)` → aplica descuento si activo

## Integración S131
- Precio recalculado cada frame/refresh
- Descuentos por día de juego (no UTC)

## Conexiones (S131)
- [[GameClock]] — proporciona Day
- [[ShopCatalogSO]] — cálculo de precio

## Notas (S131)
- Sin GameManager.Now (UTC).
- Descuentos dinámicos cada N días.
