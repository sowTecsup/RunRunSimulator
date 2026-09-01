---
tags: [scriptable-object, database, furniture]
---

# FurnitureDatabaseSO.cs

**Ruta:** `Data/Databases/FurnitureDatabaseSO.cs`

**Responsabilidad:** Base de datos única de muebles. Hereda de [[KeyedDatabaseSO]]. Indexa cada `FurnitureDefinitionSO` por ID (prefijo "F" → "F0", "F1", ...). Ofrece búsqueda por ID, acceso al diccionario, propiedad `All` para iterar sin conversión, botones editor (PopulateFromBuffer, SyncAllIDs).

**S93:** Hereda protocolo de [[KeyedDatabaseSO]]; `IDPrefix = "F"`. Campo `items` en lugar de `furniture`.

## Campos Públicos

| Campo | Tipo | Acceso | Descripción |
|-------|------|--------|-------------|
| `items` | `Dictionary<string, FurnitureDefinitionSO>` | [OdinSerialize] private | Muebles indexados por ID |

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `GetByID(id)` [inherited] | `FurnitureDefinitionSO` | Busca por ID ("F0"…); null si no existe |
| `GetAllIDs()` [inherited] | `List<string>` | Lista de todos los IDs |

## Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `All` | `IEnumerable<FurnitureDefinitionSO>` | Iterador sobre `items.Values` |
| `Count` [inherited] | `int` | De KeyedDatabaseSO |

## Métodos Editor (Odin)

| Método | Descripción |
|--------|-------------|
| `PopulateFromBuffer()` [inherited] | Botón: arrastra assets, auto-asigna IDs |
| `SyncAllIDs()` [inherited] | Botón: renumera con prefijo "F" |

## CreateAssetMenu

**Menu path:** `RunRunSimulator/Databases/Furniture Database`

## Vinculado a

- [[Index/02 - Content & Databases]]
- [[Index/10 - Furniture & Building]]
- [[KeyedDatabaseSO]] — protocolo base

**Conexiones:** [[KeyedDatabaseSO]], [[FurnitureDefinitionSO]], [[FurnitureService]], [[StoreManager]]

