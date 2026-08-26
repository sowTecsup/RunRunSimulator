---
tags: [script, combat-prototype, geometry]
---

# CombatBoard.cs

**Ruta:** `Systems/CombatPrototype/CombatBoard.cs`

**Responsabilidad:** Mapa de combate (lógica pura): anchos/profundidad, elevaciones, huecos. Constructor recibe BoardLayoutSO y levelHeight (readonly, instancia). **Cambios S82:** LevelHeight pasa de const a parámetro de instancia; InBounds devuelve false en huecos ('.' en HeightRows). GetElevation retorna 0 fuera de bounds.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[BoardLayoutSO]], [[CombatBoardBuilder]], [[CombatSimState]]
