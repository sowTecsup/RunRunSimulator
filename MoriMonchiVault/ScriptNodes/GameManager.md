---
tags: [script, core]
---

# GameManager.cs

**Ruta:** `Core/GameManager.cs`

**Responsabilidad:** Ciclo de vida del juego. Singleton que centraliza acceso a assets (database, registries, configs). Único orquestador de persistencia: escucha `GameEvents.RegistryChanged`, `FurnitureChanged`, `InventoryChanged` y ejecuta persistencia local/cloud. En AppQuit/AppPause, flush a cloud. **S34:** Nuevo método `FlushForSceneChange()` para flush antes de cambiar de escena sin perder estado. **S37:** `MintRandomCreature()` asigna `Role` aleatorio vía `CreatureGenerator.RandomRole()`. **S39:** `MintRandomCreature()` asigna `Element` aleatorio vía `CreatureGenerator.RandomElement()`. `MintRandomCreature()` genera partes/color/FurType, asigna género, elemento, rol, stats base, y registra. Dev tooling para inventario/genética vive en `GeneticsLabPreview`, `DevToolsConsole`. Getters actuales: `Registry`, `FurnitureRegistry`, `Inventory`, `Database`, `RarityOddsTable`, `RoleWorldProfiles`, `PartVisualBank`, `FurTypeDatabase`, `EquipmentDatabase`.

**Vinculado a:** [[Index/07 - Persistence & Identity]], [[Index/13 - Combat Design Direction]]

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `PushToCloud()` | Fire-and-forget async push vía `CloudSyncService.PushAsync()` |
| `FlushToCloud()` | Save local + push cloud; usado en `OnApplicationQuit/Pause` |
| `FlushForSceneChange()` | **S34** Save local + push cloud ANTES de cambiar escena; patron identico a OnApplicationQuit |
| `MintRandomCreature()` | **S37/S39** Genera random creature, asigna gender, **elemento (S39)**, rol (S37), stats, nombre, registra |

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

## Cambios S37 — MintRandomCreature con Role

Actualizado para asignar rol aleatorio:

```csharp
dna.Role = CreatureGenerator.RandomRole();  // S37 NEW
```

**Impacto:** Cada criatura mint tiene rol aleatorio 1/3. Role impacta stats (mods ConMod/AtkMod/SpdMod) durante combate vía BuildCombatant. Role es metadata (no genético), hereda en breeding.

## Cambios S39 — MintRandomCreature con Element

Actualizado para asignar elemento aleatorio:

```csharp
dna.Element = CreatureGenerator.RandomElement();  // S39 NEW
```

**Impacto:** Cada criatura mint tiene elemento aleatorio. Element es afinidad elemental que hereda 50/50 en breeding con chance de mutación. Conduce reacciones elementales en combate vía `CombatElements`.

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `Awake()` | Setea `Instance` |
| `OnEnable()` | Suscribe a `GameEvents` |
| `OnDisable()` | Desuscribe |
| `Persist(CreatureRegistrySO)` | Listener de `RegistryChanged` → `SaveSystem.SaveDatabase()` + `PushToCloud()` |
| `PersistFurniture(FurnitureRegistrySO)` | Listener de `FurnitureChanged` → `SaveSystem.SaveFurniture()` (local only) |
| `PersistInventory(PlayerInventorySO)` | Listener de `InventoryChanged` → `SaveSystem.SaveInventory()` (local only) |
| `OnApplicationQuit()` | `CollectLooseWorldProps()` + `FlushToCloud()` |
| `OnApplicationPause(bool)` | Same as quit si paused |
| `CollectLooseWorldProps()` | Barre `WorldPropInstance` sueltos, devuelve al inventario |

## Getters Públicos

| Getter | Tipo | Descripción |
|--------|------|-------------|
| `Registry` | `CreatureRegistrySO` | Ref a creature registry |
| `FurnitureRegistry` | `FurnitureRegistrySO` | Ref a furniture registry |
| `Inventory` | `PlayerInventorySO` | Ref a player inventory |
| `Database` | `CreatureDatabaseSO` | Ref a creature database |
| `RarityOddsTable` | `RarityOddsTableSO` | Ref a rarity odds table |
| `RoleWorldProfiles` | `RoleWorldProfileSO` | Ref a role world profiles (S37) |
| `PartVisualBank` | `PartVisualBankSO` | Ref a part visual bank |
| `FurTypeDatabase` | `FurTypeDatabaseSO` | Ref a fur type database |
| `EquipmentDatabase` | `EquipmentDatabaseSO` | Ref a equipment database |

## Conexiones

Entrada:
- `GameEvents.CreatureMinted()` — dispara registro automático
- `CreatureGenerator` — genera partes/color/gender/element (S39)/role (S37)/stats/personalidad

Salida:
- `GameEvents.CreatureMinted()` — notifica listeners
- `GameEvents.RegistryChanged()` → `Persist()` → persistencia automática

## Notas

- **Mint automático:** Cada criatura nueva recibe género, elemento y rol aleatorio. No hay input del jugador en la asignación (a diferencia de personalidad que podría ser seleccionable en futuro).
- **Element herencia:** En breeding, el elemento se asigna vía BreedingService (50/50 padres con chance de mutación).
- **Role herencia:** En breeding, el rol se asigna vía BreedingService (50/50 padres).
- **S39:** Nuevo método `CreatureGenerator.RandomElement()` asigna afinidad elemental al mint.
