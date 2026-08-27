---
tags: [script, combat-prototype, orchestration]
---

# CombatPrototypeManager.cs

**Ruta:** `CombatPrototype/CombatPrototypeManager.cs`

**Responsabilidad:** Orquestador principal del combate prototipo S84+. Fases: **Setup** (despliegue nocturno, planta semilla, despliega dragones) → **Planning** → **Executing** → **EnemyTurn** → **Spawning** (entrada de refuerzos por salto en arco) → (loop a Planning o victoria/derrota). `PlaceAt(cell)` en Setup coloca semilla o dragón; `SeedId` y `DeployedCount` rastrean estado. Enum `CombatPhase` incluye Setup y Spawning (S85). Campos serializados S85: `viewPrefab` (instancia `CombatUnitView` con jerarquía prefab UnitView.prefab), `seedTicks` (vida del objetivo), `germinationTurn` (turno de victoria), `spawnJumpDuration` y `spawnJumpFromCells` (tunables de animación de spawn). `RunSpawnPhase()` orquesta entrada de oleadas (S85): `PlaySpawnWave()` anima cada enemigo saltando desde borde hacia dentro, `ShakeAt()` al aterrizaje. `TurnLog` (lista de `TurnLogEntry`) registra narrativa; evento `TurnLogChanged` notifica a `TurnLogPanel`.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatBoardBuilder]], [[BoardLayoutSO]], [[TargetingController]], [[PlanExecutor]], [[EnemyTurnController]], [[CombatPrototypeHUD]], [[PlayerUnitDefinitionSO]], [[EnemyDefinitionSO]], [[NightSpawner]], [[SeedUnit]], [[CombatUnitView]], [[TurnLogEntry]]
