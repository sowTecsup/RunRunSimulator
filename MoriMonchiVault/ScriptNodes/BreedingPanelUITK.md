---
tags: [script, ui, breeding]
---

# BreedingPanelUITK.cs

**Ruta:** `UI/BreedingPanelUITK.cs`

**Responsabilidad (S54 — Fase 7 composición):** Panel modal de crianza (2 tabs: Criar/Incubando). Núcleo delgado MonoBehaviour que orquesta: Tab 0 "Criar" (seleccionar padre+madre, preview, breed) y Tab 1 "Incubando" (huevos + timers + hatch). Compone `ITabPresenter` (contrato polimórfico) + 2 presenters concretos: `BreedingBreedTabPresenter`, `BreedingEggsTabPresenter`. Implementa `IUINavigable` (foco jerárquico TabBar ↔ Content). **S93:** Usa `UiPanels.RootOf()`. Eventos: `OnRegistry` (rebuild listas), `OnBred()` callback (salta a tab 1 tras breed exitoso). Campo especial: `breed.Busy` — congelado input global mientras async breed en vuelo. `Update()` llama `eggs.Tick()` solo si tab 1 visible. Callbacks async via `AsyncBreedingService` (StartBreedingAsync/HatchAsync).

**Organización (S54 composición — Fase 7):**
- `BreedingPanelUITK.cs` — núcleo MonoBehaviour: lifecycle, navigation, tab bar ↔ content, fachada IUINavigable
- `ITabPresenter.cs` — contrato polimórfico para presenters (Enter/Navigate/Submit/Cancel/ClearFocus/Rebuild/Teardown)
- `BreedingBreedTabPresenter.cs` — Tab 0: selección padre/madre + preview + breed async (gestiona Busy)
- `BreedingEggsTabPresenter.cs` — Tab 1: lista huevos + timers + hatch async (método Tick() externo)

**Eliminadas partials (S54):**
- `BreedingPanelUITK.Content.cs` — contenido decomposed en BreedingBreedTabPresenter + BreedingEggsTabPresenter
- `BreedingPanelUITK.Navigation.cs` — navegación reducida a región TabBar⇄Content (delegando al presenter activo)

**Vinculado a:** [[Index/05 - UI System]], [[Index/11 - Technical Debt]] (Fase 7 deuda)

**Conexiones:** [[ITabPresenter]], [[BreedingBreedTabPresenter]], [[BreedingEggsTabPresenter]], [[UIManager]], [[GameManager]], [[GameEvents]], [[AsyncBreedingService]], [[BreedingController]], [[UiPanels]]

**Notas S54:**
- Presenters son clases planas (no MonoBehaviour) con state UI-only; reciben `Func<CreatureRegistrySO>` para lazy-load (no cachean registry)
- BreedingBreedTabPresenter.Busy bloquea input global (core chequea antes de procesar input)
- BreedingEggsTabPresenter.Tick() es método público EXTRA (no en ITabPresenter) — core lo llama desde Update solo si tab 1 visible (throttle 1s)
- Stats en preview usan `CombatStats.GetEffectiveStats()` (S32)
