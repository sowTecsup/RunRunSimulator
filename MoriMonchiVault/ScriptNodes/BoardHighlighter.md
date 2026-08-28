---
tags: [script, combat-prototype, visuals]
---

# BoardHighlighter.cs

**Ruta:** `CombatPrototype/BoardHighlighter.cs`

**Responsabilidad:** Pool de quads para visualizar celdas destacadas (targeting templates, intents, paths, landings, selections, spawn telegraph). Enum `HighlightKind` = Template/Intent/Path/Landing/Selection/Spawn. Cada tipo con color serializado. **S86 cambio:** `stackStep` (float) define Z-offset por prioridad de highlight — `Priority(kind)` retorna orden (Selection=0, Template=1, Intent=2, Path=3, Landing=4, Spawn=5 o similar), altura = 0.02f + Priority * stackStep. Esto evita Z-fight cuando múltiples highlights se solapan. Show/Clear/ClearAll orquestan el pool. GetPooledQuad/ReturnToPool gestiona reciclaje.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatBoard]], [[TargetingController]], [[EnemyTurnController]], [[CombatPrototypeManager]], [[NightSpawner]], [[CombatBoardBuilder]]
