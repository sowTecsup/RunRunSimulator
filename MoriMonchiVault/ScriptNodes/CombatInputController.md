---
tags: [script, combat-prototype, ui]
---

# CombatInputController.cs

**Ruta:** `Systems/CombatPrototype/CombatInputController.cs`

**Responsabilidad:** Polled input (KeyBoard/Mouse, new InputSystem). Mapeos: F1-F3 selectUnit, 1-3 selectAbility, WASD moveCursor, Q/E rotar, Enter confirmar, Tab addBeat, Backspace undo, Mouse raycast→cell. **Cambios S83:** clic izquierdo (leftButton.wasPressedThisFrame) primero intenta TrySelectPlayerAt(cell) — si encuentra dragón vivo en celda, llama targeting.SelectUnit(slotIndex) y retorna true. Solo si TrySelectPlayerAt retorna false hace targeting.SetCursor + manager.ConfirmAction (mantiene el flujo anterior de targeting/confirmación).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[TargetingController]], [[CombatPrototypeManager]], [[CombatBoardBuilder]]
