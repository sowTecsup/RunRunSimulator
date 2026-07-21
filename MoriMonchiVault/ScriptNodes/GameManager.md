---
tags: [script, core, singleton]
---

# GameManager.cs

**Ruta:** `Core/GameManager.cs`

**Responsabilidad:** Ciclo de vida del juego. Singleton que centraliza acceso a assets (database, registries, configs). Único orquestador de persistencia: escucha `GameEvents.RegistryChanged`, `FurnitureChanged`, `InventoryChanged` y ejecuta persistencia local/cloud. **S58:** Getter `PartVisualBank` eliminado (migración Suriyun completa — solo MonchiVisualBank).

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `PushToCloud()` | Fire-and-forget async push vía `CloudSyncService.PushAsync()` |
| `FlushToCloud()` | Save local + push cloud; usado en `OnApplicationQuit/Pause` |
| `FlushForSceneChange()` | Save local + push cloud ANTES de cambiar escena |
| `MintRandomCreature()` | Genera random creature pastel, asigna género, elemento, rol, stats, nombre, registra |

## Cambios S58

**Eliminado:**
- Campo `[SerializeField] private PartVisualBankSO partVisualBank;`
- Getter `public PartVisualBankSO PartVisualBank => partVisualBank;`
- Referencia completamente removida (no más legacy part system)

**Impacto:**
- CombatVisualUnits usa `GameManager.MonchiVisualBank` (Suriyun)
- Ningún código restante referencia PartVisualBank
- Limpieza de deuda técnica (deprecation finalizado)

## Getters Públicos (S58)

| Getter | Tipo | Descripción |
|--------|------|-------------|
| `Registry` | `CreatureRegistrySO` | Creature registry |
| `FurnitureRegistry` | `FurnitureRegistrySO` | Furniture registry |
| `Inventory` | `PlayerInventorySO` | Player inventory |
| `Database` | `CreatureDatabaseSO` | Creature database |
| `RarityOddsTable` | `RarityOddsTableSO` | Rarity odds table |
| `RoleWorldProfiles` | `RoleWorldProfileSO` | Role profiles |
| `MonchiVisualBank` | `MonchiVisualBankSO` | **S58 ÚNICA OPCIÓN** Suriyun model bank |
| `FurTypeDatabase` | `FurTypeDatabaseSO` | Fur type database |
| `EquipmentDatabase` | `EquipmentDatabaseSO` | Equipment database |

## Notas

- S58: Única capa visual es MonchiVisualBank (Suriyun rig + DragonAnimationDriver)
- Legacy PartVisualBankSO descartado completamente
- MintRandomCreature() retorna pastel colors (S58: ColorGenetics.RandomBase())

## Vinculado a

- [[Index/07 - Persistence & Identity]]
- [[Index/10 - Visualization]]
- [[CombatVisualUnits]] — consume MonchiVisualBank (S58)
- [[CreatureGenerator]] — usa Database, FurTypeDatabase
- [[BreedingService]] — usa Registry

## Conexiones

- **Entrada:** GameEvents (RegistryChanged, FurnitureChanged, InventoryChanged)
- **Salida:** SaveSystem (persistencia local), CloudSyncService (persistencia cloud)
- **Refs:** Todos los SO principales del proyecto
