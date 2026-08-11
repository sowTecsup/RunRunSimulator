---
tags: [script, core, singleton]
---

# GameManager.cs

**Ruta:** `Core/GameManager.cs`

**Responsabilidad:** Ciclo de vida del juego. Singleton que centraliza acceso a assets (databases, registries, configs). Único orquestador de persistencia: escucha `GameEvents.RegistryChanged`, `FurnitureChanged`, `InventoryChanged` y ejecuta persistencia local/cloud. Asigna referencias a CreatureDatabaseSO, FurnitureRegistrySO, PlayerInventorySO, **S75:** CutieMarkDatabaseSO. **S65:** `FlushToCloud()` también guarda el historial social. **S69:** `MintRandomCreature()` asigna `Sociability` y `Boldness`.

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `PushToCloud()` | Fire-and-forget async push vía `CloudSyncService.PushAsync()` |
| `FlushToCloud()` | Save local (creatures + social graph) + push cloud |
| `FlushForSceneChange()` | Save local + push cloud ANTES de cambiar escena |
| `MintRandomCreature()` | Genera random creature vía `GenerateRandom()`, asigna género, elemento, rol, stats, diales, nombre, registra |

## Cambios en S75

- **NUEVO getter:** `CutieMarkDatabase` (referencia al SO de marcas distintivas)
- Sin cambios de lógica principal (persiste, procesa eventos, etc.)

## Getters de Referencias

- `Registry` — CreatureRegistrySO
- `CreatureDatabase` — CreatureDatabaseSO (con Horns, Backs, Wings, Faces)
- `FurnitureRegistry` — FurnitureRegistrySO
- `PlayerInventory` — PlayerInventorySO
- `CutieMarkDatabase` — CutieMarkDatabaseSO
- `FurTypeDatabase` — FurTypeDatabaseSO

## Vinculado a

- [[Index/07 - Persistence & Identity]]

**Conexiones:** [[CreatureRegistrySO]], [[CreatureDatabaseSO]], [[FurnitureRegistrySO]], [[PlayerInventorySO]], [[CutieMarkDatabaseSO]], [[CloudSyncService]], [[CreatureGenerator]], [[GameEvents]]
