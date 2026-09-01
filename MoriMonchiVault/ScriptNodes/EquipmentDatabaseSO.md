---
tags: [scriptable-object, database, equipment]
---

# EquipmentDatabaseSO.cs

**Ruta:** `Data/Databases/EquipmentDatabaseSO.cs`

**Responsabilidad:** Base de datos única de equipo. Hereda de [[KeyedDatabaseSO]]. Indexa cada `EquipmentSO` por ID (prefijo "EQ" → "EQ0", "EQ1", ...). Resolver equipo acoplado a un `CreatureDNA` siempre va aquí (drag-drop editor, grillas). Ofrece búsqueda por ID, acceso al diccionario, botones editor (PopulateFromBuffer, SyncAllIDs). Propiedad estática `Editor` (solo en editor) permite resolver IDs sin GameManager vivo (ej: editor de DNA).

**S93:** Hereda protocolo de [[KeyedDatabaseSO]]; `IDPrefix = "EQ"`.

## Campos Públicos

| Campo | Tipo | Acceso | Descripción |
|-------|------|--------|-------------|
| `equipment` | `Dictionary<string, EquipmentSO>` | [OdinSerialize] private | Equipo indexado por ID |

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `GetByID(id)` [inherited] | `EquipmentSO` | Busca por ID ("EQ0"…); null si no existe |
| `GetAllIDs()` [inherited] | `List<string>` | Lista de todos los IDs |

## Propiedades

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `Equipment` | `Dictionary<string, EquipmentSO>` | Diccionario completo |
| `EquipmentCount` | `int` | Total de items |
| `Count` [inherited] | `int` | De KeyedDatabaseSO |
| `Editor` [static] | `EquipmentDatabaseSO` | Busca instancia en AssetDatabase (solo editor) |

## Métodos Editor (Odin)

| Método | Descripción |
|--------|-------------|
| `PopulateFromBuffer()` [inherited] | Botón: arrastra assets, auto-asigna IDs |
| `SyncAllIDs()` [inherited] | Botón: renumera con prefijo "EQ" |

## CreateAssetMenu

**Menu path:** `RunRunSimulator/Databases/Equipment Database`

## Vinculado a

- [[Index/02 - Content & Databases]]
- [[KeyedDatabaseSO]] — protocolo base

**Conexiones:** [[KeyedDatabaseSO]], [[EquipmentSO]], [[CreatureDNA]], [[GameManager]]

