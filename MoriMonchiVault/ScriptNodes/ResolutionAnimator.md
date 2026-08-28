---
tags: [script, combat-prototype, presentation]
---

# ResolutionAnimator.cs

**Ruta:** `CombatPrototype/ResolutionAnimator.cs`

**Responsabilidad:** Renderiza eventos de resolución en secuencia. Agrupa por Wave. PlayWave: movimiento simultáneo (Move/Push/Land) → dispara `BoardImpactFeedback.ShakeAt()` en celdas de Land e Impact/EnemyAttack, luego secuencial (Hit/Die/EnemyAttack/Rotate/Fizzle). **S86+S87 cambios:** Las 6 duraciones (`moveDuration`, `pushDuration`, `landDuration`, `rotateDuration`, `hitPause`, `wavePause`) son `[SerializeField]` para tunear. `Rotate` (nuevo S87): gira el atacante hacia el nuevo Facing con `RotateTo()` visual + duración tunable. **S87:** Projectile nuevo en ResolutionEvent (bool `Projectile` marca si dispara proyectil). Si evento es Projectile, anima `projectilePrefab` (tunable) que viaja de From a To con velocidad/altura tuneable. `ShakeAt()` itera celdas del evento (Impact y EnemyAttack pueden afectar múltiples celdas). Maneja Fizzle (ignora sin visuales).

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[ResolutionEvent]], [[CombatUnitView]], [[CombatBoard]], [[CombatSimState]], [[BoardImpactFeedback]], [[MMF_Player]]
