---
tags: [script, combat-prototype, data]
---

# ResolutionEvent.cs

**Ruta:** `CombatPrototype/ResolutionEvent.cs`

**Responsabilidad:** Evento de resolución de acción. Enum `ResolutionEventType` = Move, Hit, Push, Launch, Land, Die, EnemyAttack, Rotate, Fizzle, Impact. Campos: Type, UnitId, SourceId, From, To, Facing (nuevo S82: facing tras Rotate), Cells (celdas afectadas por plantilla, S83), TicksAfter, Environmental, Wave, **Projectile** (nuevo S87: bool, true si evento dispara proyectil From→To), **Path** **nuevo S88**: List<Vector2Int> con el camino celda a celda de movimientos por patrón (MoveOffsets enemigos rotados). Constructor: `ResolutionEvent(type, unitId)` inicializa Type/UnitId, SourceId=-1, Cells=new List. S82+S85: Rotate y Fizzle. S83: Impact. S87: Projectile. S88: Path usado en ResolutionAnimator para animar hops secuenciales en Move en lugar de desplazamiento directo.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[ActionResolver]], [[ResolutionAnimator]], [[CombatEffects]], [[PlanExecutor]]
