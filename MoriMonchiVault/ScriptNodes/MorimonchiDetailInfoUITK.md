---
tags: [script, ui, uitk]
---

# MorimonchiDetailInfoUITK.cs

**Ruta:** `UI/MorimonchiDetailInfoUITK.cs`

**Responsabilidad:** Panel de detalles UITK de MoriMochi. Tabs: Info (partes genéticas 5 slots, progresión), Equipo (grilla de equipo por slot). **S75:** Sin tab Combate (demolición). **S93:** Usa `UiPanels.RootOf()`.

## Tabs

- **Info** — Partes genéticas, BreedCount, stats efectivos
- **Equipo** — Grilla free-placement por EquipmentSlot

## Cambios en S75

- **ELIMINADO:** Tab Combate (demolición del combate)

## Vinculado a

- [[Index/05 - UI System]]

**Conexiones:** [[DetailInfoTabPresenter]], [[DetailEquipTabPresenter]], [[CreatureStats]], [[UiPanels]]
