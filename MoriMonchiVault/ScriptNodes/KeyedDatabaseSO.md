---
tags: [scriptable-object, database, data]
---

# KeyedDatabaseSO.cs

**Ruta:** `Data/Databases/KeyedDatabaseSO.cs`

**Responsabilidad:** Base genérica abstracta `KeyedDatabaseSO<T>` para databases que indexan entidades por string ID. Define protocolo: abstract `Entries` (Dictionary<string, T>), `IDPrefix` (prefijo para asignación automática), y `SetEntryID()` para actualizar ID del asset. Métodos: `PopulateFromBuffer()` (editor: arrastra assets y asigna IDs), `SyncAllIDs()` (renumera todos los entries con prefijo), `GetByID(string id)` (búsqueda), `GetAllIDs()`, `Count`. Hereda `SerializedScriptableObject` para soporte Odin dictionaries.

**S93:** Extracción de base común de PartDatabaseSO, EquipmentDatabaseSO, ItemDatabaseSO, FurnitureDatabaseSO.

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `GetByID(string id)` | `T` | Busca asset por ID; null si no existe o id es empty |
| `GetAllIDs()` | `List<string>` | Lista de todos los IDs registrados |
| `Count` | `int` | Propiedad read-only, total de entries |

## Métodos Editor (Odin)

| Método | Descripción |
|--------|-------------|
| `PopulateFromBuffer()` | Botón: arrastra assets a `dropBuffer`, auto-asigna temp IDs, llama `SyncAllIDs()` |
| `SyncAllIDs()` | Botón: renumera todos los entries con `{IDPrefix}0`, `{IDPrefix}1`, etc. Actualiza cada asset con `SetEntryID()` |

## Protocolo Abstracto

```csharp
protected abstract Dictionary<string, T> Entries { get; }
protected abstract string IDPrefix { get; }
protected abstract void SetEntryID(T entry, string id);
protected virtual void OnPopulated(int added) { }
```

- `Entries` — diccionario que mapea ID → asset
- `IDPrefix` — prefijo para generación automática (ej. "part_" → "part_0", "part_1", ...)
- `SetEntryID()` — actualizar el campo ID del asset (ej. `entry.ID = id`)
- `OnPopulated()` — hook opcional tras PopulateFromBuffer (para post-procesamiento)

## Implementaciones Conocidas

- [[PartDatabaseSO]] — `IDPrefix = "part_"`; `Entries = parts` Dictionary
- [[EquipmentDatabaseSO]] — `IDPrefix = "equip_"`; `Entries = equipment`
- [[ItemDatabaseSO]] — `IDPrefix = "item_"`; `Entries = items`
- [[FurnitureDatabaseSO]] — `IDPrefix = "furn_"`; `Entries = items`

## Ciclo de Vida Editor

1. Usuario arrastra assets a `dropBuffer` (ListDrawerSettings habilitados: +/- botones visibles)
2. Click `Populate from Buffer` → `PopulateFromBuffer()`
3. Entries se añaden con temp IDs (`_tmp_{GUID}`)
4. `SyncAllIDs()` renumera todo
5. Cada asset recibe `SetEntryID(entry, newKey)` + `EditorUtility.SetDirty()`
6. Retorna mensaje: "Synced N IDs"

## Vinculado a

- [[Index/02 - Content & Databases]]

**Conexiones:** [[PartDatabaseSO]], [[EquipmentDatabaseSO]], [[ItemDatabaseSO]], [[FurnitureDatabaseSO]]

