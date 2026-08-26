---
tags: [script, combat-prototype, logic]
---

# AbilityTargeting.cs

**Ruta:** `Systems/CombatPrototype/AbilityTargeting.cs`

**Responsabilidad:** Lógica pura de targeting y validez. Distancias (Chebyshev/Manhattan), rotación offsets por dirección, consultas IsWall. **Eliminados:** GetValidTargets, GetAffectedCellsForDirection, GetLineCells. **Nuevos:** GetLandingCell (aplica LandingKind de la habilidad), IsLandingFree (chequea si el aterrizaje está disponible). GetAffectedCells calcula celdas de impacto desde el anclaje de la acción sin chequeos de altura; IsValidTarget valida que el aterrizaje esté libre.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatAbilitySO]], [[CombatSimState]], [[CombatBoard]], [[TargetingController]], [[ActionResolver]]
