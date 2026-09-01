---
tags: [script, inventory, world-props]
---

# ItemDefinitionSO.cs

**Ruta:** `Data/Items/ItemDefinitionSO.cs`

**Responsabilidad:** Definición de world prop (objeto tangible): ID, nombre, categoría, prefab 3D, **S75:** trigger automático (cuándo se activa). ID estampado por `ItemDatabaseSO.SyncIds()`, nunca editado aquí. Precio NO vive aquí; se define en `StoreShopData`.

## Campos Públicos

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `Id` | string (ReadOnly) | ID único "I#", estampado por ItemDatabaseSO.SyncIds(). |
| `DisplayName` | string | Nombre visible |
| `Category` | WorldPropCategory | Tipo: Tool, Food, Medicine |
| `Prefab` | GameObject | Prefab 3D spawneado. Debe llevar `WorldPropInstance` |
| `Trigger` | ItemTriggerKind | **S75** Cuándo se activa: None, LowHealth, Collision, Collected |

## Categorías (WorldPropCategory)

- **Tool:** objeto que habilita acciones (broom)
- **Food:** objeto consumible (snack)
- **Medicine:** objeto curativo

## Cambios en S75

- **NUEVO campo:** `Trigger` (ItemTriggerKind enum)
- **ItemTriggerKind valores:**
  - `None = 0` — Sin comportamiento automático
  - `LowHealth = 1` — Se activa cuando portador bajo de HP
  - `Collision = 2` — Se activa al impactar
  - `Collected = 3` — Se activa al ser recogido

## Vinculado a

- [[Index/06 - Player & World]]

**Conexiones:** [[ItemDatabaseSO]], [[WorldPropInstance]], [[PlayerInventorySO]], [[StoreEnums]]
