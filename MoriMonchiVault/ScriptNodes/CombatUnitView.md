---
tags: [script, combat-prototype, presentation]
---

# CombatUnitView.cs

**Ruta:** `CombatPrototype/CombatUnitView.cs`

**Responsabilidad:** Visualización de unidad en combate. **S86+S87 cambios:** Prefab UnitView.prefab es jerarquía serializados (VisualMount, Disc, Label, Feedbacks). Refs serializadas: `visualMount`, `discRenderer`, `label`, `onHit` (MMF_Player). Tunables: `seedTint`, `baseYawOffset` (ahora **0** por defecto en prefab, offset yaw global para alineación visual), `visualScale`, `launchHeight`, `moveArcHeight`, `landArcHeight`. **S86:** `_animator` = LateUpdate lerps entre animación Anim_Dra_Fly (airborne) e Idle (grounded) si existe Animator. **S87:** Spawn facing (0,-1) apunta hacía abajo del tablero; `seedTint` aplicado al `label` TMP para diferenciar semilla. `Init()` carga visual prefab o cápsula fallback, instancia en visualMount, aplica tint, posiciona en tablero. `SetFacingInstant(facing)` y `RotateTo(facing, duration)` rotan visual usando atan2→yaw + baseYawOffset. `FlashHit()` dispara `onHit.PlayFeedbacks()` (MMF_Player Feel hook). `RefreshTicks()` muestra ticks según tipo de unidad.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatUnit]], [[SeedUnit]], [[PlayerUnit]], [[EnemyUnit]], [[CombatBoard]], [[CombatPrototypeManager]], [[ResolutionAnimator]], [[WorldLabelBillboard]]
