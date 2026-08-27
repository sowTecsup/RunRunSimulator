---
tags: [script, combat-prototype, presentation]
---

# ResolutionAnimator.cs

**Ruta:** `CombatPrototype/ResolutionAnimator.cs`

**Responsabilidad:** Renderiza eventos de resolución en secuencia. Agrupa por Wave. PlayWave: movimiento simultáneo (Move/Push/Land) → dispara `BoardImpactFeedback.ShakeAt()` en celdas de Land y cada celda de Impact/EnemyAttack (S85), luego secuencial (Hit/Die/EnemyAttack/Rotate/Fizzle). **S85 cambio:** Las 6 duraciones (`moveDuration`, `pushDuration`, `landDuration`, `rotateDuration`, `hitPause`, `wavePause`) dejaron de ser const y son `[SerializeField]` para tunear. `ShakeAt()` ahora itera sobre todas las celdas del evento (Impact y EnemyAttack pueden afectar múltiples celdas). Maneja Rotate (RotateTo visual), Fizzle (ignora), impactos ambientales (environmental hit).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[ResolutionEvent]], [[CombatUnitView]], [[CombatBoard]], [[CombatSimState]], [[BoardImpactFeedback]]
