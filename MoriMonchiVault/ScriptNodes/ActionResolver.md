---
tags: [script, combat-prototype, logic]
---

# ActionResolver.cs

**Ruta:** `CombatPrototype/ActionResolver.cs`

**Responsabilidad:** Ejecutor de acciones y turnos de enemigos. `ResolveBeat(state, beat)` ejecuta acciones de jugador: Move (aterrizaje) → Impact (golpe en anclaje, fizzle si aterrizaje ocupado), landings, muertes. `ResolveEnemyTurn(state)` → aterrizajes pendientes + ataques de enemigos vivos + movimientos post-golpe con patrón MoveOffsets. `ResolveEnemyReactions(state)` **nuevo S88** → gemelo de `ResolveEnemyTurn` pero SIN ataques, solo aterrizajes y movimientos post-golpe (para turnos sin cierre de ciclo). **TryEndOfTurnMove(state, enemy, wave, events)** → privado: intenta desplazar enemigo según MoveOffsets. Genera `ResolutionEvent` de tipo Move con campo `Path` (List<Vector2Int> celda a celda). Itera offsets rotados por facing del enemigo; si celda libre, añade a Path y avanza destino; si bloqueada/fuera de límite, rota 90° horario y retorna con evento Rotate. El Path permite animar saltos (hops) celda a celda en ResolutionAnimator. **S87 CAMBIO VIEJO:** `ResolveAttack()` con `PushFromCenter`: si true, empuje radial desde anclaje; sino, empuje en dirección facing. Ataque perforante (sin break entre víctimas). `ResolveEnemyAttack()` perforante. **Impact.Facing (S88)**: si `ability.IgnoresHeight` es true O celda origen == celda unidad, usa `action.Direction` (dirección disparo); sino calcula cardinal dominante origen→destino. **ResolveGermination(state)** (S85): mata todos enemigos vivos cuando germina semilla.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[PlannedAction]], [[CombatSimState]], [[CombatAbilitySO]], [[AbilityTargeting]], [[CombatEffects]], [[ResolutionEvent]], [[EnemyUnit]], [[SeedUnit]], [[Choreography]]
