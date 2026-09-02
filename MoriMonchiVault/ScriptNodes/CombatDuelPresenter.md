---
tags: [script, ui, combat, presenter]
---

# CombatDuelPresenter.cs

**Ruta:** `UI/CombatDuelPresenter.cs`

**Responsabilidad:** Presenter de batalla ronda a ronda. Construye dos lados por código: retratos, nombres, potencias (power row), cartas restantes (intact row), pips de golpes recibidos. Mano de botones rps-action (uno por tipo: --horns/--wings/--back). Emite `CardPlayed(handIndex)`. Contiene `RpsTriangleElement` que muestra relaciones Cuernos>Alas>Espalda. `Describe(DragonRpsRoundInfo)` localiza log ronda (espejo/victoria/derrota/reshuffle). Fila log flashea al actualizar.

**Vinculado a:** [[Index/05 - UI System]], [[Index/21 - Combate v3 - Dragon RPS]]

**Conexiones:** [[CombatPanelUITK]], [[DragonRpsSession]], [[DragonRpsRoundInfo]], [[RpsTriangleElement]], [[DragonRpsRules]]
