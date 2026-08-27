---
tags: [script, combat-prototype, logic]
---

# AbilityTargeting.cs

**Ruta:** `CombatPrototype/AbilityTargeting.cs`

**Responsabilidad:** Lógica pura de targeting y validez. Distancias (Chebyshev, Manhattan), rotación offsets por dirección, consultas de altura (IsWall). `GetLandingCell(ability, action)` aplica LandingKind de la habilidad. `IsLandingFree(state, cell)` chequea disponibilidad. `GetAffectedCells(state, ability, action)` calcula celdas de impacto desde anclaje sin chequeos de altura. `IsValidTarget(state, attacker, ability, action)` valida que aterrizaje esté libre. Utilidades: `Chebyshev(a, b)`, `DominantCardinal(from, to)`, `GetAnchorForCursor(ability, cursor, dir)`, etc.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatAbilitySO]], [[CombatSimState]], [[CombatBoard]], [[TargetingController]], [[ActionResolver]], [[NightWaves]], [[CombatAutoTester]]
