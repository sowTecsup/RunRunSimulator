---
tags: [script, combat-prototype, data]
---

# EnemyDefinitionSO.cs

**Ruta:** `Systems/CombatPrototype/EnemyDefinitionSO.cs`

**Responsabilidad:** SO de definición de enemigo: ID, nombre, GuardTicks/FinisherTicks (presupuesto defensivo), Pattern (ChaseMelee/RangedLine), AttackRange (para RangedLine), **Nuevo:** MoveOffsets (array de offsets ajedrez en espacio local facing=+x), BriefLines, VisualPrefab, Tint. **Cambios S82:** pierde MoveRange, PreferredMin/Max, ReactionDistance; gana MoveOffsets (patrón ajedrez de movimiento post-golpe bloqueado).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[EnemyUnit]], [[EnemyBrain]], [[ActionResolver]], [[CombatPrototypeManager]]
