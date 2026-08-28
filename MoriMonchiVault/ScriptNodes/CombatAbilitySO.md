---
tags: [script, combat-prototype, data]
---

# CombatAbilitySO.cs

**Ruta:** `CombatPrototype/CombatAbilitySO.cs`

**Responsabilidad:** SO plano que define una habilidad: ID, DisplayName, tipo (Movement/Attack), modo targeting (FreeCell/StraightLine/DirectionalTemplate/RangeBand/AirborneEnemy), Range, RangeMin, TemplateOffsets (patrones direccionales), PushDistance, **PushFromCenter** (nuevo S87: bool, si true el empuje es radial desde anclaje en lugar de en dirección de facing), LaunchesAirborne, SlamTargeted, SlamRange, IgnoresHeight, IgnoresObstacles, Landing (Stay/AtAnchor/BehindAnchor define dónde aterriza atacante tras ejecutar).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[PlayerUnitDefinitionSO]], [[AbilityTargeting]], [[ActionResolver]], [[TargetingController]]
