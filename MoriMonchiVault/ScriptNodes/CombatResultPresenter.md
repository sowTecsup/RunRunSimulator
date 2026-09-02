---
tags: [script, ui, combat, presenter]
---

# CombatResultPresenter.cs

**Ruta:** `UI/CombatResultPresenter.cs`

**Responsabilidad:** Presenter de pantalla de resultado. Show(outcome, player, rival) renderiza victoria/derrota (rps-result--win/lose), score, y recompensa (material si ganó) o cooldown (mostrado con hora HH:mm si perdió). Card rps-result entra con animación rps-result--enter. Botones: Again (otra batalla), Close (panel). Emite `AgainRequested` / `CloseRequested`. Navegación por Move/Submit.

**Vinculado a:** [[Index/05 - UI System]], [[Index/21 - Combate v3 - Dragon RPS]]

**Conexiones:** [[CombatPanelUITK]], [[CombatOutcome]], [[CreatureDNA]]
