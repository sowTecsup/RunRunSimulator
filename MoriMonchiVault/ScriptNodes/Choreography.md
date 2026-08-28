---
tags: [script, combat-prototype, data]
---

# Choreography.cs

**Ruta:** `Systems/CombatPrototype/Choreography.cs`

**Responsabilidad:** Contenedor de la planificación del jugador: lista de Beats (cada beat es un conjunto de PlannedAction simultáneas). **Novedades S82:** MaxActions = 2 (presupuesto global por planificación). **Métodos**: Add (acción al beat actual), AddBeat (nuevo beat), UndoLast, IsAbilityUsed(unitId, abilityIndex), **IsUnitUsed(unitId)** **nuevo S88** (¿el dragón tiene al menos una acción en plan?), TotalActions, AllActions (enumeración).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[PlannedAction]], [[Beat]], [[CombatPrototypeManager]], [[PlanProjection]], [[PlanExecutor]]
