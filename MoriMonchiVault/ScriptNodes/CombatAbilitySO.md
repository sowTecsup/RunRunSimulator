---
tags: [script, combat-prototype, data]
---

# CombatAbilitySO.cs

**Ruta:** `Systems/CombatPrototype/CombatAbilitySO.cs`

**Responsabilidad:** SO plano que define una habilidad: ID, nombre, tipo (Movement/Attack), modo targeting (FreeCell/StraightLine/DirectionalTemplate/RangeBand/AirborneEnemy), rango, offsets de plantilla, efectos (push/launch/slam), y nuevo campo **Landing** que define dónde aterriza el dragón tras ejecutar (Stay=posición actual, AtAnchor=en el anclaje elegido, BehindAnchor=detrás del anclaje).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[PlayerUnitDefinitionSO]], [[AbilityTargeting]], [[ActionResolver]]
