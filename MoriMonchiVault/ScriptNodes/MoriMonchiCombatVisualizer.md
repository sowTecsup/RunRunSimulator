---
tags: [script, RETIRADO-S58, world, combat, visual]
---

# MoriMonchiCombatVisualizer.cs — RETIRADO S58

**Estado:** RETIRADO — Migración Suriyun + retiro pipeline visual legacy (S58)

**Descripción anterior:**
- Derivada de MoriMonchiVisualizer
- 9 UnityEvents para feedback (OnAttack, OnHitDealt, OnCritDealt, OnHitTaken, OnCritTaken, OnCombatStart, OnDead, OnVictory, OnHpChanged)
- Llamadas desde CombatVisualizerService durante replay 3v3

**Reemplazo:** [[DragonAnimationDriver]] (vía [[MonchiAnimationDriver]] contrato)
- `PlayAttack(target, onImpact, onDone)` — espera callbacks reales (no fired UEvents)
- `PlayHit(intensity)` — animación knockback
- `PlayDefeat()` — caída
- `PlayVictory()` — victoria
- `PlayIdle()` — idle
- `PlayBuff(buffName)` — pasivas

**Cuando se eliminó:** S58

**Cambio principal:**
- UEvents → Métodos callback (más control, menos inspector clutter)
- DragonAnimationDriver maneja timing/callbacks interno

**Conexiones antiguas:**
- MoriMonchiVisualizer (RETIRADO)

**Ver también:** [[DragonAnimationDriver]], [[MonchiAnimationDriver]], [[CombatVisualizerService]]
