---
tags: [script, combat-prototype, presentation]
---

# ResolutionAnimator.cs

**Ruta:** `CombatPrototype/ResolutionAnimator.cs`

**Responsabilidad:** Renderiza eventos de resolución en secuencia. Agrupa por Wave. PlayWave: movimiento simultáneo (Move/Push/Land) → dispara `BoardImpactFeedback.ShakeAt()` en celdas de Land e Impact/EnemyAttack, luego secuencial (Hit/Die/EnemyAttack/Rotate/Fizzle). **Duraciones tunables (S86+)**: `moveDuration` (desplazamiento directo), `hopDuration` **nuevo S88** (saltos celda a celda en Path), `pushDuration` (empuje), `landDuration` (aterrizaje), `rotateDuration` (giro), `hitPause`, `wavePause`. **Move con Path (S88)**: si evento tiene `Path` (List<Vector2Int>) no vacío, anima saltos iterativos con `hopDuration` por celda en lugar de desplazamiento directo. Esto permite animaciones de patrón de movimiento enemigo post-golpe. **S87 viejo**: `Rotate` gira visual + duración. Projectile en ResolutionEvent anima proyectil From→To. ShakeAt() itera celdas. Maneja Fizzle.

**Vinculado a:** [[Index/20 - Combat Prototype MVP (Plan)]]

**Conexiones:** [[ResolutionEvent]], [[CombatUnitView]], [[CombatBoard]], [[CombatSimState]], [[BoardImpactFeedback]], [[MMF_Player]]
