---
tags: [script, database, asset]
---

# EquipmentDatabaseSO.cs

**Ruta:** `Data/Databases/EquipmentDatabaseSO.cs`

**Responsabilidad:** Base de datos única: espejo de `PartDatabaseSO` para equipo. Indexa cada `EquipmentSO` por ID (`"EQ0"`, `"EQ1"`…). Resolver el equipo acoplado a un `CreatureDNA` (drag-drop en editor y en las grillas) siempre va aquí. Ofrece `PopulateFromBuffer` (drag-drop múltiples SOs → `SyncAllIDs` asigna IDs secuenciales) y getter estático `Editor` para que el editor de DNA pueda resolver IDs sin un `GameManager` vivo (ej: editando el asset del registro). En runtime, `GameManager` mantiene la instancia viva.

## Métodos públicos

| Método | Retorna | Propósito |
|--------|---------|----------|
| `GetByID(string id)` | `EquipmentSO` | Resuelve un item por ID ("EQ0"…); null si no existe. |
| `GetBySlot(EquipmentSlot slot)` | `List<EquipmentSO>` | Todos los items para un slot (Weapon/Armor/Amulet). |
| `GetAllIDs()` | `List<string>` | Lista de todos los IDs. |
| `Equipment` | `Dictionary<...>` | Acceso directo al dict (read-only). |

## Editor-only

| Método | Propósito |
|--------|----------|
| `PopulateFromBuffer()` | Arrastra varios `EquipmentSO` a `dropBuffer` → añade sin duplicados + llama `SyncAllIDs`. |
| `SyncAllIDs()` | Reordena dict, asigna IDs secuenciales "EQ0"/"EQ1"/…, marca SOs como dirty. |
| `Editor` (static property) | Busca la instancia en AssetDatabase; permite resolver IDs en editor sin `GameManager`. |

**Vinculado a:** [[Index/04 - Combat]] (sistema de modificadores)

**Conexiones:** [[EquipmentSO]], [[CreatureDNA]], [[GameManager]], [[CreatureGridView]]
