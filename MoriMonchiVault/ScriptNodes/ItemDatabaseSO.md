---
tags: [scriptable-object, database, items]
---

# ItemDatabaseSO.cs

**Ruta:** `Data/Databases/ItemDatabaseSO.cs`

**Responsabilidad:** Base de datos única de items (world props, consumibles, etc.). Hereda de [[KeyedDatabaseSO]]. Indexa cada `ItemDefinitionSO` por ID (prefijo "I" → "I0", "I1", ...). Ofrece búsqueda por ID, acceso al diccionario, botones editor (PopulateFromBuffer, SyncAllIDs).

**S93:** Hereda protocolo de [[KeyedDatabaseSO]]; `IDPrefix = "I"`.

## Campos Públicos

| Campo | Tipo | Acceso | Descripción |
|-------|------|--------|-------------|
| `items` | `Dictionary<string, ItemDefinitionSO>` | [OdinSerialize] private | Items indexados por ID |

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `GetByID(id)` [inherited] | `ItemDefinitionSO` | Busca por ID ("I0"…); null si no existe |
| `GetAllIDs()` [inherited] | `List<string>` | Lista de todos los IDs |

## Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Count` [inherited] | `int` | De KeyedDatabaseSO |

## Métodos Editor (Odin)

| Método | Descripción |
|--------|-------------|
| `PopulateFromBuffer()` [inherited] | Botón: arrastra assets, auto-asigna IDs |
| `SyncAllIDs()` [inherited] | Botón: renumera con prefijo "I" |

## CreateAssetMenu

**Menu path:** `RunRunSimulator/Databases/Item Database`

## Vinculado a

- [[Index/02 - Content & Databases]]
- [[KeyedDatabaseSO]] — protocolo base

**Conexiones:** [[KeyedDatabaseSO]], [[ItemDefinitionSO]], [[PlayerInventorySO]], [[StoreManager]]

