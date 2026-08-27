---
tags: [script, ui, uitk]
---

# AbilityCardVisuals.cs

**Ruta:** `CombatPrototype/AbilityCardVisuals.cs`

**Responsabilidad:** Clase estática UITK pura (sin estado). Dos métodos: `BuildAbilityMiniGrid(ability)` retorna un VisualElement con una grilla 5×5 que visualiza la plantilla de la habilidad. Vacío = gris oscuro translúcido (#FFFFFF14); movimiento = verde (#59C96A) en centro; ataque direccional = amarillo (#FFD34D) en offsets + verde en landing (si BehindAnchor) o borde reforzado (si Stay); aéreo = rojo (#FF6B5E) en centro + versión translúcida en un radio cardinal de 1-2. `BuildAbilityTag(ability)` retorna Label con texto tagado ("→mov" azul #7EC8FF para movimiento, "⚔1" rojo #FF9E8F para ataque, +sufijo " aéreo" si aplica). Usada por `CombatPrototypeHUD` en el renderizado de cards de dragones.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatPrototypeHUD]], [[CombatAbilitySO]]
