---
tags: [script, ui, interface]
---

# ITabPresenter.cs

**Ruta:** `UI/ITabPresenter.cs`

**Responsabilidad (S54):** Contrato polimórfico generalizado para presenters de tabs. Reemplaza `ICombatTabPresenter.cs` (eliminado S54 — ver [[ICombatTabPresenter]]). Métodos: `Enter()` (reset foco interior), `Navigate(h,v):bool` (retorna false = exit a tab bar), `Submit()`, `Cancel():bool` (retorna false = exit a tab bar), `ClearFocus()`, `Rebuild()` (sync data+UI), `Teardown()` (cleanup callbacks). Los presenters son clases planas sin estado excepto el de UI; todos reciben `Func<CreatureRegistrySO>` para evitar cacheo de registry.

**Implementadores:**
- `CombatOnlineTabPresenter` (Tab 0 Batalla Online) — implementa `ITabPresenter`
- `CombatResultsTabPresenter` (Tab 1 Resultados) — implementa `ITabPresenter`
- `CombatHistoryTabPresenter` (Tab 2 Historial) — implementa `ITabPresenter`
- `BreedingBreedTabPresenter` (Tab 0 Criar) — implementa `ITabPresenter` + campo público `Busy` (async breed en vuelo)
- `BreedingEggsTabPresenter` (Tab 1 Incubando) — implementa `ITabPresenter` + método público `Tick()` (throttled 1s, cuenta atrás)

**Vinculado a:** [[Index/05 - UI System]], [[Index/13 - Combat Design Direction]]

**Conexiones:** [[CombatOnlineTabPresenter]], [[CombatResultsTabPresenter]], [[CombatHistoryTabPresenter]], [[BreedingBreedTabPresenter]], [[BreedingEggsTabPresenter]], [[CombatPanelUITK]], [[BreedingPanelUITK]]
