---
tags: [script, ui, combat]
---

# CombatOnlineTabPresenter.cs

**Ruta:** `UI/CombatOnlineTabPresenter.cs`

**Responsabilidad (S54):** Tab 0 "Batalla Online" — seleccionar criatura elegible (viva, no ocupada, bajo MaxFightCount), ver stats/partes via CombatStats, enqueue a combate async (Instant O Timer via AsyncCombatService). Implementa `ITabPresenter` (S54 renombre de ICombatTabPresenter). Dos sub-focus: Lista (scroll, highlight) ↔ Acciones (Instant/Timer buttons). Navigate: h/v en lista, h left/right en acciones, v down entra acciones, v up sale a lista. Submit en lista: entra acciones; Submit en acciones: enqueue. Cancel: sale a tabbar. Rebuild: itera elegibles (Where: !IsDead && !IsBusy && FightCount < MaxFights), renderiza cards con nombre + stats (CON ATK SPD DEF LCK EVA) + contador. Centro: muestra imagen (backgroundColor BaseColor), nombre, stats, 4 partes (BodyShape/Arm/Eye/Mouth) con swatch+nombre+set+tier. Callback onEnqueued() notifica al core para UI updates.

**Vinculado a:** [[Index/05 - UI System]], [[Index/13 - Combat Design Direction]]

**Conexiones:** [[ITabPresenter]], [[CombatPanelUITK]], [[AsyncCombatService]], [[CombatStats]], [[CreatureDatabaseSO]]
