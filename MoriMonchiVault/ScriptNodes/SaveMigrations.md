---
tags: [persistence, serialization, versioning]
---

# SaveMigrations

**Ruta:** `Scripts/Logic/SaveMigrations.cs` (assembly `MoriMonchi.Logic`)

**Responsabilidad:** Cadena de transformaciones JSON que mantiene guardados vigentes entre versiones. `Read(json, kind)` deserializa, detecta versión, migra si es necesario, y devuelve [[SaveEnvelope]]. `Write(data, ticks)` envuelve el payload con versión y timestamp. **Nunca lanza excepciones:** JSON nulo, vacío o corrupto devuelven sobre vacío. S131: Agregado SaveKind.World para persistencia de estado de mundo.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `Read(string json, SaveKind kind)` | `SaveEnvelope` | Lee JSON, detecta versión, migra si es necesario; **nunca lanza** |
| `Read(string json, SaveKind kind, long nowTicks)` | `SaveEnvelope` | (overload) igual, pero con timestamp personalizado |
| `Write(JToken data, long savedAtTicks)` | `string` | Envuelve data en sobre v4 con timestamp; retorna JSON string |

## Enum SaveKind

```csharp
public enum SaveKind
{
    Registry,   // CreatureRegistrySO (Alive + Departed DNAs)
    Furniture,  // FurnitureRegistrySO (placed furniture)
    Inventory,  // PlayerInventorySO
    Social,     // SocialGraphService (pairwise relationships)
    World       // WorldStateSO (day, minute, tutorial) — S131
}
```

## Constantes

| Constante | Valor | Descripción |
|-----------|-------|-------------|
| `CurrentVersion` | 4 | **(S131)** Versión activa; todos los Write generan v4 |
| `LegacyCreatureStatFields` | string[] | Campos removidos S129: `BaseConstitution`, `BaseAttack`, `BaseSpeed`, `BaseDefense`, `BaseLuck`, `BaseEvasion`, `Equipped` |

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

Otros `SaveKind` (`Registry`, `Furniture`, `Social`, `World`) pasan sin cambios.

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

**Inventory, Furniture, Social, World:** pasan intactos.

### v3 → v4 (S131 World intro)

**(S131)** No hay transformación de datos existentes. v4 solo añade nueva capability (SaveKind.World) para persistencia de estado mundo. Guardados v3 siguen siendo válidos; Write solo genera v4.

## SaveEnvelope Estructura

```csharp
public class SaveEnvelope
{
    public int Version = 0;
    public long SavedAtTicks = 0;
    public JToken Data = null;
}
```

**Write result (v4):**
```json
{
  "Version": 4,
  "SavedAtTicks": 638475942000000000,
  "Data": { ... }
}
```

## Casos de Borde (nunca lanzan)

- JSON nulo → sobre vacío
- JSON vacío string → sobre vacío
- Whitespace string → sobre vacío
- JSON corrupto → sobre vacío
- Array en vez de Object → se preserva intacto
- Versión futura (`> CurrentVersion`) → se devuelve intacto sin migrar
- Data null → sobre con `Data = null`
- SaveKind.World + v3 legacy → pasa intacto

## Ciclo de Vida

1. [[SaveSystem.Deserialize*]] llama `SaveMigrations.Read(json, kind)` para cada tipo
2. [[SaveSystem.Serialize*]] llama `SaveMigrations.Write(data, ticks)`
3. [[GameManager]] desencadena persistencia vía eventos
4. [[GameManager.FlushToCloudAsync()]] guarda registry, furniture, inventory, social graph y **world state** (S131)
5. Arranque: [[CloudSyncOps.SyncOnStartupAsync]] lee locales vía deseriadores

## Invariantes S131

- **Versionado en cada serialización:** Cambios menores usan pases silenciosos (v3 → v4).
- **SaveKind dispatch:** `Migrate()` toma `kind` para decidir lógica.
- **Backward compat:** v1/v2/v3 seguirán migrando a v4 sin pérdida.

## Pruebas (EditMode)

Tests en [[SaveMigrationsTests]]:
- v1 → v2 → v3 → v4 chain migrations
- v2 → v3 Registry transform
- v2 → v3 CreatureDNA legacy field stripping
- Round-trip v4 (Write → Read)
- Casos de borde (null, empty, whitespace, malformed, array, null data)

## Vinculado a

- [[Index/07 - Persistence & Identity]]
- [[Index/09 - Active Context]]

## Conexiones

**I/O:**
- [[SaveSystem]] — punto de entrada, llama Read/Write para todos los tipos

**Data:**
- [[SaveEnvelope]] — estructura de sobre
- [[RegistryData]], [[WorldStateData]] — payloads principales

**Sistemas:**
- [[GameManager]] — dispara persistencia
- [[CloudSyncOps]] — carga iniciales
- [[CloudSyncService]] — push a nube

## Notas (S131 HC-4)

- **CurrentVersion = 4:** S131 incrementó sin cambio de formato (preparando para futuro).
- **SaveKind.World:** Nuevo kind, pero sin migraciones v3→v4 de contenido (World es nuevo SO).
- **Timestamp:** Todas las serializaciones incluyen DateTime.UtcNow.Ticks para tracking.
- **Roundtrip:** Leer v3, escribir v4, releer v4 produce datos idénticos.
