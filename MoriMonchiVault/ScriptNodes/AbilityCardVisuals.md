---
tags: [script, ui, uitk]
---

# AbilityCardVisuals.cs

**Ruta:** `CombatPrototype/AbilityCardVisuals.cs`

**Responsabilidad:** Clase estática UITK pura (sin estado). **BuildAbilityMiniGrid(ability)** → VisualElement con grilla auto-encuadrada. Recorre `TemplateOffsets` para calcular bounds (±4 máximo en x/y), genera grid dinámica. Vacío = gris oscuro (#FFFFFF14); movimiento = verde (#59C96A) en centro (anclaje); ataque direccional = amarillo (#FFD34D) en offsets + verde en landing (S88: `AtAnchor` = verde en anclaje, `BehindAnchor` = verde celda antes, `Stay` = borde reforzado anclaje); aéreo = rojo (#FF6B5E) en centro + versión 40% opacity en radio cardinal 1-2. **Tamaño celdas S88**: 5px si cols > 6, sino 7px. **Grid con flexShrink = 0** (S88) para que no colapse con flex layouts. Tag (flexShrink 0 también) evita reflow. `BuildAbilityTag(ability)` → Label ("→mov" azul #7EC8FF movimiento, "⚔1" rojo #FF9E8F ataque, +sufijo " aéreo" si `TargetingMode.AirborneEnemy`). Usada por `CombatPrototypeHUD` en cards de dragones.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatPrototypeHUD]], [[CombatAbilitySO]]
