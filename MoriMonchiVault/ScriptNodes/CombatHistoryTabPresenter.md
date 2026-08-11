---
tags: [script, ui, combat]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatHistoryTabPresenter.cs

**Ruta:** `UI/CombatHistoryTabPresenter.cs`

**Responsabilidad (S54):** Tab 2 "Historial" — replay de todos los combates (aplanado: cada criatura·record = row, ordenado por date DESC). Implementa `ITabPresenter` (S54 renombre de ICombatTabPresenter). Dropdown filter por criatura (Todos + names de criaturas con history). La lista muestra nombre + oponente + resultado (Ganó/Perdió/Murió/Empate) + timestamp local. Detalle: cuando se selecciona un row, panel derecho muestra oponente completo, fecha, turn log (R1 attacker→defender damage crit), outcome largo + ¿evolucionó? + ¿murió?. Botón replay (CanReplay → CombatReplayRequest.Request). Navigate: h/v en lista + dropdown, Submit = mostrar detalle. Flattened parallel lists: historyItems (todos) / historyRendered (filtrados mostrados).

**Vinculado a:** [[Index/05 - UI System]], [[Index/13 - Combat Design Direction]]

**Conexiones:** [[ITabPresenter]], [[CombatPanelUITK]], [[CombatReplayRequest]], [[CombatRecord]]
