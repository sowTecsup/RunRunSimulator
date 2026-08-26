---
tags: [script, combat-prototype, presentation]
---

# ResolutionAnimator.cs

**Ruta:** `Systems/CombatPrototype/ResolutionAnimator.cs`

**Responsabilidad:** Renderiza eventos de resolución en secuencia. Agrupa por Wave. PlayWave: movimiento → dispara BoardImpactFeedback.ShakeAt en celdas (Land, Impact, EnemyAttack), luego secuencial (Hit/Die/EnemyAttack/Rotate/Fizzle). **Novedades S82:** maneja Rotate (RotateTo visual), Fizzle (ignora), impactos ambientales (environmental hit). Inyecta BoardImpactFeedback opcional. **Cambios S83:** loop de feedback ahora iterpola Impact (todos sus Cells) y EnemyAttack (todos sus Cells) para shakeAt en cada una, además de Land.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[ResolutionEvent]], [[CombatUnitView]], [[CombatBoard]], [[CombatSimState]], [[BoardImpactFeedback]]
