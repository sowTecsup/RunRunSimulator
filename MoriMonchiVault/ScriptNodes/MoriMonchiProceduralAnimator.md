---
tags: [script, world]
---

# MoriMonchiProceduralAnimator.cs

**Ruta:** `World/Creatures/MoriMonchiProceduralAnimator.cs`

**Responsabilidad:** Componente de animación procedural que conduce los Transforms públicos de [[MoriMonchiVisualizer]] (ModelRoot, BodyTransform, ArmTransforms, EyeTransforms) mediante matemática en LateUpdate. No usa Skinned Mesh ni bones. Estados via enum MMAnimationType: Idle/Walk son loops (respiración, balanceo, movimiento de brazos, parpadeo); Attack/Hit/Victory son one-shots superpuestos; Death es topple con shrink, se queda inmóvil. **API pública:** `PlayMMAnimation(MMAnimationType)` + 6 wrappers sin parámetros (AnimIdle/AnimWalk/AnimAttack/AnimHit/AnimDeath/AnimVictory) para cablear desde UnityEvent. Inspector Odin con TabGroups (General/Idle/Walk/Reacciones) y tooltips por stat. **DEUDA CONOCIDA:** `autoLoopFromMovement` lee NavMeshAgent.velocity para auto-cambiar entre Idle/Walk; debería delegarse a quién controle el movimiento (ej: MoriMochiAgent) para que llame directamente los wrappers en lugar de leer NAV agent aquí. Lazy rest pose capture tras Assemble del Visualizer.

**Vinculado a:** [[Index/06 - Player & World]]

**Conexiones:** [[MoriMonchiVisualizer]], [[MoriMonchiCombatVisualizer]], [[Enums]], [[MoriMochiAgent]]
