---
tags: [script, animation, gameplay]
---

# MonchiLocomotionAnimator.cs

**Ruta:** `World/Creatures/MonchiLocomotionAnimator.cs`

**Responsabilidad:** Driver de animación de locomoción. Lee la velocity del NavMeshAgent cada frame y dispara CrossFade en el Animator entre estados Idle/Walk/Run según thresholds (walkThreshold ≈ 0.15, runThreshold ≈ 2.6). NUEVO en S64: durante un tramo de movimiento (transición de Idle → Walk/Run), lanza una moneda con probabilidad `flyChance` para activar vuelo ocasional; la criatura anima "Fly" mientras se mueve, y al detenerse anima "FlyDown" (aterrizaje) antes de volver a Idle. Cede el control al Animator si DragonAnimationDriver.IsBusy (durante combate), silenciando cambios de estado en ese período. Refs serializadas al visualizer y agent, mismo GameObject. Implementación simple que desacopla movimiento de combate: el agent de IA maneja pathfinding, este maneja la cara visual animada en gameplay normal.

**Campos:**
- `visualizer` — MonchiVisualizer (requerido)
- `navAgent` — NavMeshAgent (requerido)
- `combatDriver` — DragonAnimationDriver (opcional, si busy → silencia ticks)
- `walkThreshold` — threshold de velocidad para transicionar a Walk (default 0.15)
- `runThreshold` — threshold de velocidad para transicionar a Run (default 2.6)
- `crossFade` — duración de la transición de CrossFade (default 0.2s)
- `flyChance` — **S64 NUEVO** probabilidad [0,1] de vuelo cuando entra en movimiento (default 0.25)
- `flyLandCrossFade` — duración del aterrizaje "FlyDown" (default 0.25s)
- `currentState` — estado actual de animación (string, para evitar transiciones redundantes)
- `flying` — bandera interna de si está volando en este momento

**Lógica de transición S64:**
1. Determina `rawTarget` (Idle/Walk/Run) según speed del agente
2. Si transiciona de Idle → movimiento, lanza moneda: `flying = Random.value < flyChance`
3. Si vuelve a Idle desde vuelo, anima "FlyDown" y resetea `flying = false`
4. El estado final es Fly si `isMoving && flying`, sino `rawTarget`
5. CrossFade al nuevo estado si cambió

**Vinculado a:** [[Index/10 - Visualization]], [[MoriMonchiVault/Index/14 - Social V1]]

**Conexiones:** [[MonchiVisualizer]], [[DragonAnimationDriver]], [[MoriMochiAgent]]
