---
tags: [script, combat-prototype, ui]
---

# CombatInputController.cs

**Ruta:** `CombatPrototype/CombatInputController.cs`

**Responsabilidad:** Polled input (KeyBoard/Mouse, new InputSystem). **S86 cambio:** Gate de input por HUD picking — `IsPointerOver()` determina si se permiten raycast y acciones del tablero. **S84 cambio de idioma:** drag (clic = destino, arrastrar = orienta, soltar = confirma). Mapeos: F1-F3 selectUnit (nueva S88: `TrySelectSlot(slot)` verifica HasAvailableAbility antes de permitir), 1-3 selectAbility, Q/E rotar, Enter confirmar, Tab addBeat, Backspace undo, Escape clearSelection. Mouse raycast→cell solo si no está sobre UI. Clic izquierdo: en Setup coloca semilla/dragón via `manager.PlaceAt(cell)`; en Planning intenta TrySelectPlayerAt(cell) — si halla dragón vivo CON PODERES, llama `targeting.SelectUnit(slotIndex)`, sino inicia drag (S88: bloqueado solo si dragón NO tiene poder disponible). Drag gestiona orientación via `SetDirection(cardinal)`. Clic derecho abre brief panel. **S84:** WASD pan cámara.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[TargetingController]], [[CombatPrototypeManager]], [[CombatBoardBuilder]], [[CombatPrototypeHUD]]
