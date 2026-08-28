---
tags: [script, combat-prototype, testing]
---

# CombatAutoTester.cs

**Ruta:** `CombatPrototype/CombatAutoTester.cs`

**Responsabilidad:** Simulador headless determinista de partidas completas de combate S88. Clase estática pura. `MatchResult` struct agrupa métricas: Profile, Outcome, Turns, SeedTicksLost, DragonTicksLost, DragonsDead, EnemyAttacks, EnemyHitsOnSeed, EnemyHitsOnDragons, EnemiesSpawned, EnemiesKilled, ActionsUsed. `RunMatch(layout, loadout, enemyDefs, profile, rngSeed, seedTicks, germinationTurn, baseWaveSize, extraEveryWaves, maxTurns)` orquesta simulación: planta semilla (centro), despliega dragones con `NightWaves.FindSpawnCells()`, entra loop de turnos (hasta maxTurns o victoria/derrota). **Ciclo S88**: `cycleTurn` (1-3) incrementa cada turno hasta 3. Cada turno de Planning: elige acciones del dragón según **profile** ("pasivo"=no, "distraído"=random, "defensor"=vs enemigos amenazantes, etc.), ejecuta con `ActionResolver.ResolveBeat()`, gasto de poder por `unitId * 8 + abilityIndex` en HashSet `spent`. Si `cycleTurn < 3`: resuelve reacciones con `ActionResolver.ResolveEnemyReactions()` (sin ataque). Si `cycleTurn >= 3`: resuelve turno enemigo con `ActionResolver.ResolveEnemyTurn()` (ataque + movimientos), limpia `spent`, spawna oleadas con `NightWaves.FindEdgeSpawnCells()`, restaura amigos. Chequea germinación con `ActionResolver.ResolveGermination()` al final. Turno vacío (0 acciones) si no hay poderes disponibles.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[NightWaves]], [[ActionResolver]], [[AbilityTargeting]], [[CombatSimState]], [[CombatBoard]], [[BoardLayoutSO]], [[PlayerUnitDefinitionSO]], [[EnemyDefinitionSO]]
