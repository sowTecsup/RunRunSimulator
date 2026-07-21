---
tags: [script, animation, gameplay]
---

# MonchiLocomotionAnimator.cs

**Ruta:** `World/Creatures/MonchiLocomotionAnimator.cs`

**Responsabilidad:** Driver de animación de locomoción. Lee la velocity del NavMeshAgent cada frame y dispara CrossFade en el Animator entre estados Idle/Walk/Run según thresholds (walkThreshold ≈ 0.15, runThreshold ≈ 2.6). Cede el control al Animator si DragonAnimationDriver.IsBusy (durante combate), silenciando cambios de estado en ese período. Refs serializadas al visualizer y agent, mismo GameObject. Implementación simple que desacopla movimiento de combate: el agent de IA maneja pathfinding, este maneja la cara visual animada en gameplay normal.

**Vinculado a:** [[Index/10 - Visualization]]

**Conexiones:** [[MonchiVisualizer]], [[DragonAnimationDriver]], [[MoriMochiAgent]]
