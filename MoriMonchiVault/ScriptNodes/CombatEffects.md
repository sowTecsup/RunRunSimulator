---
tags: [script, combat-prototype, logic]
---

# CombatEffects.cs

**Ruta:** `CombatPrototype/CombatEffects.cs`

**Responsabilidad:** Aplicadores de efectos. `ApplyHit(s, target, sourceId, environmental, wave, events)` decrementa ticks, marca `WasHitThisTurn` en EnemyUnit, **S84: rota-hacia-atacante** (si no-ambiental y ambos vivos y distintos, computa `DominantCardinal` y emite evento `Rotate`). `ApplyPush(target, direction, distance, sourceId, wave, events)` mueve hasta 'distance' en dirección; **S84: inmune a SeedUnit** (retorna sin hacer nada). `ApplyLaunch` (aterrizaje aéreo), `ApplyLanding` (post-aéreo), `ApplySlam` (ataque sobre aéreo). `CollectDeaths` (elimina unidades muertas). FindFreeCell auxiliar (búsqueda de casilla libre al resolver choques). Todos generan `ResolutionEvent` por cada mutación.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[ActionResolver]], [[ResolutionEvent]], [[CombatSimState]], [[CombatBoard]], [[EnemyUnit]], [[SeedUnit]], [[AbilityTargeting]]
