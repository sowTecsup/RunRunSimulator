---
tags: [script, combat-prototype, ui]
---

# CombatInputController.cs

**Ruta:** `CombatPrototype/CombatInputController.cs`

**Responsabilidad:** Polled input (KeyBoard/Mouse, new InputSystem). **S86 cambio:** Gate de input por HUD picking — `IsPointerOver()` determina si se permiten raycast y acciones del tablero. **S84 cambio de idioma:** drag (clic = destino, arrastrar = orienta, soltar = confirma). Mapeos: F1-F3 selectUnit, 1-3 selectAbility, Q/E rotar (SetDirection cardinal), Enter confirmar, Tab addBeat, Backspace undo, Escape clearSelection. Mouse raycast→cell solo si no está sobre UI. Clic izquierdo: en Setup coloca semilla/dragón via `manager.PlaceAt(cell)`; en Planning intenta TrySelectPlayerAt(cell) — si halla dragón vivo, llama `targeting.SelectUnit(slotIndex)`, sino inicia drag. Drag (hold+move+release) gestiona la orientación via `SetDirection(cardinal)`. Clic derecho abre brief panel con info. **S84:** WASD pan cámara (nuevas teclas, reemplazaron flechas).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[TargetingController]], [[CombatPrototypeManager]], [[CombatBoardBuilder]], [[CombatPrototypeHUD]]
