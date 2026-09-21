---
tags: [script, ui, store]
---

# StoreRows

**Ruta:** `UI/StoreRows.cs`

**Responsabilidad:** Utilidad estática que convierte catálogo en rows de UI. Abstracción de datos independiente de presentación (Rows viven como structs sin contexto visual). 

## Enumeraciones

### Tab

Pestañas de la tienda.

```csharp
enum Tab { Furniture, WorldProps, Consumables, Creatures }
```

## Structs Públicos

### Row

```csharp
public struct Row
{
    public string          Name;        // Display name del item
    public StoreShopData   Shop;        // Stock, precio, restock
    public Func<BuyResult> Buy;         // Lambda que dispara compra
}
```

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `TabLabel(Tab tab)` | `string` | Etiqueta localizada de la pestaña (`ui.store.tab.*`) |
| `Collect(Tab tab, ShopCatalogSO catalog, StoreManager store, List<Row> rows)` | `void` | Populate rows list con items de la tab activa del catálogo |

## Lógica Collect

**Furniture tab:**
- Itera `catalog.FurnitureListings`
- Captura `listing.Furniture` y `listing.Shop`
- Row.Buy → `store.BuyFurniture(def, shop)`

**Creatures tab (S130 NUEVO):**
- Itera `catalog.CreatureBoxListings`
- Captura `listing.Box` y `listing.Shop`
- Row.Buy → `store.BuyCreatureBox(box, shop)`

**WorldProps / Consumables tabs:**
- Itera `catalog.ItemListings`
- Filtra por categoría (`MatchesItemTab`)
- Row.Buy → `store.BuyWorldProp(def, shop)`

## Integración S130

`StorePanelUITK` llama `StoreRows.Collect()` cada vez que activa una pestaña, llenando la lista de filas de UI. El struct `Row` es agnóstico a presentación; la UI solo consume `Name`, `Shop` y ejecuta `Buy()` en click.

## Vinculado a

[[Index/04 - Store & Transactions]]

**Conexiones:** [[StorePanelUITK]], [[StoreManager]], [[ShopCatalogSO]], [[StoreShopData]]

