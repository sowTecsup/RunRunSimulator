---
tags: [script, RETIRADO-S58, world, animation]
---

# MoriMonchiProceduralAnimator.cs — RETIRADO S58

**Estado:** RETIRADO — Migración Suriyun (S58)

**Descripción anterior:**
- Animación procedural (no Animator)
- Conducía transforms de MoriMonchiVisualizer (brazos, ojos, cabeza)
- Estados: Idle, Walk, Attack, Hit, Death, Victory
- LateUpdate + matemática trigonométrica (respiración, balanceo, parpadeo)

**Reemplazo:** [[DragonAnimationDriver]] (Animator Suriyun)
- Rig FBX con Animator + AnimatorController
- Estados nativos (Idle, Attack, Hit, Defeat, Victory)
- Transiciones suaves vía parámetros (Speed, Intensity, etc.)

**Cuando se eliminó:** S58

**Cambio:**
- Procedural math → Animator assets (más eficiente, más polished)
- NavMeshAgent velocity check → Controller llama PlayIdle/PlayWalk directamente

**Conexiones antiguas:**
- MoriMonchiVisualizer (RETIRADO)
- MoriMonchiCombatVisualizer (RETIRADO)

**Ver también:** [[DragonAnimationDriver]], [[MonchiAnimationDriver]], [[MonchiVisualizer]]
