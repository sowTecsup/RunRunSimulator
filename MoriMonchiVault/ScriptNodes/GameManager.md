---
tags: [script, core]
---

# GameManager.cs

**Ruta:** `Core/GameManager.cs`

**Responsabilidad:** Ciclo de vida del juego. Singleton que centraliza acceso a assets (database, registries, configs). Único orquestador de persistencia: escucha `GameEvents.OnRegistryChanged`, `OnFurnitureChanged`, `OnInventoryChanged` y ejecuta persistencia local/cloud. En AppQuit/AppPause, flush a cloud. **S34:** Nuevo método `FlushForSceneChange()` para flush antes de cambiar de escena sin perder estado. `MintRandomCreature()` genera partes/color/FurType, asigna personalidad y stats base, y registra. Dev tooling para inventario/genética vive en `GeneticsLabPreview`, `DevToolsConsole`. Getters actuales: `Registry`, `FurnitureRegistry`, `Inventory`, `Database`, `RarityOddsTable`, `PersonalityProfiles`, `PartVisualBank`, `FurTypeDatabase`, `EquipmentDatabase`.

**Vinculado a:** [[Index/07 - Persistence & Identity]]

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `PushToCloud()` | Fire-and-forget async push vía `CloudSyncService.PushAsync()` |
| `FlushToCloud()` | Save local + push cloud; usado en `OnApplicationQuit/Pause` |
| `FlushForSceneChange()` | **S34** Save local + push cloud ANTES de cambiar escena; patron identico a OnApplicationQuit |
| `MintRandomCreature()` | Genera random creature, asigna stats, personalidad, nombre, registra |

## Cambios S34 — FlushForSceneChange

Nuevo método publico para flush defensivo antes de `SceneManager.LoadScene()`:

```csharp
public void FlushForSceneChange()
{
    CollectLooseWorldProps();
    FlushToCloud();
}
```

**Uso:** `CombatReplayRequest.Request()` llama esto antes de `SceneManager.LoadScene(CombatSceneName)` para asegurar que el estado está sincronizado con cloud y disco antes de entrar a la escena de combate.

**Patrón:** Identico a `OnApplicationQuit()` — recoge props sueltos y hace push. La escena de combate no necesita data de gameplay (solo CombatRecord ya persistido en CreatureDNA), pero el flush asegura que cualquier cambio reciente (equipo, inventario, etc.) no se pierda si algo falla durante el cambio.

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `Awake()` | Setea `Instance` |
| `OnEnable()` | Suscribe a `GameEvents` |
| `OnDisable()` | Desuscribe |
| `Persist(CreatureRegistrySO)` | Listener de `OnRegistryChanged` → `SaveSystem.SaveDatabase()` + `PushToCloud()` |
| `PersistFurniture(FurnitureRegistrySO)` | Listener de `OnFurnitureChanged` → `SaveSystem.SaveFurniture()` (local only) |
| `PersistInventory(PlayerInventorySO)` | Listener de `OnInventoryChanged` → `SaveSystem.SaveInventory()` (local only) |
| `OnApplicationQuit()` | `CollectLooseWorldProps()` + `FlushToCloud()` |
| `OnApplicationPause(bool)` | Same as quit si paused |
| `CollectLooseWorldProps()` | Barre `WorldPropInstance` sueltos, devuelve al inventario |

## Flujo de Persistencia

1. **Gameplay cambia state** → emite `GameEvents.OnRegistryChanged` / `OnFurnitureChanged` / `OnInventoryChanged`
2. **GameManager.Persist()** escucha → `SaveSystem.SaveDatabase()` (local) + `PushToCloud()` (cloud)
3. **Cloud push:** vía `CloudSyncService.PushAsync()`, async, no bloquea
4. **On quit/pause:** `FlushToCloud()` = `SaveSystem.SaveDatabase()` + `PushToCloud()` garantizado
5. **On scene change:** **S34** `FlushForSceneChange()` = same como quit, defensivo antes de LoadScene

## Getters Públicos

| Getter | Tipo | Descripción |
|--------|------|-------------|
| `ServerNow` | `DateTime` | Hora servidor con offset local, fallback `DateTime.Now` |
| `Registry` | `CreatureRegistrySO` | Criaturas vivas |
| `FurnitureRegistry` | `FurnitureRegistrySO` | Muebles colocados |
| `Inventory` | `PlayerInventorySO` | Items + creature parts sueltos |
| `Database` | `CreatureDatabaseSO` | Stats/parts/rareza definitions |
| `RarityOddsTable` | `RarityOddsTableSO` | Probabilidades de rareza por cría |
| `PersonalityProfiles` | `PersonalityProfileSO` | Datos de personalidades |
| `PartVisualBank` | `PartVisualBankSO` | Meshes/prefabs de partes |
| `FurTypeDatabase` | `FurTypeDatabaseSO` | Tipos de pelaje |
| `EquipmentDatabase` | `EquipmentDatabaseSO` | Items, stats, procs |

## Campos Serializados (via Odin)

- `database`, `rarityOddsTable`, `personalityProfiles` — config
- `creatureRegistry`, `furnitureRegistry`, `inventory` — state SO
- `partVisualBank`, `furTypeDatabase`, `equipmentDatabase` — visual/combat data
- `cloudSync` — servicio de sincronización

## Notas

- **Singleton:** Setea `Instance` en Awake; destruye duplicados
- **Persistencia reactiva:** Eventos impulsan SaveSystem, no loops
- **Cloud async:** PushAsync no bloquea; seguro desde cualquier contexto
- **WorldProps collector:** Barre propiedades sueltas excepto hotbar activo (ya persistido via slot)
- **S34 FlushForSceneChange:** Patrón defensivo para transiciones de escena; identico a OnApplicationQuit
- **Server time:** Sincronizado con UGS; fallback offline
