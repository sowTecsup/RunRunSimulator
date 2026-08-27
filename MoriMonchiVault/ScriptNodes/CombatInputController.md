---
tags: [script, combat-prototype, ui]
---

# CombatInputController.cs

**Ruta:** `CombatPrototype/CombatInputController.cs`

**Responsabilidad:** Polled input (KeyBoard/Mouse, new InputSystem). **S84 cambio de idioma:** drag (clic = destino, arrastrar = orienta, soltar = confirma). Mapeos: F1-F3 selectUnit, 1-3 selectAbility, WASD moveCursor (pan en tablero con clamp, S84), Q/E rotar, Enter confirmar, Tab addBeat, Backspace undo, Mouse raycast→cell. Clic izquierdo en Setup (fase de despliegue) llama `manager.PlaceAt(cell)` para plantar semilla o dragón. Drag con hold+move+release gestiona la orientación (SetDirection cardinal). Clic derecho abre info. **S83:** clic izquierdo primero intenta TrySelectPlayerAt(cell) — si encuentra dragón vivo, llama targeting.SelectUnit(slotIndex).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[TargetingController]], [[CombatPrototypeManager]], [[CombatBoardBuilder]]
