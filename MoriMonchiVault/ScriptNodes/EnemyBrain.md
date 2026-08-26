---
tags: [script, combat-prototype, logic]
---

# EnemyBrain.cs

**Ruta:** `Systems/CombatPrototype/EnemyBrain.cs`

**Responsabilidad:** Lógica pura de IA enemiga. ComputeIntent() selecciona target (cercano) y ejecuta patrón (ChaseMelee: greedy Manhattan → ataque si adyacente; RangedLine: greedy Alignment → ataque lineal si alineado).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[EnemyUnit]], [[EnemyIntent]], [[CombatSimState]], [[AbilityTargeting]], [[EnemyDefinitionSO]]
