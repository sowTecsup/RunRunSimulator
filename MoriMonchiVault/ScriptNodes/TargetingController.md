---
tags: [script, combat-prototype, ui]
---

# TargetingController.cs

**Ruta:** `Systems/CombatPrototype/TargetingController.cs`

**Responsabilidad:** Controlador de selección/targeting UI. Estados: SelectedUnitId, SelectedAbilityIndex, CursorCell, CurrentDirection. Métodos: SelectUnit/Ability, SetCursor/MoveCursor, Rotate. Valida acciones con AbilityTargeting.IsValidTarget. Genera PlannedAction. Maneja slam (dos clics).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatSimState]], [[CombatAbilitySO]], [[PlannedAction]], [[AbilityTargeting]], [[BoardHighlighter]], [[CombatInputController]], [[CombatPrototypeHUD]]
