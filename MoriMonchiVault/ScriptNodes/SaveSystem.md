---
tags: [persistence, io, serialization]
---

# SaveSystem

**Ruta:** `Core/SaveSystem.cs`

**Responsabilidad:** I/O de persistencia local (JSON) para CreatureRegistry, FurnitureRegistry, PlayerInventory, SocialGraph y WorldState aislados por scope de jugador (multi-instancia). S128: Toda lectura/escritura pasa por [[SaveMigrations]] (sobre con versión y timestamp). S129: Registry cambió de Dictionary a RegistryData (Alive + Departed). S131: Agregados SaveWorldState, LoadWorldState, SerializeWorldState, DeserializeWorldState para estado de mundo.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `SetUserScope(string playerId)` | `void` | Scope para archivos (sin scope → "file.json"; con scope → "file_{userId}.json") |
| **Creature Database** |
| `SaveDatabase(CreatureRegistrySO registry)` | `void` | Guarda registry completo → `registry.GetData()` (RegistryData) → JSON sobre v3 |
| `LoadInto(CreatureRegistrySO registry)` | `void` | Carga JSON → `Deserialize()` → `registry.LoadFrom(RegistryData)`; hereda legado si primer login |
| `Serialize(RegistryData data)` | `string` | Serializa `RegistryData` (Alive + Departed) → JSON sobre v3 |
| `Serialize(CreatureDNA)` | `string` | Serializa una criatura → JSON (no sobre) |
| `Deserialize(string json)` | `RegistryData` | JSON → `RegistryData` (migra si es v1/v2) |
| `LoadDatabaseCopy()` | `Dictionary<string, CreatureDNA>` | Lee copy sin tocar registry ni disparar eventos |
| **Furniture** |
| `SaveFurniture(registry)` | `void` | Guarda muebles disco |
| `LoadFurniture(registry)` | `void` | Carga muebles; empty start si no existe |
| `SerializeFurniture(registry)` | `string` | Serializa muebles → JSON sobre |
| `DeserializeFurniture(json)` | `Dictionary<string, PlacedFurniture>` | JSON → muebles dict |
| **Inventory** |
| `SaveInventory(inventory)` | `void` | Guarda inventario disco |
| `LoadInventory(inventory)` | `void` | Carga inventario; empty start si no existe |
| `SerializeInventory(inventory)` | `string` | Serializa inventario → JSON sobre |
| `DeserializeInventory(json)` | `PlayerInventorySO.InventoryData` | JSON → inventory data |
| **Social Graph** |
| `SaveSocialGraph()` | `void` | Exporta grafo social → JSON sobre |
| `LoadSocialGraph(registry)` | `void` | Carga grafo social, filtra huérfanos |
| `SerializeSocialGraph()` | `string` | JSON string del grafo |
| `DeserializeSocialGraph(json)` | `Dictionary<string, float>` | JSON → grafo dict |
| **World State (S131)** |
| `SaveWorldState(WorldStateSO world)` | `void` | Guarda día/minuto/tutorial → JSON sobre |
| `LoadWorldState(WorldStateSO world)` | `void` | Carga world state; defaults si no existe |
| `SerializeWorldState(WorldStateSO world)` | `string` | Serializa WorldStateData → JSON sobre |
| `DeserializeWorldState(string json)` | `WorldStateData` | JSON → WorldStateData |
| **Utilities** |
| `LatestLocalSavedAt()` | `long` | Timestamp (ticks) del archivo más reciente (registry/furniture/inventory/social/world) |
| `BackupLocal(string suffix)` | `void` | Respalda los 5 archivos con sufijo: `file.{suffix}.bak.json` |

## Rutas & Scoping

**Base:** `Application.persistentDataPath`

**Archivos:**
- Sin scope: `creature_database.json`, `furniture_registry.json`, `player_inventory.json`, `social_graph.json`, `world_state.json`
- Con scope: `creature_database_{userId}.json`, `furniture_registry_{userId}.json`, `player_inventory_{userId}.json`, `social_graph_{userId}.json`, `world_state_{userId}.json`

**Migración automática:** Primera vez con scope, hereda save viejo sin scope si existe.

## Serialización JSON

