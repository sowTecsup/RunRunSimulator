---
tags: [script, combat-prototype, ui]
---

# TargetingController.cs

**Ruta:** `CombatPrototype/TargetingController.cs`

**Responsabilidad:** Controla la interfaz de targeting del jugador: selección de unidad, habilidad, dirección (cardinales), slam (dos pasos). Propiedades públicas: SelectedUnitId, SelectedAbilityIndex, CursorCell, CurrentDirection, AwaitingSlamCell (bool si pendingSlamTarget != null). **S87 CAMBIO:** Evento C# público `SelectionChanged` (Action sin parámetros) disparado en `SelectUnit()`, `SelectAbility()`, `SetDirection()`, `SetCursor()` (si direction cambió), `Rotate()`, `ClearSelection()`. Flujo de slam (dos pasos). Plantilla de aterrizaje (Landing) se muestra como highlight diferente; validación usa `GetLandingCell` + `IsLandingFree`. Suscribe a `BoardHighlighter` para visualizar celdas afectadas, zonas de aterrizaje, selección. `RefreshHighlights()` pinta Selection en celda de unidad seleccionada si está viva. `GetSelectedAbility()` retorna CombatAbilitySO de SelectedUnitId + SelectedAbilityIndex.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatSimState]], [[CombatAbilitySO]], [[PlannedAction]], [[AbilityTargeting]], [[BoardHighlighter]], [[CombatInputController]], [[CombatPrototypeHUD]], [[SelectionFacingPreview]]
