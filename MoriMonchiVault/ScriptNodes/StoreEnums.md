---
tags: [enum, store, economy]
---

# StoreEnums

**Ruta:** `Core/Enums/StoreEnums.cs`

**Responsabilidad:** Enumeraciones para tienda, muebles y props. Contiene: `FurnitureCategory` (3 tipos), `ItemType` (2 tipos), `WorldPropCategory` (3 categorías), `BuyResult` (4 resultados), `Currency` (2 monedas), `StoreItemTypeFilter` (flags de filtro). S131: Borrados `DiscountDay`, `DiscountMonth`, `RestockPeriod` (reempl azados por int day modular).

## Enumeraciones Vigentes

| Enum | Valores | Descripción |
|------|---------|-------------|
| `FurnitureCategory` | Decoration, Display, Functional | Categoría de mueble |
| `ItemType` | Furniture, WorldProp | Tipo de item |
| `WorldPropCategory` | Tool, Food, Medicine | Categoría de prop spawnable |
| `BuyResult` | Success, OutOfStock, InsufficientFunds, AlreadyOwned | Resultado de compra |
| `Currency` | Dabloons (0), Minerita (1) | Moneda (S127) |
| `StoreItemTypeFilter` | Flags: Furniture (1), WorldProp (2), None (0) | Filtro de búsqueda |

## Borrados S131

| Enum | Razón |
|------|-------|
| `DiscountDay` | Flags Mon-Sun → reempl. `day % DiscountEveryDays == 0` |
| `DiscountMonth` | Flags Jan-Dec → no hay calendario |
| `RestockPeriod` | EarlyMonth/MidMonth/EndOfMonth → reempl. `day % RestockEveryDays` |

**Cambios de contrato:**
- `ShopCatalogSO.IsDiscountActive(int day)` — módulo aritmético
- `ShopCatalogSO.NeedsRestock(int day)` — compara días absolutos

## Uso

- `FurnitureCategory`, `ItemType` — clasificación de SOs
- `WorldPropCategory` — categorización en world props
- `Currency` — moneda de transacción (S127+)
- `BuyResult` — retornado por `StoreManager.Buy*()`, UI interpreta
- `StoreItemTypeFilter` — Flags para filtro de tienda

## Historial

**S93:** Consolidación en archivo dedicado.
**S128:** ItemTriggerKind eliminado.
**S131:** DiscountDay/Month/RestockPeriod eliminados (simplificación a calendario de juego).

## Vinculado a

- [[Index/04 - Store & Transactions]]
- [[Index/09 - Active Context]]

## Conexiones

**Data:**
- [[FurnitureDefinitionSO]], [[ItemDatabaseSO]], [[ItemDefinitionSO]]

**Sistemas:**
- [[ShopCatalogSO]] — consume Currency, BuyResult, Filter
- [[StoreManager]] — retorna BuyResult
- [[StorePanelUITK]] — filtra con StoreItemTypeFilter

## Notas (S131 HC-4)

- **Simplificación:** Calendario real (mes/semana) → días de juego (0, 1, 2, ...).
- **Módulo:** `day % N == 0` es patrón para repetición de eventos cada N días.
- **Currency:** Dabloons (abundante) vs Minerita (escaso) para economía dual S127+.
