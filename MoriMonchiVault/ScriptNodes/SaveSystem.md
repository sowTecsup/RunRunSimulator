---
tags: [persistence, io, serialization]
---

# SaveSystem

**Ruta:** `Core/SaveSystem.cs`

**Responsabilidad:** I/O de persistencia local (JSON) y serialización. Guarda/carga CreatureRegistry, FurnitureRegistry, PlayerInventory aislados por scope de jugador (multi-instance support). Newtonsoft.Json + UnityColorConverter.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `SetUserScope(string playerId)` | void | Namespaces el archivo de save. Sin scope → "creature_database.json"; con scope → "creature_database_{playerId}.json" |
| `SaveDatabase(CreatureRegistrySO registry)` | void | Serializa registry entero → JSON archivo |
| `LoadInto(CreatureRegistrySO registry)` | void | Deserializa JSON → `registry.LoadFrom(data)`. Hereda save viejo si es primer login con scope |
| `Serialize(Dictionary<string, CreatureDNA> data)` | `string` | JSON string de diccionario |
| `Serialize(CreatureDNA dna)` | `string` | JSON string de una criatura |
| `DeserializeCreature(string json)` | `CreatureDNA` | **NUEVO S32** JSON string → `CreatureDNA` instance. Inverso de `Serialize(CreatureDNA)` |
| `Deserialize(string json)` | `Dictionary<string, CreatureDNA>` | JSON string → diccionario |
| `SerializeFurniture(FurnitureRegistrySO registry)` | `string` | JSON de furniture registry |
| `DeserializeFurniture(string json)` | `Dictionary<string, PlacedFurniture>` | JSON → furniture dict |
| `SerializeInventory(PlayerInventorySO inventory)` | `string` | JSON de inventario |
| `DeserializeInventory(string json)` | `PlayerInventorySO.InventoryData` | JSON → inventory data |

## Rutas & Scoping

- **Base:** `Application.persistentDataPath`
- **Sin scope:** `creature_database.json`, `furniture_registry.json`, `player_inventory.json`
- **Con scope:** `creature_database_{userId}.json`, etc.

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

## Novo en S32: DeserializeCreature

Inverso de `Serialize(CreatureDNA)`. Utilizado por:
- `CombatDevConsole.VerifyDeterminismButton()` — clona criaturas para verificar determinismo
- `AsyncCombatService.ApplyResult()` — deserializa snapshots del `CloudMatchBlob`

```csharp
public static CreatureDNA DeserializeCreature(string json) =>
    string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<CreatureDNA>(json, Settings);
```

## Vinculado a

- [[Index/07 - Persistence & Identity]]
- [[GameManager]] — orquesta `SaveDatabase()` en `OnEnable`
- [[CloudSyncService]] — sincroniza local ↔ cloud
- [[CombatDevConsole]] — deserializa para clonación de tests
- [[AsyncCombatService]] — deserializa snapshots de cloud

## Conexiones

**Entrada:**
- `GameManager` — trigger de `SaveDatabase()` vía `GameEvents.OnRegistryChanged`
- `CloudSyncService` — escribe blobs en cloud, lee locals
- `CombatDevConsole` — deserializa para verificación

**Salida:**
- `Application.persistentDataPath/*.json` — almacenamiento local
- Registries mutadas via `registry.LoadFrom(data)`

## Notas

- Sin manejo de excepción explícito en métodos; propagadas al caller.
- `NullValueHandling.Ignore` significa que campos null no se escriben; si faltan en JSON durante deseri, se defaultean a null o 0.
- Conversores custom solo para `Color` y `enum`; tipos complejos (ej. `Dictionary`) deserializan nativament.
