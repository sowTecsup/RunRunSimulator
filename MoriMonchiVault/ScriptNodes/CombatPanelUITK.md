---
tags: [script, ui, combat, uitk]
---

# CombatPanelUITK.cs

**Ruta:** `UI/CombatPanelUITK.cs`

**Responsabilidad:** MonoBehaviour IUINavigable que orquesta la UI de combate Dragon RPS. UIPanelType = 4. Estados: Pick (seleccionar criatura jugadora) → Duel (batalla ronda a ronda) → Result (resultado). Dueño de `DragonRpsSession`. Ciclo: ShowPick() → StartDuel(elegida) → PlayCard(handIndex cada turno) → FinishDuel() → Resolve + Show(outcome). Entrada/salida vía OnPanelSet/OnPanelToggle. Cancel en Duel desuscribe sin premios ni cooldown.

**Vinculado a:** [[Index/05 - UI System]], [[Index/21 - Combate v3 - Dragon RPS]]

**Conexiones:** [[CombatPickPresenter]], [[CombatDuelPresenter]], [[CombatResultPresenter]], [[DragonRpsSession]], [[DragonRpsService]], [[UIManager]], [[GameManager]]
