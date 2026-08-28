---
tags: [script, combat-prototype, orchestration]
---

# NightSpawner.cs

**Ruta:** `CombatPrototype/NightSpawner.cs`

**Responsabilidad:** Orquesta las oleadas de enemigos que entran por bordes. Mantiene cola `pendingWave` (lista de `EnemySpawn` con celda + orientación). `ResetForEncounter()` limpia estado (waveNumber=0, pending, markers). `PrepareNextWave(canonical, seedCell)` incrementa número de oleada, calcula tamaño con `NightWaves.WaveSize()`, halla celdas de borde con `NightWaves.FindEdgeSpawnCells()`, computa facing hacia semilla con `AbilityTargeting.DominantCardinal()`, llama `PaintTelegraph()` (muestra `HighlightKind.Spawn` en highlighter + crea markers TMP 3D con texto "×", posición, rotación hacia cámara via WorldLabelBillboard). `ConsumeWave()` toma pending, adapta celdas si fueron ocupadas, retorna lista consumida y limpia estado. Tunables: `baseWaveSize`, `extraEveryWaves`, `markerColor`, `markerText`, `markerFontSize`, `markerHeight`.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatPrototypeManager]], [[NightWaves]], [[AbilityTargeting]], [[BoardHighlighter]], [[WorldLabelBillboard]]
