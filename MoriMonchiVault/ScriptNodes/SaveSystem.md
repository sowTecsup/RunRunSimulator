---
tags: [persistence, io, serialization]
---

# SaveSystem

**Ruta:** `Core/SaveSystem.cs`

**Responsabilidad:** I/O de persistencia local (JSON) para CreatureRegistry, FurnitureRegistry, PlayerInventory y SocialGraph aislados por scope de jugador (multi-instancia). **S128:** Toda lectura/escritura pasa por [[SaveMigrations]] (sobre con versión y timestamp). Métodos públicos: `SetUserScope()`, `SaveDatabase()`, `LoadInto()`, `Save/Load` de muebles/inventario/grafo social, `SerializeSocialGraph()`, `LatestLocalSavedAt()`, `BackupLocal()`, `LoadDatabaseCopy()` (lectura sin registro).

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `SetUserScope(string playerId)` | `void` | Scope para archivos (sin scope → "file.json"; con scope → "file_{userId}.json") |
| `SaveDatabase(CreatureRegistrySO registry)` | `void` | Guarda registry completo → JSON sobre v2 |
| `LoadInto(CreatureRegistrySO registry)` | `void` | Carga JSON → registry.LoadFrom(); hereda legado si primer login |
| `Serialize(Dictionary)` | `string` | Serializa diccionario criaturas → JSON sobre |
| `Serialize(CreatureDNA)` | `string` | Serializa una criatura → JSON (no sobre) |
| `Deserialize(string json)` | `Dictionary<string, CreatureDNA>` | JSON → diccionario (migra si es v1) |
| `SerializeFurniture(registry)` | `string` | Serializa muebles → JSON sobre |
| `DeserializeFurniture(json)` | `Dictionary<string, PlacedFurniture>` | JSON → muebles dict |
| `SaveFurniture(registry)` | `void` | Guarda muebles disco |
| `LoadFurniture(registry)` | `void` | Carga muebles; empty start si no existe |
| `SerializeInventory(inventory)` | `string` | Serializa inventario → JSON sobre |
| `DeserializeInventory(json)` | `PlayerInventorySO.InventoryData` | JSON → inventory data (migra si v1) |
| `SaveInventory(inventory)` | `void` | Guarda inventario disco |
| `LoadInventory(inventory)` | `void` | Carga inventario; empty start si no existe |
| `SaveSocialGraph()` | `void` | **S128 NUEVO** Exporta grafo social → JSON sobre |
| `LoadSocialGraph(registry)` | `void` | Carga grafo social, filtra huérfanos |
| `SerializeSocialGraph()` | `string` | JSON string del grafo |
| `DeserializeSocialGraph(json)` | `Dictionary<string, float>` | JSON → grafo dict |
| `LatestLocalSavedAt()` | `long` | **S128 NUEVO** Retorna timestamp (ticks) del archivo más reciente modificado |
| `BackupLocal(string suffix)` | `void` | **S128 NUEVO** Respalda los 4 archivos con sufijo: `file.{suffix}.bak.json` |
| `LoadDatabaseCopy()` | `Dictionary<string, CreatureDNA>` | **S119 NUEVO** Lee copy sin tocar registry ni disparar eventos |

## Rutas & Scoping

- **Base:** `Application.persistentDataPath`
- **Archivos:**
  - Sin scope: `creature_database.json`, `furniture_registry.json`, `player_inventory.json`, `social_graph.json`
  - Con scope: `creature_database_{userId}.json`, `furniture_registry_{userId}.json`, `player_inventory_{userId}.json`, `social_graph_{userId}.json`

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
- `Write()`: envuelve data en `{ Version: 2, SavedAtTicks: UtcNow.Ticks, Data: ... }`
- `Read()`: deserializa, detecta versión, migra si es necesario (v1 → v2 para Inventory)

## Social Graph (S65)

**SaveSocialGraph():** Llama `SocialGraphService.ExportData()` → diccionario (PairKey → delta) → JSON sobre scoped.

**LoadSocialGraph():** Lee social_graph_<playerId>.json, deserializa, filtra huérfanos (criaturas eliminadas), importa a SocialGraphService. Sin archivo → `Clear()`.

## Nuevos en S128

| Método | Uso |
|--------|-----|
| `LatestLocalSavedAt()` | [[CloudSyncOps]] para detectar cambios locales vs nube |
| `BackupLocal(suffix)` | Crea respaldos `*.conflict.bak.json` antes de aplicar merge en conflictos |
| `SerializeSocialGraph()` / `DeserializeSocialGraph()` | Encapsulan lógica del grafo social |

## Invariantes S128+

- Nunca lanza si JSON es nulo/corrupto → sobre vacío
- Todos los archivos usan sobre (Version/SavedAtTicks/Data)
- Timestamp: `DateTime.UtcNow.Ticks` al escribir, extraído al leer para reconciliación
- Scoping: `_userScope` centralizado, heredado sin scope si primer login

## Vinculado a

[[Index/07 - Persistence & Identity]]

**Conexiones:** [[GameManager]], [[CloudSyncOps]], [[CloudSyncService]], [[CreatureRegistrySO]], [[FurnitureRegistrySO]], [[PlayerInventorySO]], [[SocialGraphService]], [[SaveMigrations]], [[SaveEnvelope]]

