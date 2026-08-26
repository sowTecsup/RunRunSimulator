---
tags: [script, combat-prototype, logic]
---

# EnemyBrain.cs

**Ruta:** `Systems/CombatPrototype/EnemyBrain.cs`

**Responsabilidad:** Computador de intención de ataque. ComputeIntent toma EnemyUnit y retorna EnemyIntent con AttackDirection = enemy.Facing y AttackOffsets basados en Pattern (ChaseMelee → [1,0], RangedLine → línea hasta AttackRange). Lógica pura, sin estado.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[EnemyUnit]], [[EnemyIntent]], [[EnemyDefinitionSO]]
