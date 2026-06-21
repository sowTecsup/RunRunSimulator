---
tags: [script, ui]
---

# CombatPanelUITK.md

**Ruta:** `UI/CombatPanelUITK.cs`

**Responsabilidad:** Panel UI de combate (3 pestañas: Batalla Online, Combate Local, Resultados). Implementa `IUINavigable` (focus jerárquico). Obtiene config vía `CombatController.Instance.Config`. Obtiene registry de GameManager. Combate local via `CombatService.Simulate()`, async vía `CombatController.EnqueueForAsyncCombat()`, poll vía wrappers de CombatController.

**Vinculado a:** [[Index/05 - UI System]]

**Conexiones:** [[UIManager]], [[CombatController]], [[CombatService]], [[AsyncCombatService]], [[GameManager]]

**Organización (partial class):**
- `CombatPanelUITK.cs` — núcleo/lifecycle/wiring/data
- `CombatPanelUITK.Tabs.cs` — contenido de las 4 pestañas
- `CombatPanelUITK.Navigation.cs` — IUINavigable + foco
