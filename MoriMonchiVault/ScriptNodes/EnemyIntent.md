---
tags: [script, combat-prototype, data]
---

# EnemyIntent.cs

**Ruta:** `Systems/CombatPrototype/EnemyIntent.cs`

**Responsabilidad:** Intención de ataque del enemigo este turno. **Cambios S82:** pierde MoveSteps; gana AttackDirection (Vector2Int del patrón) y AttackOffsets (array de offsets rotativos en espacio local). HasAttack property. GetAttackCells computa celdas de impacto desde posición del enemigo. RotateOffset auxiliar (copia de AbilityTargeting).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[EnemyUnit]], [[EnemyBrain]], [[ActionResolver]]
