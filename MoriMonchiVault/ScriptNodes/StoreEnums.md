---
tags: [enum, store, economy]
---

# StoreEnums

**Ruta:** `Core/Enums/StoreEnums.cs`

**Responsabilidad:** Enumeraciones para tienda, muebles y props. Contiene: `FurnitureCategory` (3 tipos), `ItemType` (2 tipos), `WorldPropCategory` (3 categorías), `DiscountDay` (Flags, 7 días), `DiscountMonth` (Flags, 12 meses), `RestockPeriod` (3 períodos), `BuyResult` (4 resultados de compra).

**S128:** Se borra `ItemTriggerKind` (era para consumo automático, no implementado en combate S128).

## Enumeraciones

| Enum | Valores | Descripción |
|------|---------|-------------|
| `FurnitureCategory` | Decoration (0), Display (1), Functional (2) | Categoría de mueble |
| `ItemType` | Furniture (0), WorldProp (1) | Tipo de item |
| `WorldPropCategory` | Tool (0), Food (1), Medicine (2) | Categoría de world prop |
| `DiscountDay` | Flags: Mon-Sun (1-64), All | Días con descuento |
| `DiscountMonth` | Flags: Jan-Dec (1-2048), All | Meses con descuento |
| `RestockPeriod` | EarlyMonth (0), MidMonth (1), EndOfMonth (2) | Cuándo restock |
| `BuyResult` | Success, OutOfStock, InsufficientFunds, AlreadyOwned | Resultado de compra |

## Uso

- `FurnitureCategory`, `ItemType` — clasificación de SOs
- `WorldPropCategory` — categoría de world prop spawnable
- `DiscountDay`, `DiscountMonth` — Flags para rangos temporales
- `RestockPeriod` — scheduler de restock (futura)
- `BuyResult` — retornado por `StoreManager.Buy*()`, UI interpreta para mensajes

## Cambios S128

**Eliminado:** `ItemTriggerKind` (None/LowHealth/Collision/Collected) — consumo automático de items no implementado en combate desmontado.

## Historial

**S93:** Consolidación en archivo dedicado.
**S128:** ItemTriggerKind eliminado (limpieza RPS).

## Vinculado a

[[Index/04 - Store & Transactions]]

**Conexiones:** [[FurnitureDefinitionSO]], [[StoreManager]], [[StorePanelUITK]], [[ItemDatabaseSO]]

