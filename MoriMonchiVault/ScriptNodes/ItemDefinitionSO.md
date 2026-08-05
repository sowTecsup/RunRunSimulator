---
tags: [script, inventory, world-props]
---

# ItemDefinitionSO.cs

**Ruta:** `Data/Items/ItemDefinitionSO.cs`

**Responsabilidad:** Definición de world prop (objeto tangible): nombre, categoría, prefab 3D. **Solo para world props** (escoba, comida, medicina) — furniture vive en `FurnitureDefinitionSO` y es un dominio separado (distinto flujo, IDs "F#" vs "I#"). ID estampado por `ItemDatabaseSO.SyncIds()`, nunca editado aquí. Precio NO vive aquí; se define en `StoreShopData` (listing del shop).

## Campos Públicos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Id` | string (ReadOnly) | ID único "I#", estampado por ItemDatabaseSO.SyncIds() según slot del diccionario. No editable. |
| `DisplayName` | string | Nombre visible (ej. "Snack Monchi", "Broom") |
| `Category` | WorldPropCategory (enum) | Tipo: `Tool`, `Food`, `Medicine` |
| `Prefab` | GameObject (Required) | Prefab 3D spawneado al mundo. Debe llevar componente `WorldPropInstance` |

## Categorías (WorldPropCategory)

- **Tool:** objeto que habilita acciones (broom para limpiar)
- **Food:** objeto consumible (snack, hand-feed)
- **Medicine:** objeto curativo

## Prefab Requerimientos

El GameObject asignado debe:
- Llevar componente `WorldPropInstance` para marcar identidad (id "I#")
- Llevar Rigidbody (kinematic en mano, dynamic cuando suelto)
- Llevar Collider(s) para interacción

## Assets Existentes

| Id | DisplayName | Category | Prefab |
|----|-------------|----------|--------|
| I0 | Mop | Tool | ItemMop |
| I1 | Snack Monchi | Food | ItemSnack (S70) |

## Precio

**NO vive en ItemDefinitionSO.** El precio se define en `StoreShopData`, que vincula un listing del shop con un item:
```csharp
public class StoreShopData
{
    public string ItemId;  // "I#" ref
    public int Price;
    // ...
}
```
Ver `ShopCatalogSO` para catalogo de tienda.

## Diferencia con FurnitureDefinitionSO

| Aspecto | World Props (Item) | Furniture |
|--------|-------------------|-----------|
| **ID Namespace** | I# | F# |
| **Delivery** | DeliveryBox (prop suelto) o pickup en mundo | Compra directa a inventario |
| **Persistencia** | Instancias únicas en `PlayerInventorySO.worldPropsStored` (lista, dupes OK) | Ownership set en `PlayerInventorySO.furnitureOwned` (no dupes) |
| **Interacción** | Pickup/use/throw/drop vía HotbarController | Placement/move vía BuildModeController |
| **Definición** | ItemDefinitionSO | FurnitureDefinitionSO |

## Vinculado a

[[Index/06 - Player & World]]
[[Index/07 - Persistence & Identity]]

## Conexiones

[[ItemDatabaseSO]], [[HotbarController]], [[WorldPropInstance]], [[PlayerInventorySO]], [[ShopCatalogSO]], [[StoreShopData]], [[FurnitureDefinitionSO]]
