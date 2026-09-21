---
tags: [persistence, serialization, versioning]
---

# SaveMigrations

**Ruta:** `Scripts/Logic/SaveMigrations.cs` (assembly `MoriMonchi.Logic`)

**Responsabilidad:** Cadena de transformaciones JSON que mantiene guardados vigentes entre versiones. `Read(json, kind)` deserializa, detecta versión, migra si es necesario, y devuelve [[SaveEnvelope]]. `Write(data, ticks)` envuelve el payload con versión y timestamp. **Nunca lanza excepciones:** JSON nulo, vacío o corrupto devuelven sobre vacío. Un guardado futuro se devuelve intacto.

**S128:** Introducida para soportar evolución de persistencia sin datos perdidos. **S129:** CurrentVersion = 3; migrations v1→v2 (Inventory renombramiento) y v2→v3 (Registry + CreatureDNA legacy field stripping).

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Read(string json, SaveKind kind)` | `SaveEnvelope` | Lee JSON, detecta versión, migra si es necesario; **nunca lanza** |
| `Write(JToken data, long ticks)` | `string` | Envuelve data en sobre v3 con timestamp; retorna JSON string |

## Constantes

| Constante | Valor | Descripción |
|-----------|-------|-------------|
| `CurrentVersion` | 3 | **(S129)** Versión activa; todos los Write generan v3 |
| `LegacyCreatureStatFields` | string[] | Campos removidos: `BaseConstitution`, `BaseAttack`, `BaseSpeed`, `BaseDefense`, `BaseLuck`, `BaseEvasion`, `Equipped` |

## Logica de Read

1. **Parseo seguro:** intenta `JToken.Parse(json)`, captura `JsonException` → retorna sobre vacío
2. **Detección de envoltorio:** si la raíz es JObject con `Version` y `Data` → es sobre; si no → es v1 legacy
3. **Migraciones:** bucle `fromVersion < CurrentVersion`, llama `Migrate(kind, version, envelope)` para cada paso
4. **Retorno:** sobre con `Version = CurrentVersion`

## Migraciones Implementadas

### v1 → v2 (Inventory solo)

`InventoryV1ToV2()`:
```
AdventureMaterial → Minerita  (renombramiento)
Borra: PassiveMaterial, EvolutionEssence
Deja intacto: Dabloons, FurnitureOwned, WorldPropsStored, EquipmentGrids, HotbarSlots
```

Otros `SaveKind` (`Registry`, `Furniture`, `Social`) pasan sin cambios.

### v2 → v3 (Registry + CreatureDNA legacy cleanup)

**(S129)** Migración múltiple por SaveKind:

**Registry (v2 → v3):**
```
Legacy: { "creatures": {...} } (diccionario único)
Nuevo:  { "Alive": {...}, "Departed": {} } (RegistryData)

Lógica:
1. Detecta si data es dict (v2) o RegistryData (v3)
2. Si v2: envuelve `{ Alive: dict, Departed: {} }`
3. Si ya v3: intacto
```

**CreatureDNA (todos, v2 → v3):**
```
Borra campos legacy de CreatureDNA dentro de diccionarios:
  - BaseConstitution, BaseAttack, BaseSpeed
  - BaseDefense, BaseLuck, BaseEvasion
  - Equipped

Por cada DNA en Alive + Departed, quita esos campos antes de retornar
```

**Inventory, Furniture, Social:** pasan intactos.

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
3. [[GameManager]] desencadena persistencia vía eventos
4. Arranque: [[CloudSyncOps.SyncOnStartupAsync]] lee locales vía deseriadores

## Pruebas (EditMode)

Tests en [[SaveMigrationsTests]]:
- v1 → v2 → v3 chain migrations
- v2 → v3 Registry transform
- v2 → v3 CreatureDNA legacy field stripping
- Round-trip v3 (Write → Read)
- Casos de borde (null, empty, whitespace, malformed, array, null data)

## Vinculado a

[[Index/07 - Persistence & Identity]] (S128-S129 sección)
[[Index/28 - Cimientos y camino a Game Ready]]

**Conexiones:** [[SaveEnvelope]], [[SaveSystem]], [[SaveMigrationsTests]], [[RegistryData]], [[CreatureDNA]]
