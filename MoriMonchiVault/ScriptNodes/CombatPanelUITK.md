---
tags: [memory-bank, script, ui]
---

# CombatPanelUITK.md

**Ruta:** `UI/CombatPanelUITK.cs`

**Responsabilidad:** Panel UI de combate. Selección de criaturas, progreso, resultados. `IUINavigable`.

**Vinculado a:** [[Index/05 - UI System]]

**Conexiones:** [[UIManager]], [[CombatController]], [[CombatService]]

**Organización (partial class):**
- `CombatPanelUITK.cs` — núcleo/lifecycle/wiring/data
- `CombatPanelUITK.Tabs.cs` — contenido de las 4 pestañas
- `CombatPanelUITK.Navigation.cs` — IUINavigable + foco
