---
tags: [persistence, tests, editmode]
---

# SaveMigrationsTests

**Ruta:** `Tests/EditMode/SaveMigrationsTests.cs` (assembly `MoriMonchi.Logic.Tests`)

**Responsabilidad:** Suite de 10 pruebas EditMode que validan [[SaveMigrations]]. Verifica round-trip (Write → Read), migraciones v1 → v2, casos de borde (null/empty/corrupto), preservación de datos y transformaciones.

**S128:** Introducida junto a SaveMigrations para asegurar robustez sin regresos.

## Pruebas

| Nombre | Qué valida | Resultado esperado |
|--------|-----------|-------------------|
| `LegacyInventory_V1ToV2_RenamesAdventureMaterialToMinerita` | v1 → v2 renombramiento | `AdventureMaterial: 93` → `Minerita: 93`, borrados campos viejos, `Dabloons: 84` intacto |
| `CurrentVersionEnvelope_RoundTrip_KeepsMineritaAndSavedAtTicks` | Write + Read v2 | Datos y timestamp preservados |
| `CurrentVersionEnvelope_WithoutAdventureMaterial_DoesNotInventMinerita` | v2 sin `AdventureMaterial` | `Minerita` null (no genera cero) |
| `LegacyRegistry_ThreeCreatures_FieldsStayIntact` | Registry v1 legado | 3 criaturas con nombres, timestamps, needs, deaths intactos |
| `Read_NullJson_ReturnsEmptyEnvelopeWithoutThrowing` | JSON null | Retorna sobre vacío, sin excepción |
| `Read_EmptyJson_ReturnsEmptyEnvelopeWithoutThrowing` | JSON string vacío | Retorna sobre vacío, sin excepción |
| `Read_WhitespaceJson_ReturnsEmptyEnvelopeWithoutThrowing` | JSON whitespace | Retorna sobre vacío, sin excepción |
| `Read_MalformedJson_ReturnsEmptyEnvelopeWithoutThrowing` | JSON corrupto (`{no es json`) | Retorna sobre vacío, sin excepción |
| `Read_JsonArrayInsteadOfObject_DoesNotThrowAndDoesNotCorrupt` | Array en vez de Object | Preserva array intacto en Data |
| `WriteThenRead_NullData_DoesNotThrow` | Write(null) → Read | Sin excepción, sobre vacío válido |

## Cobertura

- **Versiones:** v1 legado, v2 actual
- **SaveKind:** Inventory (con migraciones), Registry (intacta), Furniture (intacta), Social (intacta)
- **Casos de borde:** 6 tipos de entrada inválida sin lanzar
- **Preservación:** 3 criaturas con todos sus campos
- **Round-trip:** timestamp + datos

## Flujo de Pruebas

```
1. Construir JSON v1 legado o v2
2. SaveMigrations.Read(json, kind)
3. Validar estructura de envelope (Version, SavedAtTicks, Data)
4. Para v1 → v2: validar transformación de campos
5. Para borde: validar que no lanza + retorna sobre vacío
```

## Integración

Corre en **EditMode** (sin Play). No tiene dependencias de Unity (todo Newtonsoft.Json + JToken).

Assembly: `MoriMonchi.Logic.Tests` (autoref de `MoriMonchi.Logic`)

## Vinculado a

[[Index/07 - Persistence & Identity]] (S128 sección)

**Conexiones:** [[SaveMigrations]], [[SaveEnvelope]], [[SaveSystem]]

