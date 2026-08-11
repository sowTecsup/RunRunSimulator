---
tags: [script, ui, combat]
---

> ⚰️ **RETIRADO-S75** — script borrado del proyecto en la demolición del combate (2026-08-11). Nodo conservado como referencia histórica.

# CombatPanelUITK.cs

**Ruta:** `UI/CombatPanelUITK.cs`

**Responsabilidad (S53 — Fase 7 piloto composición):** Panel modal de combate 3 pestañas (UI Toolkit). Núcleo MonoBehaviour delgado que orquesta: Tab 0 "Batalla Online" (seleccionar/enqueue), Tab 1 "Resultados" (cola + countdown), Tab 2 "Historial" (replay). Compone `ITabPresenter` (contrato polimórfico — renombrado en S54 de ICombatTabPresenter) + 3 presenters concretos: `CombatOnlineTabPresenter`, `CombatResultsTabPresenter`, `CombatHistoryTabPresenter`. Implementa `IUINavigable` (foco jerárquico TabBar ↔ Content). Setup: `Wire()` instancia presenters, obtiene refs via `document.Q<>()`, wiring callbacks Rebuild/Tick/ClearFocus. Eventos: `OnRegistry` (rebuild listas), `OnCombatLogged` (refresh results+history). Tab 3 "Equipo 3v3" es sibling `CombatLineupUITK` (vive en TabView pero lógica separada). `Update()` llama `results.Tick()` solo si tab 1 visible. Campos: database (stats/partes), asyncCombatService (enqueue). Callback `onEnqueued()` salta a Resultados (tab 1) tras enqueue exitoso.

**Organización (S53 composición — Fase 7):**
- `CombatPanelUITK.cs` — núcleo MonoBehaviour: lifecycle, navigation, tab bar ↔ content, fachada IUINavigable
- `ITabPresenter.cs` — contrato polimórfico para presenters (S54 rename de ICombatTabPresenter)
- `CombatOnlineTabPresenter.cs` — Tab 0: selección + enqueue
- `CombatResultsTabPresenter.cs` — Tab 1: cola + countdown
- `CombatHistoryTabPresenter.cs` — Tab 2: historial + replay

**Eliminadas partials (S53):**
- `CombatPanelUITK.Tabs.cs` → descomposed en CombatOnlineTabPresenter + CombatResultsTabPresenter + CombatHistoryTabPresenter
- `CombatPanelUITK.Navigation.cs` → reducida a región TabBar⇄Content (delegando al presenter activo)

**Vinculado a:** [[Index/05 - UI System]], [[Index/13 - Combat Design Direction]]

**Conexiones:** [[ITabPresenter]], [[CombatOnlineTabPresenter]], [[CombatResultsTabPresenter]], [[CombatHistoryTabPresenter]], [[CombatLineupUITK]], [[UIManager]], [[GameManager]], [[GameEvents]], [[AsyncCombatService]], [[CombatController]]

**Notas S53-S54:**
- IUINavigable reducido a región TabBar⇄Content delegando al presenter activo
- Presenters son clases planas (no MonoBehaviour) con state UI-only; reciben `Func<CreatureRegistrySO>` para lazy-load
- ITabPresenter generalizado en S54 (reemplaza ICombatTabPresenter) para reutilización en Breeding + Detail
