---
tags: [script, combat-prototype, presentation]
---

# CombatUnitView.cs

**Ruta:** `CombatPrototype/CombatUnitView.cs`

**Responsabilidad:** Visualización de unidad en combate. **S85 cambio grande:** NO construye jerarquía por código — es el componente raíz del prefab `Assets/RunRunSimulator/CombatPrototype/UnitView.prefab` con hijos serializados: VisualMount (padre de visual 3D), Disc (renderer de base coloreada), Label (TMP), Feedbacks (MMF_Player para FlashHit), OnHit (Feel feedback). Refs serializadas (visualMount, discRenderer, label, onHit). Tunables: `seedTint` (color especial para SeedUnit), `baseYawOffset` (offset yaw global), `visualScale`, `launchHeight`, `moveArcHeight`, `landArcHeight`. `Init(unit, board)` inicializa: carga visual prefab o cápsula fallback, instancia en visualMount, aplica tint, posiciona en tablero. `SetFacingInstant(facing)` y `RotateTo(facing, duration)` rotan el visual usando atan2→yaw. `FlashHit()` = `onHit.PlayFeedbacks()` (Feel, sin tweens manuales). `RefreshTicks(unit)` muestra "G{guard}·{finisher}" para enemigos, número para jugadores, semilla vacía.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatUnit]], [[SeedUnit]], [[CombatBoard]], [[CombatPrototypeManager]], [[ResolutionAnimator]]
