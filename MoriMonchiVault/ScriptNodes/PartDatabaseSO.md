---
tags: [scriptable-object, database, genetics]
---

# PartDatabaseSO.cs

**Ruta:** `Data/Databases/PartDatabaseSO.cs`

**Responsabilidad:** Base abstracta genérica `PartDatabaseSO<T> : KeyedDatabaseSO<T>` para databases de partes (Body, Horn, Back, Wing, Face). Hereda de KeyedDatabaseSO protocolo de ID auto-sync. Campo `parts` Dictionary<string, T> indexado por ID. Métodos: `GetPartByID()` (wrapper GetByID), `GetRandomPart(Rarity?, PartSet?)` (filtrado), `RollAllNames()` (botón editor: asigna nombres random). Propiedades: `Parts` (acceso al diccionario), `PartCount` (total).

**S93:** Hereda de [[KeyedDatabaseSO]]. `GetByID` ahora es wrapper en base class.

## Campos Públicos

| Campo | Tipo | Acceso | Descripción |
|-------|------|--------|-------------|
| `parts` | `Dictionary<string, T>` | [OdinSerialize] private | Todas las partes indexadas por ID |

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `GetPartByID(id)` | `T` | Busca parte por ID (wrapper de GetByID) |
| `GetRandomPart(Rarity?, PartSet?)` | `T` | Selecciona random parte, con filtros opcionales |
| `GetByID(id)` [inherited] | `T` | De KeyedDatabaseSO: búsqueda por ID |

## Métodos Editor (Odin)

| Método | Descripción |
|--------|-------------|
| `RollAllNames()` | Botón: asigna nombre random a cada parte vía `PartNameBank.GetRandomName(part.Set, part.GetPartRole())` |
| `PopulateFromBuffer()` [inherited] | Botón: arrastra assets, auto-asigna IDs |
| `SyncAllIDs()` [inherited] | Botón: renumera todas las partes con prefijo |

## Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Parts` | `Dictionary<string, T>` | Diccionario completo |
| `PartCount` | `int` | Total de partes registradas |
| `Count` [inherited] | `int` | De KeyedDatabaseSO |

## Implementaciones Concretas

Clases que heredan `PartDatabaseSO<T>`:
- `BodyShapeDatabaseSO : PartDatabaseSO<BodyShapePart>`
- `HornDatabaseSO : PartDatabaseSO<BodyPart>` (asume role Body; legacy)
- `BackDatabaseSO : PartDatabaseSO<BodyPart>` (role Back)
- `WingDatabaseSO : PartDatabaseSO<BodyPart>` (role Wing)
- `FaceDatabaseSO : PartDatabaseSO<BodyPart>` (role Face)

## Vinculado a

- [[Index/01 - Creature Genetics & System]]
- [[KeyedDatabaseSO]] — protocolo base
- [[BodyPart]] — tipo genérico T
- [[PartNameBank]] — nombres aleatorios

**Conexiones:** [[KeyedDatabaseSO]], [[BodyPart]], [[BodyShapePart]], [[PartNameBank]], [[CreatureGenerator]]

