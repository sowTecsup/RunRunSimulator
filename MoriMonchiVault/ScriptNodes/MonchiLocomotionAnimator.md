---
tags: [script, animation, gameplay]
---

# MonchiLocomotionAnimator.cs

**Ruta:** `World/Creatures/MonchiLocomotionAnimator.cs`

**Responsabilidad:** Driver de animación de locomoción. Lee la velocity del NavMeshAgent cada frame y dispara CrossFade en el Animator entre estados Idle/Walk/Run según thresholds (walkThreshold ≈ 0.15, runThreshold ≈ 2.6). NUEVO en S64: durante un tramo de movimiento (transición de Idle → Walk/Run), lanza una moneda con probabilidad `flyChance` para activar vuelo ocasional; la criatura anima "Fly" mientras se mueve, y al detenerse anima "FlyDown" (aterrizaje) antes de volver a Idle. **S97 NUEVO:** Expone UnityEvents `onTakeOff` y `onFlyLand` para que se enganche Feel (MMFeedbacks) en el prefab. Cede el control al Animator si DragonAnimationDriver.IsBusy (durante combate), silenciando cambios de estado en ese período. Refs serializadas al visualizer y agent, mismo GameObject. Implementación simple que desacopla movimiento de combate: el agent de IA maneja pathfinding, este maneja la cara visual animada en gameplay normal.

## Campos Serializados

- `visualizer` (MonchiVisualizer, required) — acceso al animator y transforms
- `navAgent` (NavMeshAgent, required) — lectura de velocity
- `combatDriver` (DragonAnimationDriver, optional) — si busy → silencia ticks
- `walkThreshold` (float, default 0.15) — threshold de velocidad para transicionar a Walk
- `runThreshold` (float, default 2.6) — threshold de velocidad para transicionar a Run
- `crossFade` (float, default 0.2) — duración de la transición de CrossFade (s)
- `flyChance` (float, default 0.25) — **S64** probabilidad [0,1] de vuelo cuando entra en movimiento
- `flyLandCrossFade` (float, default 0.25) — duración del aterrizaje "FlyDown" (s)
- **S97 NUEVOS:**
  - `onTakeOff` (UnityEvent) — disparado cuando inicia vuelo (flying = true)
  - `onFlyLand` (UnityEvent) — disparado cuando aterriza (flying = false)

## Campos Internos

- `currentState` (string) — estado actual de animación (para evitar transiciones redundantes)
- `flying` (bool) — bandera interna de si está volando en este momento

## Métodos Públicos

- `Update()` — tick principal: consulta velocity, decide transición, dispara CrossFade, invoke UnityEvents (S97)

## Ciclo de Transición S64 + S97

1. Determina `rawTarget` (Idle/Walk/Run) según speed del navAgent:
   - speed < walkThreshold → Idle
   - walkThreshold ≤ speed < runThreshold → Walk
   - speed ≥ runThreshold → Run

2. Si transiciona de Idle → movimiento:
   - Lanza moneda: `flying = Random.value < flyChance`
   - Si flying entra, **S97:** invoke `onTakeOff?.Invoke()` para Feel

3. Si vuelve a Idle desde vuelo:
   - CrossFade a "FlyDown" durante `flyLandCrossFade`
   - Luego resetea `flying = false`
   - **S97:** invoke `onFlyLand?.Invoke()` para Feel
   - Finalmente CrossFade a Idle

4. Estado final es `Fly` si `isMoving && flying`, sino `rawTarget`

5. CrossFade al nuevo estado si cambió

## Invariantes S97

- **Feel vía UnityEvents:** `onTakeOff` y `onFlyLand` NO disparan lógica de gameplay (cero cambios de estado, velocidad, efectos); son solo señales para el sistema de Feel (suscriptor: `MMF_Player` en prefab hijo `Feedbacks/`)
- **Enanchufado en prefab:** el prefab `MorimonchiAgent` tiene hijo `Feedbacks/` con `MMF_Player` para cada evento; los UnityEvent se connectan en el Inspector (no por código). Cada `MMF_Player` contiene `MMF_ParticlesInstantiation` en pool.
- **Idempotencia:** invocar `onTakeOff` cuando ya está volando es seguro (el Feel ignora o superpone); mismo con `onFlyLand`.
- **Timing:** eventos se disparan en el mismo frame del cambio de animación (útil para sincronizar partículas).

## Vinculado a

- [[Index/10 - Visualization]]
- [[Index/23 - Arena Sandbox y Expedicion]] (S97: Feel)
- [[MoriMonchiVault/Index/14 - Social V1]]

## Conexiones

- [[MonchiVisualizer]] — acceso al animator y transforms
- [[DragonAnimationDriver]] — si busy, silencia transiciones
- [[MoriMochiAgent]] — prefab que lo contiene
- [[MMF_Player]] — **S97** receptor de `onTakeOff` y `onFlyLand` (hijo `Feedbacks/` del prefab)
