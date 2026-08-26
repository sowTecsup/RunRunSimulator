---
tags: [script, combat-prototype, visuals]
---

# BoardHighlighter.cs

**Ruta:** `Systems/CombatPrototype/BoardHighlighter.cs`

**Responsabilidad:** Pool de quads para visualizar celdas destacadas (targeting templates, intents, paths, landings, selections). Soporta 5 tipos (Template/Intent/Path/Landing/Selection) cada uno con color propio serializado. **Cambios S83:** HighlightKind enum gana `Selection`. Color selectionColor serializado (blanco 1, 1, 1 alpha 0.65). GetColor retorna selectionColor para Selection.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatBoard]], [[TargetingController]], [[EnemyTurnController]], [[CombatPrototypeManager]]
