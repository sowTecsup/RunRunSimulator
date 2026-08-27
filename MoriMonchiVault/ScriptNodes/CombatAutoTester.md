---
tags: [script, combat, testing]
---

# CombatAutoTester.cs

**Ruta:** `CombatPrototype/CombatAutoTester.cs`

**Responsabilidad:** Simulador headless determinista de partidas completas de combate. Clase estática pura. `MatchResult` struct agrupa métricas: outcome, turnos, ticks perdidos (semilla/dragones), muertes, ataques enemigos, aciertos, spawns, acciones usadas. `RunMatch(layout, loadout, enemyDefs, profile, rngSeed, seedTicks, germinationTurn, baseWaveSize, extraEveryWaves, maxTurns)` orquesta una simulación completa: planta semilla (centro del tablero), despliega dragones en anillos cerca de semilla con `NightWaves.FindSpawnCells()`, entra en loop de turnos (hasta maxTurns o victoria/derrota), cada turno elige acción del dragón según **profile** ("pasivo" = no actúa, "distraído" = aleatoria, "defensor" = prioriza enemigos cerca de semilla), ejecuta con `ActionResolver`, resuelve turno enemigo, spawna oleadas por bordes con `NightWaves.FindEdgeSpawnCells()`, chequea germinación. Profiles son parámetro testeable de legibilidad IA/comportamiento.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[NightWaves]], [[ActionResolver]], [[AbilityTargeting]], [[CombatSimState]], [[EnemyBrain]], [[CombatAutoTester.MatchResult]]
