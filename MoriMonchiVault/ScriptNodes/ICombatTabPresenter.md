---
tags: [script, ui, interface, RETIRADO]
---

# ICombatTabPresenter.cs — RETIRADO (S54)

**Estado:** ELIMINADO/RENOMBRADO. La interfaz `ICombatTabPresenter.cs` fue renombrada en S54 a `ITabPresenter.cs` (generalización para reutilización en Breeding + Detail).

**Reemplazado por:** [[ITabPresenter]]

**Razón:** Decisión de arquitectura S54 — `ITabPresenter` es la versión generalizada, implementada por:
- `CombatOnlineTabPresenter`, `CombatResultsTabPresenter`, `CombatHistoryTabPresenter` (combat)
- `BreedingBreedTabPresenter`, `BreedingEggsTabPresenter` (breeding)

Ver [[ITabPresenter]] para detalles.
