---
tags: [script, combat]
---

# CombatVisualEvents.cs

**Ruta:** `Systems/CombatVisualizer/CombatVisualEvents.cs`

**Responsabilidad:** Bus de eventos estático del visualizer de combate (separado de `GameEvents`). Define los DTOs y los eventos por fase.

**DTOs:** `CombatVisualSide` (A/B), `CombatVisualContext` (DNAs, HP máx, slots, total de turnos), `CombatVisualHit` (atacante/defensor/daño/crit), `CombatVisualLogKind` (Versus/Hit/Crit/Death/Result), `CombatVisualLogLine` (texto rich-text + Kind) y `CombatVisualPanelState` (snapshot completo del panel: turno, total, log[], fin, empate, ganador, flags de control IsAuto/CanForward/CanBack, Speed).

**Dos familias de eventos:**
- **Granulares (juice, para los hooks Feel):** `OnVisualCombatStart/End`, `OnTurnStart/End`, `OnAttack`, `OnHit`, `OnCrit`, `OnDead`, `OnLog`. Solo en forward.
- **De estado (para la UI):** `OnPanelState` (snapshot, reconstruye el panel; sirve para forward y rewind) y `OnHpChanged` (barras + hook de HP).

**Vinculado a:** [[Index/03 - Combat]]

**Conexiones:** [[CombatVisualizerService]], [[CombatVisualHooks]], [[CombatVisualizerPanelUITK]], [[MoriMonchiCombatVisualizerUITK]], [[CombatRecord]], [[CombatTurn]], [[CreatureDNA]]
