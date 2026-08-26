---
tags: [script, combat-prototype, logic]
---

# CombatEffects.cs

**Ruta:** `Systems/CombatPrototype/CombatEffects.cs`

**Responsabilidad:** Aplicadores de efectos: ApplyHit (marca WasHitThisTurn en EnemyUnit), ApplyPush (movimiento con colisión de muro/unidad), ApplyLaunch (aterrizaje aéreo), ApplyLanding (aterrizaje post-aéreo), ApplySlam (ataque sobre aéreo), CollectDeaths. FindFreeCell auxiliar (búsqueda de casilla libre al resolver choques). Genera ResolutionEvent por cada mutación.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[ActionResolver]], [[ResolutionEvent]], [[CombatSimState]], [[CombatBoard]], [[EnemyUnit]]
