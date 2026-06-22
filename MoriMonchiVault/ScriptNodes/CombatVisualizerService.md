---
tags: [script, combat]
---

# CombatVisualizerService.cs

**Ruta:** `Systems/CombatVisualizer/CombatVisualizerService.cs`

**Responsabilidad:** Singleton que reproduce visualmente un `CombatRecord` completo. Instancia dos `MoriMonchiVisualizer` en slots A/B, itera turno a turno disparando `CombatVisualEvents`, sincroniza timings (windup, impacto, entre turnos). Dueno de la vida/destruccion de los combatientes visuales.

**Vinculado a:** [[Index/03 - Combat]]

**Conexiones:** [[CombatVisualEvents]], [[CombatRecord]], [[CombatTurn]], [[CreatureDNA]], [[CreatureDatabaseSO]], [[PartVisualBankSO]], [[FurTypeDatabaseSO]], [[MoriMonchiVisualizer]], [[CombatService]]
