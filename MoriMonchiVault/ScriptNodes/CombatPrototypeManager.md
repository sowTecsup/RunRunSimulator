---
tags: [script, combat-prototype, orchestration]
---

# CombatPrototypeManager.cs

**Ruta:** `CombatPrototype/CombatPrototypeManager.cs`

**Responsabilidad:** Orquestador principal del combate prototipo S84+. Fases: **Setup** (despliegue nocturno, planta semilla, despliega dragones) → **Planning** (planificación choreography) → **Executing** (anima plan) → **Reacting** (reacciones enemigas sin cierre de ciclo) → **EnemyTurn** (enemigos atacan + cierre de ciclo, germinación check) → **Spawning** (oleadas entran saltando por bordes) → loop o victoria/derrota. **Ciclo de 3 turnos**: `cycleTurn` (1-3) incrementa tras Planning+Reacting; al llegar a `cycleLength` (3), pasa a EnemyTurn con ataque. `PlaceAt(cell)` en Setup: planta semilla (`SeedId`) o dragón (`DeployedCount`); valida celda libre. Enum `CombatPhase` = Planning/Executing/Reacting/EnemyTurn/Victory/Defeat/Setup/Spawning. Campos S88: `cycleLength` (3 default, tunable), `cycleTurn` (estado del ciclo). **API S88**: `CycleTurn` (turno actual en ciclo 1-3), `TurnsUntilEnemyAttack` (cycleLength - cycleTurn + 1, mínimo 1), `IsAbilitySpent(unitId, abilityIndex)` (comprobar gasto de poder con índice unitId*8+abilityIndex en `spentAbilities` HashSet), `HasAvailableAbility(unitId)` (¿dragón vivo con algún poder no gastado?), `AnyUsableAbility()` (¿hay al menos un dragón con poderes disponibles?). **ExecutePlan** permite plan vacío solo si NO hay poderes usables (`AnyUsableAbility()`). Germinación al cierre del turno del jugador antes del ataque enemigo (si `turnCounter >= germinationTurn`). **HasPendingReactions()** detecta unidades airborne o enemigos golpeados; salta fase Reacting vacía. Refuerzos y restauración de `spentAbilities` solo tras ataque enemigo del cierre de ciclo. `TurnLog` (lista `TurnLogEntry`) registra narrativa de turnos; evento `TurnLogChanged` notifica UI. Proyección de estado: `Projection` = clon de canonical tras plan sin ejecutar, para preview HUD. `viewPrefab` con jerarquía completa, `seedTicks`/`germinationTurn` tunables.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatBoardBuilder]], [[BoardLayoutSO]], [[TargetingController]], [[PlanExecutor]], [[EnemyTurnController]], [[CombatPrototypeHUD]], [[PlayerUnitDefinitionSO]], [[EnemyDefinitionSO]], [[NightSpawner]], [[SeedUnit]], [[CombatUnitView]], [[TurnLogEntry]], [[CombatSimState]], [[ResolutionAnimator]], [[ActionResolver]]
