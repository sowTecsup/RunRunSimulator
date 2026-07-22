---
tags: [script, core, singleton]
---

# GameManager.cs

**Ruta:** `Core/GameManager.cs`

**Responsabilidad:** Ciclo de vida del juego. Singleton que centraliza acceso a assets (database, registries, configs). Único orquestador de persistencia: escucha `GameEvents.RegistryChanged`, `FurnitureChanged`, `InventoryChanged` y ejecuta persistencia local/cloud. **S58:** Getter `PartVisualBank` eliminado (migración Suriyun completa — solo MonchiVisualBank). **S61:** `MintRandomCreature()` llama `GenerateRandom(database, furTypeDatabase)` sin `rarityOddsTable`; el campo y getter `rarityOddsTable`/`RarityOddsTable` siguen existiendo (reserva para gemas futuro).

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `PushToCloud()` | Fire-and-forget async push vía `CloudSyncService.PushAsync()` |
| `FlushToCloud()` | Save local + push cloud; usado en `OnApplicationQuit/Pause` |
| `FlushForSceneChange()` | Save local + push cloud ANTES de cambiar escena |
| `MintRandomCreature()` | **S61** Genera random creature pastel vía `GenerateRandom(database, furTypeDatabase)`, asigna género, elemento, rol, stats, nombre, registra (sin rarityOddsTable) |

## Cambios S61

**MintRandomCreature() simplificado:**
- Llama `CreatureGenerator.GenerateRandom(database, furTypeDatabase)` (sin rarityOddsTable)
- Decisión de diseño: Partes con probabilidad uniforme; rareza reservada para gemas futuras
- Campo `rarityOddsTable` y getter `RarityOddsTable` siguen serializados (reserva de arq.)
- MintRandomCreature NO usa rarityOddsTable (future: eventualmente solo para roll de gemas en breeding)

**Impacto S61:**
- Mint ahora es 100% uniforme (excepto FurType si tabla ponderada)
- Simplificación: una sola ruta de generación, sin branching de rareza
- Compatibilidad: campo rarityOddsTable sigue en inspector (no rompe serialización)

## Cambios S58

**Eliminado:**
- Campo `[SerializeField] private PartVisualBankSO partVisualBank;`
- Getter `public PartVisualBankSO PartVisualBank => partVisualBank;`
- Referencia completamente removida (no más legacy part system)

**Impacto:**
- CombatVisualUnits usa `GameManager.MonchiVisualBank` (Suriyun)
- Ningún código restante referencia PartVisualBank
- Limpieza de deuda técnica (deprecation finalizado)

## Getters Públicos

| Getter | Tipo | Descripción |
|--------|------|-------------|
| `Registry` | `CreatureRegistrySO` | Creature registry |
| `FurnitureRegistry` | `FurnitureRegistrySO` | Furniture registry |
| `Inventory` | `PlayerInventorySO` | Player inventory |
| `Database` | `CreatureDatabaseSO` | Creature database |
| `RarityOddsTable` | `RarityOddsTableSO` | **S61 RESERVA FUTURA** Rarity odds table (sin uso en S61, campo serializado para gemas) |
| `RoleWorldProfiles` | `RoleWorldProfileSO` | Role profiles |
| `MonchiVisualBank` | `MonchiVisualBankSO` | **S58 ÚNICA OPCIÓN** Suriyun model bank |
| `FurTypeDatabase` | `FurTypeDatabaseSO` | Fur type database |
| `EquipmentDatabase` | `EquipmentDatabaseSO` | Equipment database |

## Notas

- S61: Mint ahora uniforme (sans rareza); rarityOddsTable reservado para gemas futuro
- S58: Única capa visual es MonchiVisualBank (Suriyun rig + MonchiAnimationDriver)
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
