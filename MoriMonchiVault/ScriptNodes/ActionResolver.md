---
tags: [script, combat-prototype, logic]
---

# ActionResolver.cs

**Ruta:** `CombatPrototype/ActionResolver.cs`

**Responsabilidad:** Ejecutor de acciones y turnos de enemigos. `ResolveBeat(state, beat)` ejecuta acciones de jugador: Move (aterrizaje) → Impact (golpe en anclaje, fizzle si aterrizaje ocupado), landings, muertes. `ResolveEnemyTurn(state)` (S87): enemigos atacan con patrón AtAnchor + offsets rotados. **S87 CAMBIO GRANDE:** `ResolveAttack()` ahora con `PushFromCenter`: si true, empuje es radial desde anclaje (cada celda afectada se empuja hacia afuera), si false empuje es en dirección de facing. Ataque perforante (penetra unidades sin break, hiere múltiples). `ResolveEnemyAttack()` perforante sin break entre víctimas. Movimiento post-ataque solo si fue golpeado (WasHitThisTurn), intenta offsets, si bloqueado → rota 90° horario con evento Rotate. **S85 NUEVO:** `ResolveGermination(state)` mata todos enemigos vivos cuando germina semilla (turno de victoria). Genera `ResolutionEvent` por cada mutación (Move, Hit, Impact, Push, Launch, Land, Die, EnemyAttack, Rotate, Fizzle).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[PlannedAction]], [[CombatSimState]], [[CombatAbilitySO]], [[AbilityTargeting]], [[CombatEffects]], [[ResolutionEvent]], [[EnemyUnit]], [[SeedUnit]], [[Choreography]]
