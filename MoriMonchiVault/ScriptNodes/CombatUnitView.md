---
tags: [script, combat-prototype, presentation]
---

# CombatUnitView.cs

**Ruta:** `Systems/CombatPrototype/CombatUnitView.cs`

**Responsabilidad:** Visualización de unidad en combate: instancia prefab visual o cápsula, crea label de ticks, disco-base coloreado. **Cambios S82:** SetFacingInstant + RotateTo(facing, duration) giran SOLO el visual (no afectan al label) usando atan2 → yaw offset 180°. RefreshTicks muestra "G{guard}·{finisher}" para enemigos, número para jugadores.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatUnit]], [[CombatBoard]], [[CombatPrototypeManager]], [[ResolutionAnimator]]
