---
tags: [script, combat, controller]
---

# NightSpawner.cs

**Ruta:** `CombatPrototype/NightSpawner.cs`

**Responsabilidad:** Orquesta las oleadas de enemigos. Mantiene una cola `pendingWave` (lista de `EnemySpawn` con celda + orientación). `ResetForEncounter()` limpia el estado (waveNumber=0, pending, markers). `PrepareNextWave(canonical, seedCell)` incremente el número de oleada, calcula tamaño con `NightWaves`, halla celdas de borde, computa facing hacia semilla con `AbilityTargeting.DominantCardinal()`, llama `PaintTelegraph()` (muestra `HighlightKind.Spawn` + crea markers TMP 3D con posición, rotación hacia cámara, color/fuente serializados en S85). `ConsumeWave()` toma la pending, adapta celdas si fueron ocupadas, retorna lista y limpia estado. Telegrafo visual es tunable: `markerColor`, `markerText`, `markerFontSize`, `markerHeight`.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatPrototypeManager]], [[NightWaves]], [[AbilityTargeting]], [[BoardHighlighter]]
