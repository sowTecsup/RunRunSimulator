---
tags: [script, combat-prototype, data]
---

# BoardLayoutSO.cs

**Ruta:** `Systems/CombatPrototype/BoardLayoutSO.cs`

**Responsabilidad:** SO que define layout del tablero: HeightRows (grid de altura en dígitos) y SpawnRows (grid de spawns). **Novedades S82:** struct EnemySpawn {Cell, Facing}; GetEnemySpawnsWithFacing() reemplaza GetEnemySpawns(); chars de spawn: 'P' (player), '>' '<' '^' 'v' (enemigo con dirección), 'E' (enemigo default +x); IsHole(x,z) → char '.' en HeightRows marca celda-hueco (out of bounds).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatBoard]], [[CombatPrototypeManager]], [[CombatBoardBuilder]]
