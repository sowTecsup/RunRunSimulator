---
tags: [script, core, singleton]
---

# GameManager.cs

**Ruta:** `Core/GameManager.cs`

**Responsabilidad:** Ciclo de vida del juego. Singleton que centraliza acceso a assets (databases, registries, configs). Único orquestador de persistencia: escucha `GameEvents.RegistryChanged`, `FurnitureChanged`, `InventoryChanged` y ejecuta persistencia local/cloud. Propiedades estáticas: `CurrentInventory` (PlayerInventorySO), `Now` (DateTime con offset servidor). `OnDestroy()` limpia Instance. `MintRandomCreature()` genera random creature y la registra.

**S93:** Agregados getters estáticos `CurrentInventory` y `Now`. Eliminado `FlushForSceneChange()`. Eliminada referencia a `CutieMarkDatabase`. `OnDestroy()` limpia Instance de forma segura.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `PushToCloud()` | Fire-and-forget async push vía `CloudSyncService.PushAsync()` |
| `FlushToCloud()` | Save local (creatures + social graph) + push cloud |
| `MintRandomCreature()` | Genera random creature vía `GenerateRandom()`, asigna género/elemento/rol/stats/diales/nombre, registra |

## Propiedades Estáticas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Instance` | `GameManager` | Singleton; null si destroyed |
| `CurrentInventory` | `PlayerInventorySO` | Acceso rápido: `GameManager.CurrentInventory` vs `GameManager.Instance.Inventory` |
| `Now` | `DateTime` | Hora con offset servidor (CloudSyncService.ServerOffset) |

## Getters de Referencias

- `Registry` — CreatureRegistrySO
- `Database` — CreatureDatabaseSO (con Horns, Backs, Wings, Faces)
- `FurnitureRegistry` — FurnitureRegistrySO
- `Inventory` — PlayerInventorySO
- `FurTypeDatabase` — FurTypeDatabaseSO
- `EquipmentDatabase` — EquipmentDatabaseSO
- `RarityOddsTable` — RarityOddsTableSO
- `MonchiVisualBank` — MonchiVisualBankSO
- `RoleWorldProfiles` — RoleWorldProfileSO

## Ciclo de Vida

1. `Awake()` → `Instance = this`
2. `OnEnable()` → Suscribe a eventos (OnRegistryChanged, OnFurnitureChanged, OnInventoryChanged)
3. Gameplay → eventos → `Persist()` (SaveDatabase + PushToCloud)
4. `OnApplicationQuit()` / `OnApplicationPause()` → `CollectLooseWorldProps()` + `FlushToCloud()`
5. `OnDestroy()` → Limpia `Instance` si es el mismo

## Vinculado a

- [[Index/07 - Persistence & Identity]]

**Conexiones:** [[CreatureRegistrySO]], [[CreatureDatabaseSO]], [[FurnitureRegistrySO]], [[PlayerInventorySO]], [[CloudSyncService]], [[CreatureGenerator]], [[GameEvents]], [[SaveSystem]]

