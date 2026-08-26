---
tags: [script, combat-prototype, data]
---

# ResolutionEvent.cs

**Ruta:** `Systems/CombatPrototype/ResolutionEvent.cs`

**Responsabilidad:** Evento de resolución de acción (Move, Hit, Push, Launch, Land, Die, EnemyAttack, Rotate, Fizzle, Impact). Campos: Type, UnitId, SourceId, From, To, Facing (nuevo S82), Cells, TicksAfter, Environmental, Wave. **Cambios S82:** enum sin Reaction; Rotate (giro de facing post-golpe bloqueado) y Fizzle (acción inválida) reemplazan reacciones antiguas; campo Facing para comunicar nuevo facing tras Rotate. **Cambios S83:** enum gana `Impact` para marcar impacto de plantilla sobre tablero (nunca se emite si fizzle, pero sí aunque no haya víctimas); Cells = celdas afectadas por plantilla.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[ActionResolver]], [[ResolutionAnimator]], [[CombatEffects]]
