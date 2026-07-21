---
tags: [script, ui, combat]
---

# CombatResultsTabPresenter.cs

**Ruta:** `UI/CombatResultsTabPresenter.cs`

**Responsabilidad (S54):** Tab 1 "Resultados" — lista de criaturas en cola QueuedForCombat (pendientes de server tick). Implementa `ITabPresenter` (S54 renombre de ICombatTabPresenter). Foco: botón Refresh (arriba) + filas resultado (por nombre alfabético). Botón Refresh: `PollResultsAsync()` (checkea nube, aplica resultados → cada uno dispara OnCombatLogged, criaturas salen de la cola a CombatHistory). Cada fila muestra nombre + timestamp encolado + countdown compartido a next hour :00 UTC (ambos se actualizan en `Tick()` llamado por CombatPanelUITK solo si tab 1 visible). Navigate: h/v entre botón + filas, salida por arriba/abajo. Submit en botón: refresh. Focus order: button first, then rows.

**Vinculado a:** [[Index/05 - UI System]], [[Index/13 - Combat Design Direction]]

**Conexiones:** [[ITabPresenter]], [[CombatPanelUITK]], [[AsyncCombatService]], [[GameEvents]]
