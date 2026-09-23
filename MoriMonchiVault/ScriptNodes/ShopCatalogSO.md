---
tags: [script, store, data]
---

# ShopCatalogSO

**Ruta:** `Systems/Store/ShopCatalogSO.cs`

**Responsabilidad:** Catálogo unificado con descuentos y restock schedule por día de juego. Expone 3 tipos de listings: Furniture, WorldProps (ItemDefinitionSO), y Creature Boxes. S131: API cambió de DateTime → int day (GameClock.Instance.Day). Métodos: IsDiscountActive(int day), FinalPrice(StoreShopData, int day), NeedsRestock(int day), RestockAll(int day).

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
| `FurnitureListings` | `IReadOnlyList<FurnitureListing>` | Lista de muebles |
| `ItemListings` | `IReadOnlyList<ItemListing>` | Lista de props |
| `CreatureBoxListings` | `IReadOnlyList<CreatureBoxListing>` | Lista de cajas de criaturas |
| `RestockEveryDays` | int | Intervalo de restock (default 3) |
| `DiscountEveryDays` | int | Intervalo de descuento en días (default 7; 0=nunca) |

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `IsDiscountActive(int day)` | `bool` | **(S131)** `day % DiscountEveryDays == 0` (si DiscountEveryDays > 0) |
| `FinalPrice(StoreShopData shop, int day)` | `int` | Precio con descuento aplicado según IsDiscountActive(day) |
| `NeedsRestock(int day)` | `bool` | `lastRestockDay <= 0 || day - lastRestockDay >= RestockEveryDays` |
| `RestockAll(int day)` | `void` | Recarga stock de todas listings; actualiza lastRestockDay |

## Cambios S131

**Borrados:**
- Enum `DiscountDay` (flags Monday-Sunday)
- Enum `DiscountMonth` (flags January-December)
- Enum `RestockPeriod` (EarlyMonth/MidMonth/EndOfMonth)
- Métodos que usaban DateTime

**Nuevos campos:**
```csharp
[Min(1)] public int RestockEveryDays = 3;     // Intervalo días de juego
[Min(0)] public int DiscountEveryDays = 7;    // Intervalo días de juego (0=nunca)
```

**Nueva API:**
- `IsDiscountActive(int day)` — módulo aritmético simple
- `FinalPrice(StoreShopData shop, int day)` — delega `shop.FinalPrice(IsDiscountActive(day))`
- `NeedsRestock(int day)` — compara `lastRestockDay` (int)
- `RestockAll(int day)` — simple loop, guarda `lastRestockDay = day`

**Dev button:**
```csharp
[Button("Force Restock All (DEV)")]
private void DevForceRestock()
{
    int day = GameClock.Instance != null ? GameClock.Instance.Day : 1;
    RestockAll(day);
    Debug.Log("[ShopCatalog] Force restock fired");
}
```

## Flujo Restock (S131)

1. `StoreManager.OnDayStarted(int day)` → `NeedsRestock(day)?`
2. Si true: `RestockAll(day)` → recarga stock de todas listings (furniture, items, boxes)

## Integración S130 + S131

`CreatureBoxListings` se recarga en restock igual que furniture e items. `StoreRows.Collect(Tab.Creatures)` itera este array. `StoreManager.BuyCreatureBox()` valida vía `shop.InStock`.

## Vinculado a

- [[Index/04 - Store & Transactions]]
- [[Index/28 - Cimientos y camino a Game Ready]]
- [[Index/09 - Active Context]]

## Conexiones

**Datos:**
- [[StoreShopData]] — consultas de precio/stock
- [[FurnitureDefinitionSO]], [[ItemDefinitionSO]], [[CreatureBoxSO]]

**Sistemas:**
- [[StoreManager]] — llama IsDiscountActive, FinalPrice, NeedsRestock, RestockAll
- [[GameClock]] — proporciona día actual
- [[GameEvents]] — StoreManager escucha OnDayStarted

## Notas (S131 HC-4)

- **Simplificación:** DateTime → int day (calendar no existe; solo días de juego).
- **Módulo:** IsDiscountActive usa `day % DiscountEveryDays == 0` (repetitivo cada N días).
- **Restock tracking:** lastRestockDay (int, persistido en SerializedScriptableObject).
- **Dev button:** lee GameClock.Instance.Day con fallback a 1.
