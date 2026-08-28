---
tags: [script, combat-prototype, data]
---

# ResolutionEvent.cs

**Ruta:** `CombatPrototype/ResolutionEvent.cs`

**Responsabilidad:** Evento de resolución de acción. Enum `ResolutionEventType` = Move, Hit, Push, Launch, Land, Die, EnemyAttack, Rotate, Fizzle, Impact. Campos: Type, UnitId, SourceId, From, To, Facing (nuevo S82: facing tras Rotate), Cells (celdas afectadas por plantilla, S83), TicksAfter, Environmental, Wave, **Projectile** (nuevo S87: bool, true si evento dispara proyectil que viaja de From a To). Constructor: `ResolutionEvent(type, unitId)` inicializa Type/UnitId, SourceId=-1, Cells=new List. Cambios S82+S85: Rotate (giro de facing post-golpe) y Fizzle (acción inválida) reemplazan reacciones antiguas. S83: Impact marca impacto de plantilla (nunca si fizzle, pero sí aunque no haya víctimas). S87: Projectile para hook animador de proyectiles.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[ActionResolver]], [[ResolutionAnimator]], [[CombatEffects]], [[PlanExecutor]]
