---
tags: [script, store, data]
---

# ShopCatalogSO

**Ruta:** `Systems/Store/ShopCatalogSO.cs`

**Responsabilidad:** Catálogo unificado con descuentos por mes+weekday y restock schedule. Itera y expone 3 tipos de listings: Furniture, WorldProps (ItemDefinitionSO), y Creature Boxes (S130 NUEVO).

## Enumeraciones de Schedule

- `DiscountDay` — flags (None, Monday–Sunday, All)
- `DiscountMonth` — flags (None, January–December, All)
- `RestockPeriod` — enum (EarlyMonth 1-10, MidMonth 11-20, EndOfMonth 21+)

## Clases Internas

### FurnitureListing

```csharp
public class FurnitureListing
{
    [Required, AssetsOnly] public FurnitureDefinitionSO Furniture;
    public StoreShopData Shop;
}
```

### ItemListing

```csharp
public class ItemListing
{
    [Required, AssetsOnly] public ItemDefinitionSO Item;
    public StoreShopData Shop;
}
```

### CreatureBoxListing (S130)

```csharp
public class CreatureBoxListing
{
    [Required, AssetsOnly] public CreatureBoxSO Box;
    public StoreShopData Shop;
}
```

## Propiedades Públicas

| Propiedad | Retorna | Descripción |
|-----------|---------|-------------|
| `FurnitureListings` | `IReadOnlyList<FurnitureListing>` | Lista de muebles a vender |
| `ItemListings` | `IReadOnlyList<ItemListing>` | Lista de props a vender |
| `CreatureBoxListings` | `IReadOnlyList<CreatureBoxListing>` | Lista de cajas de criaturas (S130) |

## Métodos de Schedule

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `IsDiscountActive(DateTime now)` | `bool` | Evalúa flags DiscountDay/Month contra fecha |
| `FinalPrice(StoreShopData shop, DateTime now)` | `int` | Precio con descuento aplicado |
| `IsRestockDay(DateTime now)` | `bool` | Es día de restock según schedule |
| `NeedsRestock(DateTime now)` | `bool` | Ya hubo restock en este mes/período |
| `RestockAll(DateTime now)` | `void` | Llena stock de todas las listings (furniture + items + creature boxes) |

## Flujo Restock (S130)

```
1. IsRestockDay() → checks RestockMonths flag + RestockPeriod (día del mes)
2. NeedsRestock() → valida que no haya recargado ya en este mes/período
3. RestockAll(now):
   - Loop furnitureListings → l.Shop?.Restock()
   - Loop itemListings → l.Shop?.Restock()
   - Loop creatureBoxListings → l.Shop?.Restock()  [S130 NUEVO]
   - Actualiza lastRestockYear/Month/Period
```

## Integración S130

`CreatureBoxListings` se recarga en restock igual que furniture e items. `StoreRows.Collect(Tab.Creatures, ...)` itera este array. `StoreManager.BuyCreatureBox()` valida stock vía `shop.InStock` y consume con `shop.TryConsume()`.

## Vinculado a

[[Index/04 - Store & Transactions]]
[[Index/28 - Cimientos y camino a Game Ready]]

**Conexiones:** [[StoreManager]], [[StoreShopData]], [[StorePanelUITK]], [[StoreRows]], [[CreatureBoxSO]]

