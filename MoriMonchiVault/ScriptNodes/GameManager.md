---
tags: [script, core, singleton]
---

# GameManager.cs

**Ruta:** `Core/GameManager.cs`

**Responsabilidad:** Ciclo de vida del juego. Singleton que centraliza acceso a assets (databases, registries, configs). Único orquestador de persistencia: escucha `GameEvents.RegistryChanged`, `FurnitureChanged`, `InventoryChanged` e invoca persistencia local/cloud. Propiedades estáticas: `CurrentInventory` (PlayerInventorySO), `Now` (DateTime con offset servidor). `OnDestroy()` limpia Instance. `MintRandomCreature()` genera random creature y la registra.

**S93:** Agregados getters estáticos `CurrentInventory` y `Now`. Eliminado `FlushForSceneChange()`. Eliminada referencia a `CutieMarkDatabase`. `OnDestroy()` limpia Instance de forma segura. **S124:** Sin cambios estructurales; continúa orquestando persistencia de expediciones vía GameEvents.

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
4. Expedición (S124) → ArenaRunDirector.Retreat() → ExpeditionHandoff.ReturnToStore() → carga tienda → ExpeditionBridge aplica resultado (inventario + energía)
5. `OnApplicationQuit()` / `OnApplicationPause()` → `CollectLooseWorldProps()` + `FlushToCloud()`
6. `OnDestroy()` → Limpia `Instance` si es el mismo

## Eventos Orquestados (sin lógica de gameplay)

GameManager **escucha** pero no **dispara**. Los eventos que monitorea:
- `GameEvents.OnRegistryChanged` — gameplay alteró registry
- `GameEvents.OnFurnitureChanged` — furniture mutó
- `GameEvents.OnInventoryChanged` — inventario cambió

## Invariantes S93+S124

- Singleton: Awake crea Instance, OnDestroy limpia si es el mismo
- No disparador de eventos: solo consumidor de persistencia
- Acceso centralizado: todas las referencias públicas por getter, no direct SerializeField exposure
- Persistencia order: Local (SaveDatabase) → Cloud (PushAsync) — nunca inversión

## Vinculado a

[[Index/07 - Persistence & Identity]]

**Conexiones:** [[CreatureRegistrySO]], [[CreatureDatabaseSO]], [[FurnitureRegistrySO]], [[PlayerInventorySO]], [[CloudSyncService]], [[CreatureGenerator]], [[GameEvents]], [[SaveSystem]]
