---
tags: [script, ui, detail]
---

# MorimonchiDetailInfoUITK.cs

**Ruta:** `UI/MorimonchiDetailInfoUITK.cs`

**Responsabilidad (S54 — Fase 7 composición):** Resumen detallado de MoriMochi (ventana modal, FireRed-summary inspired, 4 tabs). Núcleo delgado MonoBehaviour que orquesta: Tab 0 "Información" (stats+equipo, género/estado/nacimiento, rol+elemento, partes, contadores), Tab 1 "Combate" (historial 3v3 newest-first con replay), Tab 2-3 "Linaje + Descendencia" (árbol genealógico), Tab 4 "Equipo" (3 slots + stats Base→Final). Escucha `UIManager.OnCreatureSelected` (evento carga dna + registry en campo). Implementa `IUINavigable` (solo A/D navega tabs — no Submit/Cancel internos). Setup: `Wire()` instancia presenters, obtiene refs via `document.Q<>()`. Eventos: `OnRegistryChanged` repopula en-place (equipping desde mochila actualiza tab Equipo sin mover foco). Backdrop modal con sortingOrder > grid.

**Organización (S54 composición — Fase 7):**
- `MorimonchiDetailInfoUITK.cs` — núcleo MonoBehaviour: lifecycle, wiring, población, IUINavigable routing (A/D tabs)
- Cuatro presenters colaboradores (NO implementan ITabPresenter — son ro Rebuild sin navegación interna):
  - `DetailInfoTabPresenter.cs` — Tab 0: stats (base + bonus equipo), gender/state/nacimiento, rol+elemento, partes, contadores
  - `DetailCombatTabPresenter.cs` — Tab 1: historial 3v3 (replay, outcomes, stats)
  - `DetailTreesPresenter.cs` — Tab 2-3: Linaje (ancestros 2gen) + Descendencia (cría, agrupa por pareja) — comparten dominio genetics
  - `DetailEquipTabPresenter.cs` — Tab 4: 3 slots (Arma/Armadura/Amuleto) + cards clickeables abren mochila + stats tabla

**Decisión de arquitectura S54:**
- Presenters del Detail NO son `ITabPresenter` (son ro Rebuild, sin navegación A/D internas ni state jerárquico)
- Son colaboradores planos ctor(root, deps) + Rebuild(dna), sin Teardown (callbacks se recrean en cada rebuild)
- `DetailTreesPresenter` cubre DOS tabs (Linaje + Descendencia) por compartir `MakeChip()` + `ParseGenetics()`
- Core navega A/D entre tabs; cada tab es responsabilidad del presenter (sin sub-foco interno)

**Eliminadas partials (S54):**
- `MorimonchiDetailInfoUITK.Trees.cs` — descomposed en `DetailTreesPresenter.cs` (BuildLineage + BuildBreed + helpers)

**Vinculado a:** [[Index/05 - UI System]], [[Index/11 - Technical Debt]] (Fase 7 deuda)

**Conexiones:** [[DetailInfoTabPresenter]], [[DetailCombatTabPresenter]], [[DetailTreesPresenter]], [[DetailEquipTabPresenter]], [[UIManager]], [[GameManager]], [[GameEvents]], [[EquipmentBackpackUITK]], [[CreatureDatabaseSO]], [[EquipmentDatabaseSO]], [[EquipmentPaletteSO]], [[CombatReplayRequest]]

**Notas S54:**
- Presenters son clases planas (no MonoBehaviour) sin estado excepto UI; reciben `Func<CreatureRegistrySO>` para lazy-load
- Callbacks de botones (equipo: mochila, combate: replay) se recrean en cada Rebuild
- OnRegistryChanged repopula in-place sin mover tab (user mantiene foco)
- IUINavigable: OnUINavigate A/D navega tabs (0-3), OnUISubmit vacío, OnUICancel retorna false (cierra vía UIManager)
- Stats en Info/Equipo usan `CombatStats.GetEffectiveStats()` + `EquipmentStats.Apply()` (S32+S26)
- Rol/Elemento heredables desde DNA (S39)
