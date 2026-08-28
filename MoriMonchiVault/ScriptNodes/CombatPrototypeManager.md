---
tags: [script, combat-prototype, orchestration]
---

# CombatPrototypeManager.cs

**Ruta:** `CombatPrototype/CombatPrototypeManager.cs`

**Responsabilidad:** Orquestador principal del combate prototipo S84+. Fases: **Setup** (despliegue nocturno, planta semilla, despliega dragones) → **Planning** (planificación choreography) → **Executing** (anima plan) → **EnemyTurn** (enemigos atacan, turno + germinación check) → **Spawning** (oleadas entran saltando por bordes) → loop o victoria/derrota. `PlaceAt(cell)` en Setup: planta semilla (`SeedId`) o dragón (`DeployedCount`); valida celda libre. Enum `CombatPhase` = Planning/Executing/EnemyTurn/Victory/Defeat/Setup/Spawning (S85). Campos S86+S87: `viewPrefab` (prefab UnitView con jerarquía completa), `seedTicks` (vida semilla), `germinationTurn` (turno victoria), `spawnJumpDuration` y `spawnJumpFromCells` (tunables salto entrada). `RunSpawnPhase()` orquesta entrada oleadas: `PlaySpawnWave()` anima cada enemigo saltando desde borde hacia dentro, ShakeAt() al aterrizaje. `TurnLog` (lista `TurnLogEntry`) registra narrativa de turnos; evento `TurnLogChanged` notifica UI. Proyección de estado: `Projection` = clon de canonical tras plan sin ejecutar, para preview HUD.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatBoardBuilder]], [[BoardLayoutSO]], [[TargetingController]], [[PlanExecutor]], [[EnemyTurnController]], [[CombatPrototypeHUD]], [[PlayerUnitDefinitionSO]], [[EnemyDefinitionSO]], [[NightSpawner]], [[SeedUnit]], [[CombatUnitView]], [[TurnLogEntry]], [[CombatSimState]], [[ResolutionAnimator]]
