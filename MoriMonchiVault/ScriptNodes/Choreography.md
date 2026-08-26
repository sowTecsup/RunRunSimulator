---
tags: [script, combat-prototype, data]
---

# Choreography.cs

**Ruta:** `Systems/CombatPrototype/Choreography.cs`

**Responsabilidad:** Contenedor de la planificación del jugador: lista de Beats (cada beat es un conjunto de PlannedAction simultáneas). **Novedades S82:** MaxActions = 2 (presupuesto global por planificación). Métodos: Add (acción al beat actual), AddBeat (nuevo beat), UndoLast, IsAbilityUsed, TotalActions, AllActions (enumeración).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[PlannedAction]], [[Beat]], [[CombatPrototypeManager]], [[PlanProjection]], [[PlanExecutor]]
