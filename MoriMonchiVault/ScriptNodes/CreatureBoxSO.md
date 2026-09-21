---
tags: [script, store, data]
---

# CreatureBoxSO

**Ruta:** `Data/Store/CreatureBoxSO.cs`

**Responsabilidad:** Definición de caja de MoriMonchis para venta. Especifica cantidad y metadatos display (Id, nombre, descripción).

## Campos Públicos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Id` | `string` | Identificador único de la caja |
| `DisplayName` | `string` | Nombre mostrado en UI |
| `Description` | `string` | Descripción larga (text area) |
| `Count` | `int` | Cantidad de MoriMonchis que contiene (mín. 1) |

## Uso

- **StoreManager.BuyCreatureBox()** → instancia `DeliveryBox`, llama `Configure(CreatureBoxSO)`
- **StoreRows.Collect()** → itera `ShopCatalogSO.CreatureBoxListings` para armar rows de tienda
- **DeliveryBox.Interact()** → abre caja, mintea `Count` criaturas, dispara único `OnRegistryChanged`

## Integración S130

Parte de la expansión C5 (catálogo de cajas de criaturas). Las cajas se venden como items entregables (`DeliveryBox`), similar a props del mundo (`ItemDefinitionSO`), pero en lugar de instanciar un prefab, mienten criaturas vía `GameManager.MintCreature()` y las lanzan por cañón.

## Vinculado a

[[Index/04 - Store & Transactions]]
[[Index/28 - Cimientos y camino a Game Ready]]

**Conexiones:** [[StoreManager]], [[DeliveryBox]], [[ShopCatalogSO]], [[StoreRows]]