```csharp
private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
{
    Converters = new List<JsonConverter> { 
        new UnityColorConverter(),  // Color → hex
        new StringEnumConverter()   // enums → string
    },
    Formatting = Formatting.Indented,
    NullValueHandling = NullValueHandling.Ignore,
};
```

## Sobre (SaveEnvelope)

**S128:** Toda lectura/escritura pasa por [[SaveMigrations]]:
- `Write()`: envuelve data en `{ Version: 3, SavedAtTicks: UtcNow.Ticks, Data: ... }`
- `Read()`: deserializa, detecta versión, migra si es necesario (v1/v2 → v3)

## S131: WorldState Serialization

**SaveWorldState(WorldStateSO world):**
1. Llama `world.GetData()` → `WorldStateData { Day, MinuteOfDay, TutorialStep }`
2. Serializa a JSON vía `SerializeWorldState()`
3. Envuelve en sobre V3 (SaveMigrations.Write)
4. Guarda a `world_state[_{userId}].json`

**LoadWorldState(WorldStateSO world):**
1. Lee JSON si existe, sobre migra
2. `DeserializeWorldState()` → `WorldStateData` (null-safe: retorna null si no existe)
3. `world.LoadFrom(data)` (null-safe: defaults a día=1, minuto=360, tutorial=0)

**LatestLocalSavedAt() S131 update:**
```csharp
long worldAt = SavedAtOf(ScopedPath(WORLD_FILENAME), SaveKind.World);
if (worldAt > latest) latest = worldAt;
```

## S129: RegistryData Serialization

**SaveDatabase():**
1. Llama `registry.GetData()` → `RegistryData { Alive, Departed }`
2. Serializa `RegistryData` a JSON
3. Envuelve en sobre V3

**LoadInto():**
1. Lee JSON, sobre migra si v1/v2
2. `Deserialize()` → `RegistryData`
3. `registry.LoadFrom(data)` puebla ambos diccionarios

## Social Graph

**SaveSocialGraph():** Llama `SocialGraphService.ExportData()` → diccionario → JSON sobre scoped.

**LoadSocialGraph():** Lee social_graph.json, deserializa, filtra huérfanos, importa a SocialGraphService.

## Invariantes S128+/S131

- Nunca lanza si JSON es nulo/corrupto → sobre vacío o defaults
- Todos los archivos usan sobre (Version/SavedAtTicks/Data)
- Timestamp: `DateTime.UtcNow.Ticks` al escribir, extraído al leer
- Scoping: `_userScope` centralizado, heredado sin scope si primer login
- WorldState: nuevo archivo, mismo sobre/migration pipeline

## Cambios S131

**Nuevos métodos:**
- `SaveWorldState(WorldStateSO)` — persiste estado de mundo
- `LoadWorldState(WorldStateSO)` — carga estado de mundo
- `SerializeWorldState(WorldStateSO)` → `string`
- `DeserializeWorldState(string)` → `WorldStateData`

**Integración:**
- GameManager.PersistWorldState() → SaveWorldState()
- SaveMigrations.Read/Write con SaveKind.World
- LatestLocalSavedAt() incluye world_state

## Vinculado a

- [[Index/07 - Persistence & Identity]]
- [[Index/09 - Active Context]]

## Conexiones

**Data:**
- [[CreatureRegistrySO]], [[FurnitureRegistrySO]], [[PlayerInventorySO]], [[WorldStateSO]]

**Sistemas:**
- [[GameManager]] — dispara SaveDatabase/SaveFurniture/SaveInventory/SaveWorldState
- [[CloudSyncOps]], [[CloudSyncService]] — usan para merge/conflict detection
- [[SocialGraphService]] — exporta/importa grafo social
- [[SaveMigrations]] — versionado y envelope
- [[GameClock]] — dispara WorldStateChanged → SaveWorldState

## Notas (S131 HC-4)

- **WorldState archivo nuevo:** `world_state[_{userId}].json` — separado de registry.
- **Defaults:** Si archivo no existe, LoadWorldState retorna null → SO defaults a día=1, minuto=360.
- **BackupLocal S131:** Ahora respalda 5 archivos (+ world_state).
- **Multi-account:** SaveKind enum necesita entrada World (ya incluida).
