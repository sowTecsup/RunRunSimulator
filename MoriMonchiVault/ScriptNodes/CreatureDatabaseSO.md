---
tags: [script, genetics]
---

# CreatureDatabaseSO.cs

**Ruta:** `Data/Databases/CreatureDatabaseSO.cs`

**Responsabilidad:** Database maestra orquestadora de partes genéticas. Referencia a 5 sub-bases de datos (BodyShapes, Horns, Backs, Wings, Faces), cada una con su propia `PartDatabaseSO<T>`. Getters `GetBodyShape()`, `GetHorn()`, `GetBack()`, `GetWing()`, `GetFace()` para resolver IDs. Método `ValidateAllDatabases()` detecta duplicados de IDs across all sub-DBs.

## Estructura

5 campos requeridos serializados:
- `BodyShapeDatabaseSO BodyShapes` — DB de cuerpos (prefijo "BS")
- `HornDatabaseSO Horns` — DB de cuernos (prefijo "H")
- `BackDatabaseSO Backs` — DB de dorsos (prefijo "BK")
- `WingDatabaseSO Wings` — DB de alas (prefijo "W")
- `FaceDatabaseSO Faces` — DB de caras (prefijo "FC")

## Métodos Públicos

| Método | Retorna | Descripción |
|--------|---------|-------------|
| `GetBodyShape(string id)` | `BodyShapePart` | Resuelve ID → BodyShapePart |
| `GetHorn(string id)` | `HornPart` | Resuelve ID → HornPart |
| `GetBack(string id)` | `BackPart` | Resuelve ID → BackPart |
| `GetWing(string id)` | `WingPart` | Resuelve ID → WingPart |
| `GetFace(string id)` | `FacePart` | Resuelve ID → FacePart |
| `ValidateAllDatabases()` | `void` | Button editor que detecta duplicados de IDs |

## Cambios en S75

- **S75 ACTUALIZADO:** Cambio de 4 a 5 partes: reemplazó ArmDatabaseSO/EyeDatabaseSO/MouthDatabaseSO con HornDatabaseSO/BackDatabaseSO/WingDatabaseSO, + FaceDatabaseSO.
- Getters correspondientes actualizados.
- `ValidateAllDatabases()` valida los 5 sub-DBs.

## Vinculado a

- [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[BodyShapeDatabaseSO]], [[HornDatabaseSO]], [[BackDatabaseSO]], [[WingDatabaseSO]], [[FaceDatabaseSO]], [[PartDatabaseSO]], [[CreatureDNA]], [[CreatureGenerator]], [[BreedingService]]
