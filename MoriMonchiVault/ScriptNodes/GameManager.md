---
tags: [script, core]
---

# GameManager.cs

**Ruta:** `Core/GameManager.cs`

**Responsabilidad:** Ciclo de vida del juego. Singleton que centraliza acceso a assets (database, registries, configs). Único orquestador de persistencia: escucha `GameEvents.OnRegistryChanged`, `OnFurnitureChanged`, `OnInventoryChanged` y ejecuta persistencia local/cloud. En AppQuit/AppPause, flush a cloud. **S34:** Nuevo método `FlushForSceneChange()` para flush antes de cambiar de escena sin perder estado. **S37:** `MintRandomCreature()` asigna `Role` aleatorio vía `CreatureGenerator.RandomRole()`. `MintRandomCreature()` genera partes/color/FurType, asigna personalidad y stats base, asigna role, y registra. Dev tooling para inventario/genética vive en `GeneticsLabPreview`, `DevToolsConsole`. Getters actuales: `Registry`, `FurnitureRegistry`, `Inventory`, `Database`, `RarityOddsTable`, `PersonalityProfiles`, `PartVisualBank`, `FurTypeDatabase`, `EquipmentDatabase`, `RoleProfiles` (S37).

**Vinculado a:** [[Index/07 - Persistence & Identity]], [[Index/13 - Combat Design Direction]]

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `PushToCloud()` | Fire-and-forget async push vía `CloudSyncService.PushAsync()` |
| `FlushToCloud()` | Save local + push cloud; usado en `OnApplicationQuit/Pause` |
| `FlushForSceneChange()` | **S34** Save local + push cloud ANTES de cambiar escena; patron identico a OnApplicationQuit |
| `MintRandomCreature()` | **S37** Genera random creature, asigna stats, personalidad, **rol (S37)**, nombre, registra |

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
public CreatureDNA MintRandomCreature()
{
    var dna = CreatureGenerator.GenerateRandom(Database, RarityOddsTable);
    
    // Stats base point-buy (18 puntos)
    var (con, atk, spd) = CreatureGenerator.RandomBaseStats();
    dna.BaseConstitution = con;
    dna.BaseAttack = atk;
    dna.BaseSpeed = spd;
    
    // Personalidad y rol no heredados, asignados al azar
    dna.Personality = CreatureGenerator.RandomPersonality();
    dna.Role = CreatureGenerator.RandomRole();  // **S37 NEW**
    
    // Nombre + registro
    dna.CustomName = CreatureNameBank.GenerateName(dna, Database);
    dna.Stamp();
    Registry.Register(dna);
    GameEvents.OnCreatureMinted(dna);
    GameEvents.OnRegistryChanged(Registry);
    
    return dna;
}
```

**Impacto:** Cada criatura mint tiene rol aleatorio 1/3. Role impacta stats (mods ConMod/AtkMod/SpdMod) durante combate vía BuildCombatant. Role es metadata (no genético), hereda en breeding.

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

## Getters Públicos (S37)

| Getter | Tipo | Descripción |
|--------|------|-------------|
| `RoleProfiles` | `RoleTableSO` | **S37 NEW** Ref a asset de perfiles de rol (Protector/Agresivo/Empático) |

## Conexiones

Entrada:
- `GameEvents.OnCreatureMinted()` — dispara registro automático
- `CreatureGenerator` — genera partes/color/stats/personalidad/**role (S37)**

Salida:
- `GameEvents.OnCreatureMinted()` — notifica listeners
- `GameEvents.OnRegistryChanged()` → `Persist()` → persistencia automática

## Notas

- **Mint automático:** Cada criatura nueva recibe rol aleatorio al azar. No hay input del jugador en la asignación de rol (a diferencia de personalidad que podría ser seleccionable en futuro).
- **Role herencia:** En breeding, el rol se asigna vía BreedingService (50/50 padres, no vía GameManager).
- **S37:** El único cambio público es que MintRandomCreature ahora asigna Role (internamente llama CreatureGenerator.RandomRole()).
