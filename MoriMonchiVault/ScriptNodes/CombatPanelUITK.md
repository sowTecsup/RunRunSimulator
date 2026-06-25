---
tags: [script, ui]
---

# CombatPanelUITK.md

**Ruta:** `UI/CombatPanelUITK.cs`

**Responsabilidad:** Panel UI de combate (4 pestañas: Batalla Online, Combate Local, Resultados, Historial). Implementa `IUINavigable` (focus jerárquico). Obtiene config vía propiedad lazy `Config => CombatController.Instance?.Config` (resuelta en el momento de uso en `DoLocalFight`/`MaxFights`), evita carrera de orden de Awake si el panel despierta antes que CombatController. Obtiene registry de GameManager. Combate local vía `CombatService.Simulate()`, async vía `CombatController.EnqueueForAsyncCombat()`, poll vía wrappers de CombatController.

**Vinculado a:** [[Index/05 - UI System]]

**Conexiones:** [[UIManager]], [[CombatController]], [[CombatService]], [[AsyncCombatService]], [[GameManager]]

**Organización (partial class):**
- `CombatPanelUITK.cs` — núcleo/lifecycle/wiring/data
- `CombatPanelUITK.Tabs.cs` — contenido de las 4 pestañas
- `CombatPanelUITK.Navigation.cs` — IUINavigable + foco
