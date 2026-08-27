---
tags: [script, combat-prototype, visuals]
---

# BoardHighlighter.cs

**Ruta:** `CombatPrototype/BoardHighlighter.cs`

**Responsabilidad:** Pool de quads para visualizar celdas destacadas (targeting templates, intents, paths, landings, selections, **spawn telegraph S84**). Enum `HighlightKind` incluye Template/Intent/Path/Landing/Selection/**Spawn**. Cada tipo con color propio serializado. **S84:** Spawn color tunable (spawnColor, púrpura por defecto). GetColor retorna color adecuado para cada HighlightKind. Show/Clear/ClearAll orquestan el pool.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatBoard]], [[TargetingController]], [[EnemyTurnController]], [[CombatPrototypeManager]], [[NightSpawner]]
