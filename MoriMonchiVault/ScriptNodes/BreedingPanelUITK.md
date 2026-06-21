---
tags: [script, ui]
---

# BreedingPanelUITK.md

**Ruta:** `UI/BreedingPanelUITK.cs`

**Responsabilidad:** Panel UI de breeding (2 pestañas: Criar, Incubando). Implementa `IUINavigable` (focus jerárquico). Obtiene registry de GameManager. Cría local via `BreedingController.BreedCreatures()`, async via `BreedingController.StartBreedingAsync()`, hatch via `BreedingController.HatchAsync()`. Tick de huevos en Update (cuenta atrás al servidor).

**Vinculado a:** [[Index/05 - UI System]]

**Conexiones:** [[UIManager]], [[BreedingController]], [[AsyncBreedingService]], [[GameManager]]

**Organización (partial class):**
- `BreedingPanelUITK.cs` — núcleo/lifecycle/wiring/data
- `BreedingPanelUITK.Content.cs` — candidatos/huevos/preview/breed/hatch
- `BreedingPanelUITK.Navigation.cs` — IUINavigable + foco
