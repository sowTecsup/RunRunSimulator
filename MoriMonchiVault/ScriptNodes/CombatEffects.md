---
tags: [script, combat-prototype, logic]
---

# CombatEffects.cs

**Ruta:** `Systems/CombatPrototype/CombatEffects.cs`

**Responsabilidad:** Lógica pura de efectos de combate. Mutadores: ApplyHit (resta 1 tick), ApplyPush (mueve con detección pared/bloqueador/caída), ApplyLaunch (airborne), ApplyLanding (desciende, colisión), ApplySlam (airborne slam). CollectDeaths marca muertos.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[CombatSimState]], [[ResolutionEvent]], [[ActionResolver]]
