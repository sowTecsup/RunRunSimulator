---
tags: [persistence, io, serialization]
---

# SaveSystem

**Ruta:** `Core/SaveSystem.cs`

**Responsabilidad:** I/O de persistencia local (JSON) y serialización. Guarda/carga CreatureRegistry, FurnitureRegistry, PlayerInventory y SocialGraph aislados por scope de jugador (multi-instance support). Newtonsoft.Json + UnityColorConverter. **S65:** Métodos de persistencia del historial de afinidad social (SaveSocialGraph, LoadSocialGraph).

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `SetUserScope(string playerId)` | void | Namespaces el archivo de save. Sin scope → "creature_database.json"; con scope → "creature_database_{playerId}.json" |
| `SaveDatabase(CreatureRegistrySO registry)` | void | Serializa registry entero → JSON archivo |
| `LoadInto(CreatureRegistrySO registry)` | void | Deserializa JSON → `registry.LoadFrom(data)`. Hereda save viejo si es primer login con scope |
| `Serialize(Dictionary<string, CreatureDNA> data)` | `string` | JSON string de diccionario |
| `Serialize(CreatureDNA dna)` | `string` | JSON string de una criatura |
| `Deserialize(string json)` | `Dictionary<string, CreatureDNA>` | JSON string → diccionario |
| `SerializeFurniture(FurnitureRegistrySO registry)` | `string` | JSON de furniture registry |
| `DeserializeFurniture(string json)` | `Dictionary<string, PlacedFurniture>` | JSON → furniture dict |
| `SaveFurniture(FurnitureRegistrySO registry)` | `void` | Guarda placed furniture scoped por jugador |
| `LoadFurniture(FurnitureRegistrySO registry)` | `void` | Carga placed furniture; empty start si no existe |
| `SerializeInventory(PlayerInventorySO inventory)` | `string` | JSON de inventario |
| `DeserializeInventory(string json)` | `PlayerInventorySO.InventoryData` | JSON → inventory data |
| `SaveInventory(PlayerInventorySO inventory)` | `void` | Guarda inventario scoped |
| `LoadInventory(PlayerInventorySO inventory)` | `void` | Carga inventario; empty start si no existe |
| `SaveSocialGraph()` | `void` | **S65 NUEVO** Exporta deltas de SocialGraphService a social_graph_<playerId>.json |
| `LoadSocialGraph(CreatureRegistrySO registry)` | `void` | **S65 NUEVO** Carga social_graph_<playerId>.json e importa a SocialGraphService con poda de huérfanos (criaturas ya eliminadas) |

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
    Converters = new List<JsonConverter> { new UnityColorConverter(), new StringEnumConverter() },
    Formatting = Formatting.Indented,
    NullValueHandling = NullValueHandling.Ignore,
};
```

- `UnityColorConverter` — serializa `Color` a hex
- `StringEnumConverter` — enums como strings (ej. `Tier.Tier1` → `"Tier1"`)
- `Indented` — legible para debug
- `IgnoreNull` — omite campos null

## Social Graph (S65)

**SaveSocialGraph():** Llama `SocialGraphService.ExportData()`, serializa el diccionario `Dictionary<string, float>` (PairKey → delta) a JSON en ruta scoped.

**LoadSocialGraph(CreatureRegistrySO registry):** Lee social_graph_<playerId>.json, deserializa a diccionario, luego llama `SocialGraphService.ImportData(data, id => registry.TryGet(id, out _))` para poda de huérfanos. Si no existe archivo, llama `SocialGraphService.Clear()`.

## Vinculado a

- [[Index/07 - Persistence & Identity]]
- [[GameManager]] — orquesta persistencia vía eventos
- [[CloudSyncService]] — sincroniza local ↔ cloud
- [[SocialGraphService]] — guarda/carga history de interacciones

**Conexiones:** [[GameManager]], [[CloudSyncService]], [[CreatureRegistrySO]], [[FurnitureRegistrySO]], [[PlayerInventorySO]], [[SocialGraphService]]

