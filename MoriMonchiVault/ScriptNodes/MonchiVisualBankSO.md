---
tags: [script, visual, database]
---

# MonchiVisualBankSO.cs

**Ruta:** `Data/Databases/MonchiVisualBankSO.cs`

**Responsabilidad:** Banco visual centralizado del modelo Suriyun. Mantiene la lista de cuerpos FBX prefabricados (`bodies` list) con posibilidad de sobrescrituras por BodyShape ID (`bodyOverrides` dict), el AnimatorController compartido, la lista de materiales gema para brillantes, y referencia al MoodSet. `GetBody(bodyShapeId)` devuelve determinísticamente (hash FNV-1a % count) el cuerpo correspondiente al BodyShapeID, priorizando overrides. `GetGem(uniqueId)` devuelve el material gema usando el mismo hash determinístico sobre uniqueID. Usa `StableHash()` interna para garantizar consistencia en replay/red.

**Vinculado a:** [[Index/02 - Genetics & Breeding]], [[Index/10 - Visualization]]

**Conexiones:** [[GameManager]], [[MonchiVisualizer]], [[MonchiMoodSetSO]], [[FurTypeDatabaseSO]]
