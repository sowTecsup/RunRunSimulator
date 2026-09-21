---
tags: [persistence, serialization, versioning]
---

# SaveMigrations

**Ruta:** `Scripts/Logic/SaveMigrations.cs` (assembly `MoriMonchi.Logic`)

**Responsabilidad:** Cadena de transformaciones JSON que mantiene guardados vigentes entre versiones. `Read(json, kind)` deserializa, detecta versión, migra si es necesario, y devuelve [[SaveEnvelope]]. `Write(data, ticks)` envuelve el payload con versión y timestamp. **Nunca lanza excepciones:** JSON nulo, vacío o corrupto devuelven sobre vacío. Un guardado futuro se devuelve intacto.

**S128:** Introducida para soportar evolución de persistencia sin datos perdidos.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Read(string json, SaveKind kind)` | `SaveEnvelope` | Lee JSON, detecta versión, migra si es necesario; **nunca lanza** |
| `Write(JToken data, long ticks)` | `string` | Envuelve data en sobre v2 con timestamp; retorna JSON string |

## Constantes

| Constante | Valor | Descripción |
|-----------|-------|-------------|
| `CurrentVersion` | 2 | Versión activa; todos los Write generan v2 |

## Logica de Read

1. **Parseo seguro:** intenta `JToken.Parse(json)`, captura `JsonException` → retorna sobre vacío
2. **Detección de envoltorio:** si la raíz es JObject con `Version` y `Data` → es sobre; si no → es v1 legacy
3. **Migraciones:** bucle `fromVersion < CurrentVersion`, llama `Migrate(kind, version, envelope)`
4. **Retorno:** sobre con `Version = CurrentVersion`

## Migraciones Implementadas

### v1 → v2 (Inventory solo)

`InventoryV1ToV2()` (lineas 97-111):

```
AdventureMaterial → Minerita  (renombramiento)
Borra: PassiveMaterial, EvolutionEssence
Deja intacto: Dabloons, FurnitureOwned, WorldPropsStored, EquipmentGrids, HotbarSlots
```

Otros `SaveKind` (`Registry`, `Furniture`, `Social`) pasan sin cambios.

## Casos de Borde (nunca lanzan)

- JSON nulo → sobre vacío
- JSON vacío string → sobre vacío
- Whitespace string → sobre vacío
- JSON corrupto → sobre vacío
- Array en vez de Object → se preserva intacto
- Versión futura (`> CurrentVersion`) → se devuelve intacto sin migrar
- Data null → sobre con `Data = null`

## Ciclo de Vida

1. [[SaveSystem.Deserialize*]] llama `SaveMigrations.Read()` para cada tipo
2. [[SaveSystem.Serialize*]] llama `SaveMigrations.Write()`
3. [[GameManager]] desencadena persitencia vía eventos
4. Arranque: [[CloudSyncOps.SyncOnStartupAsync]] lee locales vía deseriadores

## Pruebas (EditMode)

10 casos en [[SaveMigrationsTests]]:
- v1 → v2 renombramiento y borrado
- v2 round-trip (Write → Read)
- v2 sin `AdventureMaterial`
- Legado Registry intacto
- Casos de borde (null, empty, whitespace, malformed, array, null data)

## Vinculado a

[[Index/07 - Persistence & Identity]] (S128 sección)

**Conexiones:** [[SaveEnvelope]], [[SaveSystem]], [[SaveMigrationsTests]]

