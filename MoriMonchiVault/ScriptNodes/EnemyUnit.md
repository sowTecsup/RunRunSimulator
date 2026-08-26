---
tags: [script, combat-prototype, data]
---

# EnemyUnit.cs

**Ruta:** `Systems/CombatPrototype/EnemyUnit.cs`

**Responsabilidad:** Unidad enemiga en estado de combate. Extiende CombatUnit. **Cambios S82:** pierde HasReacted, WasHitThisBeat; gana Facing (Vector2Int dirección del enemigo) y WasHitThisTurn (flag de onda actual). Referencia a Definition (EnemyDefinitionSO) e Intent (EnemyIntent computed).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatUnit]], [[EnemyDefinitionSO]], [[EnemyIntent]], [[CombatSimState]]
