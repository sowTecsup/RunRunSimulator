---
tags: [script, ui, combat, presenter]
---

# CombatPickPresenter.cs

**Ruta:** `UI/CombatPickPresenter.cs`

**Responsabilidad:** Presenter que construye lista de criaturas jugables. Cards rps-card muestran retrato via MonchiPortraitUI, nombre, estado. Elegibles muestran 3 potenciales (HornPotential·WingPotential·BackPotential); no elegibles muestran motivo (ocupada/cooldown/cansada). Emite `FightRequested(CreatureDNA)` / `CloseRequested`. Navegación por Move(dx), Submit(), selección resaltada por clase rps-card--selected.

**Vinculado a:** [[Index/05 - UI System]], [[Index/21 - Combate v3 - Dragon RPS]]

**Conexiones:** [[CombatPanelUITK]], [[DragonRpsGenes]], [[CreatureRegistrySO]], [[CombatTuningSO]]
