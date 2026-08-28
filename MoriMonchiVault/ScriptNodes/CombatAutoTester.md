---
tags: [script, combat-prototype, testing]
---

# CombatAutoTester.cs

**Ruta:** `CombatPrototype/CombatAutoTester.cs`

**Responsabilidad:** Simulador headless determinista de partidas completas de combate. Clase estática pura. `MatchResult` struct agrupa métricas: Profile, Outcome, Turns, SeedTicksLost, DragonTicksLost, DragonsDead, EnemyAttacks, EnemyHitsOnSeed, EnemyHitsOnDragons, EnemiesSpawned, EnemiesKilled, ActionsUsed. `RunMatch(layout, loadout, enemyDefs, profile, rngSeed, seedTicks, germinationTurn, baseWaveSize, extraEveryWaves, maxTurns)` orquesta simulación completa: planta semilla (centro tablero), despliega dragones cerca de semilla con `NightWaves.FindSpawnCells()`, entra loop de turnos (hasta maxTurns o victoria/derrota), cada turno elige acción del dragón según **profile** ("pasivo"=no actúa, "distraído"=aleatoria, "defensor"=prioriza enemigos cerca de semilla, etc.), ejecuta con `ActionResolver.ResolveBeat()`, resuelve turno enemigo, spawna oleadas por bordes con `NightWaves.FindEdgeSpawnCells()`, chequea germinación con `ActionResolver.ResolveGermination()`. Profiles son parámetro testeable de legibilidad IA.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[NightWaves]], [[ActionResolver]], [[AbilityTargeting]], [[CombatSimState]], [[CombatBoard]], [[BoardLayoutSO]], [[PlayerUnitDefinitionSO]], [[EnemyDefinitionSO]]
