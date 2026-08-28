---
tags: [script, combat-prototype, logic]
---

# AbilityTargeting.cs

**Ruta:** `CombatPrototype/AbilityTargeting.cs`

**Responsabilidad:** Lógica pura de targeting y validez. Distancias (Chebyshev, Manhattan). `RotateOffset(offset, direction)` rota offsets de template por dirección cardinal. `IsWall(board, fromCell, cell)` = true si elevation(cell) >= elevation(fromCell) + 2. `GetAnchorForCursor()` retorna anclaje de golpe según tipo. `GetLandingCell()` aplica LandingKind de habilidad. `IsLandingFree()` chequea que aterrizaje esté disponible. **S87 CAMBIO GRANDE:** `GetAffectedCells(state, unit, ability, action)` nueva firma que acepta (state, unit, ability, action). Genera celdas de impacto desde anclaje. Filtra por altura: si no IgnoresHeight, salta celdas con |Δelev| >= 2. Si IgnoresObstacles, continúa sobre huecos (out-of-bounds). Valida Range (Manhattan desde anclaje). Si TemplateOffsets vacío → lanza excepción legible. `DominantCardinal(from, to)`, `Chebyshev()`, `Manhattan()`, `GetAnchorForCursor()` utilidades simples.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatAbilitySO]], [[CombatSimState]], [[CombatBoard]], [[CombatUnit]], [[TargetingController]], [[ActionResolver]], [[NightWaves]], [[CombatAutoTester]]
