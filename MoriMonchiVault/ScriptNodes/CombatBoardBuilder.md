---
tags: [script, combat-prototype, presentation]
---

# CombatBoardBuilder.cs

**Ruta:** `Systems/CombatPrototype/CombatBoardBuilder.cs`

**Responsabilidad:** Construye representación 3D del tablero. **Cambios S82:** levelHeight serializado (en lugar de const); diccionario blocks {Vector2Int → Transform}; GetBlock(cell) para acceso. BuildCell usa MMWiggle (Feel) por bloque, salta huecos (InBounds check), tunables de amplitud/frecuencia. Colores alternados light/dark.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[BoardLayoutSO]], [[CombatBoard]], [[BoardImpactFeedback]], [[CombatCameraController]]
