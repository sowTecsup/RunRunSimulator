---
tags: [enum, store, furniture]
---

# StoreEnums.cs

**Ruta:** `Core/Enums/StoreEnums.cs`

**Responsabilidad:** Enumeraciones para tienda, muebles y props. Contiene: `FurnitureCategory` (3 tipos: Decoration/Display/Functional), `ItemType` (2 tipos de item: Furniture/WorldProp), `WorldPropCategory` (3 categorías de world props: Tool/Food/Medicine), `DiscountDay` (Flags, 7 días de la semana + All), `DiscountMonth` (Flags, 12 meses + All), `RestockPeriod` (3 períodos: EarlyMonth/MidMonth/EndOfMonth), `BuyResult` (4 resultados de compra: Success/OutOfStock/InsufficientFunds/AlreadyOwned), `StoreItemTypeFilter` (Flags: Furniture/WorldProp), `ItemTriggerKind` (4 tipos de trigger para items: None/LowHealth/Collision/Collected).

**S93:** Consolidación de enums de store en archivo dedicado.

## Enumeraciones

| Enum | Valores | Descripción |
|------|---------|-------------|
| `FurnitureCategory` | Decoration (0), Display (1), Functional (2) | Categoría de mueble |
| `ItemType` | Furniture (0), WorldProp (1) | Tipo de item (mueble vs prop del mundo) |
| `WorldPropCategory` | Tool (0), Food (1), Medicine (2) | Categoría de world prop |
| `DiscountDay` | Flags: Monday-Sunday (1-64), All | Días con descuento |
| `DiscountMonth` | Flags: January-December (1-2048), All | Meses con descuento |
| `RestockPeriod` | EarlyMonth (0), MidMonth (1), EndOfMonth (2) | Cuándo hacer restock |
| `BuyResult` | Success, OutOfStock, InsufficientFunds, AlreadyOwned | Resultado de transacción de compra |
| `StoreItemTypeFilter` | Flags: Furniture, WorldProp | Filtro de búsqueda en tienda |
| `ItemTriggerKind` | None, LowHealth (1), Collision (2), Collected (3) | Cuándo activar un item (consumo automático) |

## Uso

- `FurnitureCategory`, `ItemType` — clasificación de `FurnitureDefinitionSO`, `ItemDatabaseSO`
- `WorldPropCategory` — categoría de world prop spawnable
- `DiscountDay`, `DiscountMonth` — Flags para especificar cuándo aplica descuento (ej. `DiscountDay.Monday | DiscountDay.Friday`)
- `RestockPeriod` — scheduler de restock (scheduler service futura)
- `BuyResult` — retorna `CashRegister.Buy()`, UI interpreta para mostrar mensajes
- `StoreItemTypeFilter` — UI de catálogo filtra qué mostrar (Flags permite Furniture | WorldProp)
- `ItemTriggerKind` — cuándo consumir un item automáticamente (ej. Medicine al bajar salud)

## Vinculado a

- [[Index/04 - Store & Transactions]]
- [[FurnitureDefinitionSO]] — contiene FurnitureCategory
- [[CashRegister]] — retorna BuyResult
- [[StoragePanelUITK]] — usa StoreItemTypeFilter para filtrado

**Conexiones:** [[FurnitureDefinitionSO]], [[CashRegister]], [[StoragePanelUITK]], [[WorldPropInstance]], [[ItemDatabaseSO]]

