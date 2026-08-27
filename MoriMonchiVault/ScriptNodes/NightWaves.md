---
tags: [script, combat, utility]
---

# NightWaves.cs

**Ruta:** `CombatPrototype/NightWaves.cs`

**Responsabilidad:** Clase estática pura (sin estado). Tres métodos: `WaveSize(waveNumber, baseWaveSize, extraEveryWaves)` calcula el tamaño de una oleada (`baseSize + (n-1)/extraEveryWaves`). `FindSpawnCells(state, seedCell, count, startRadius, exclude)` halla celdas libres en anillos Chebyshev concéntricos desde `startRadius` (usada solo por `CombatAutoTester.FindSpawnCells()` para despliegue inicial). `FindEdgeSpawnCells(state, seedCell, count, exclude)` halla celdas **de borde** (que tienen un vecino cardinal fuera de InBounds), devuelve hasta `count` ordenadas por distancia Chebyshev a la semilla, con desempate determinista (Y, X). `EdgeOutwardDirection(board, cell)` retorna el cardinal que apunta hacia fuera del borde de esa celda (o zero si no es borde).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[NightSpawner]], [[CombatAutoTester]], [[AbilityTargeting]], [[CombatSimState]]
