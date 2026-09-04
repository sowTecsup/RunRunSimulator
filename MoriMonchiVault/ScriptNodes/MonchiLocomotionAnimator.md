---
tags: [script, animation, gameplay]
---

# MonchiLocomotionAnimator.cs

**Ruta:** `World/Creatures/MonchiLocomotionAnimator.cs`

**Responsabilidad:** Driver de animación de locomoción. Lee velocity del NavMeshAgent cada frame y dispara CrossFade entre estados Idle/Walk/Run según thresholds (walkThreshold ≈ 0.15, runThreshold ≈ 2.6). **S64:** durante movimiento (Idle → Walk/Run), lanza moneda con probabilidad `flyChance` para activar vuelo ocasional; anima "Fly" mientras se mueve, aterriza "FlyDown" antes de volver a Idle. **S97:** Expone UnityEvents `onTakeOff` y `onFlyLand` para Feel (MMFeedbacks). **S98 NUEVO:** Único dueño del Animator en gameplay. Soporta gestos: `PlayGesture` (de un disparo), `HoldGesture` (sostenido), `StopGesture`, con estado `IsGesturing` e invariante `CanGesture` (solo si quieto + combate free). Giros dinámicos con clips laterales (Walk_L/R, Run_L/R, Fly_L/R) elegidos por tasa de giro suavizada con histéresis `turnThreshold`. Cedes control al Animator si `DragonAnimationDriver.IsBusy` (combate), silenciando cambios de estado. Refs serializadas al visualizer y agent, mismo GameObject.

## Campos Serializados

- `visualizer` (MonchiVisualizer, required) — acceso al animator y transforms
- `navAgent` (NavMeshAgent, required) — lectura de velocity
- `combatDriver` (DragonAnimationDriver, optional) — si busy → silencia ticks
- `walkThreshold` (float, default 0.15) — threshold de velocidad para transicionar a Walk
- `runThreshold` (float, default 2.6) — threshold de velocidad para transicionar a Run
- `crossFade` (float, default 0.2) — duración de transición CrossFade para locomoción (s)
- `flyChance` (float, default 0.25) — probabilidad [0,1] de vuelo cuando entra en movimiento
- `flyLandCrossFade` (float, default 0.25) — duración del aterrizaje "FlyDown" (s)
- `onTakeOff` (UnityEvent) — S97 disparado cuando inicia vuelo (flying = true)
- `onFlyLand` (UnityEvent) — S97 disparado cuando aterriza (flying = false)
- **S98 NUEVOS:**
  - `gestureCrossFade` (float, default 0.15) — duración de transición para gestos
  - `turnClips` (bool, default true) — habilita giros con clips laterales (Walk_L/R, etc.)
  - `turnThreshold` (float, default 70) — velocidad angular (°/s) para activar giro (histéresis × 0.5)
  - `turnSmoothing` (float, default 8) — suavizado exponencial de tasa de giro

## Campos Internos

- `currentState` (string) — estado de animación de locomoción (para evitar transiciones redundantes)
- `flying` (bool) — bandera de vuelo en este frame
- **S98 NUEVOS:**
  - `gestureState` (string) — estado de gesto activo (vacío = sin gesto)
  - `gestureUntil` (float) — Time.time hasta el que el gesto de un disparo sigue activo
  - `gestureHeld` (bool) — true si el gesto es sostenido (`HoldGesture`), false si es de un disparo
  - `lastYaw` (float) — ángulo Y en frame anterior (para calcular tasa de giro)
  - `yawRate` (float) — velocidad angular suavizada (°/s)
  - `turning` (bool) — bandera de si está girando actualmente (con histéresis)
  - `hasStateCache` (Dictionary<string, bool>) — cache de `HasState(anim, state)`
  - `clipLengthCache` (Dictionary<string, float>) — cache de duraciones de clips

## Propiedades

- `IsGesturing → bool` — true si hay gesto activo: `gestureState != "" && (gestureHeld || Time.time < gestureUntil)`. S98: usado para determinar si se puede iniciar otro gesto o si debe persistir la animación de gesto.
- `IsStill → bool` — true si rawTarget es "Idle" (velocity < walkThreshold). Calculado cada frame.
- **S98 NUEVOS:**
  - `CanGesture() → bool` — privado. Devuelve true solo si: no hay combate en curso (`combatDriver == null || !IsBusy`) AND `IsStill`. Bloquea gestos mientras se camina o combate.

## Métodos Públicos

- `PlayGesture(string state) → bool` — S98. Inicia gesto de un disparo (no sostenido). Requiere: poder gesticular (`CanGesture()`), que el estado exista en el Animator. Si ok: CrossFade inmediato, set `gestureState`, guarda `gestureUntil = Time.time + ClipLength(state)`, set `gestureHeld=false`, devuelve true. Si no se puede (combatiendo, caminando, sin estado), devuelve false.
- `HoldGesture(string state) → bool` — S98. Inicia o sostiene gesto. Si ya se sostiene el MISMO estado, devuelve true (idempotente). Si distinto o no sostenido: requiere `CanGesture()` y que estado exista. Si ok: CrossFade, set `gestureState`, set `gestureHeld=true`, devuelve true.
- `StopGesture()` — S98. Cancela gesto: vacía `gestureState`, `gestureHeld=false`, `currentState=""`. Al siguiente Update, vuelve a locomoción normal.
- `Update()` — tick principal: consulta velocity, decide transición de locomoción o gesto, dispara CrossFade, invoca UnityEvents.

## Ciclo de Transición S64 + S97 + S98

### Bloqueo de Combate (S97)
Si `combatDriver.IsBusy`, resetea todo (currentState, flying, gestureState, gestureHeld) y retorna sin cambios.

### Locomoción Base
1. Determina `rawTarget` según speed del navAgent:
   - speed < walkThreshold → "Idle"
   - walkThreshold ≤ speed < runThreshold → "Walk"
   - speed ≥ runThreshold → "Run"

2. Calcula `IsStill = (rawTarget == "Idle")`

### Gestión de Gestos (S98)
3. Si `gestureState != ""` (hay gesto activo):
   - Si `isMoving`, cancela: vacía gestureState y gestureHeld, resetea currentState
   - Else si gesto es de un disparo (`!gestureHeld`) y expiró (`Time.time >= gestureUntil`), limpiar
   - Else, retorna sin tocar locomoción (gesto en curso)

### Giros (S98)
4. Calcula tasa de giro suavizada:
   - `rawYawRate = DeltaAngle(lastYaw, currentYaw) / dt` (°/s)
   - `yawRate = Lerp(yawRate, rawYawRate, 1 - Exp(-turnSmoothing × dt))` (suavizado exponencial)

5. Histéresis de giro:
   - Si ya estaba girando: mantiene si `|yawRate| >= turnThreshold × 0.5`
   - Si no giraba: activa si `|yawRate| >= turnThreshold`
   - Se desactiva si `!turnClips || !isMoving`

### Vuelo (S64 + S97)
6. Si transiciona de Idle → movimiento: lanza moneda, `flying = Random.value < flyChance`
   - Si flying entra, invoca `onTakeOff?.Invoke()` para Feel

7. Si vuelve a Idle desde vuelo: CrossFade "FlyDown", resetea `flying=false`, invoca `onFlyLand?.Invoke()`, set currentState a "Idle", retorna

### Elección de Clip y Transición
8. Elige target base:
   - Si `isMoving && flying` → "Fly"
   - Else → `rawTarget`

9. Si giros activos:
   - Lateral = target + ("_R" si yawRate > 0, "_L" si < 0)
   - Si existe ese clip, usa lateral

10. Si target ≠ currentState, CrossFade y actualiza currentState

## Invariantes S98

- **Único dueño del Animator:** solo este componente hace `CrossFadeInFixedTime()` en gameplay normal. Si combate ocupa, respeta y se retira.
- **CanGesture bloquea:** gestos solo en quietud + sin combate. Esto evita conflictos de animación con locomoción.
- **PlayGesture vs HoldGesture:** uno dispara y expira (`gestureUntil`), otro sostiene indefinidamente (`gestureHeld=true`). Ambos vuelven a locomoción si se toca a mover o se llama `StopGesture()`.
- **Giros con histéresis:** evita parpadeo. Histéresis × 0.5 significa que salir del giro requiere menos velocidad que entrar (ej. 70 para entrar, 35 para salir).
- **Caché de estados y clips:** `HasState()` y `ClipLength()` cachean resultado la primera vez; esto agiliza el Loop principal.
- **Feel vía UnityEvents:** `onTakeOff` y `onFlyLand` son señales puras (cero side effects en gameplay); receptores: `MMF_Player` en prefab hijo `Feedbacks/`.
- **Idempotencia de gestos:** `HoldGesture()` con el mismo estado devuelve true sin re-disparar; seguro de llamar múltiples frames.

## Vinculado a

- [[Index/10 - Visualization]]
- [[Index/23 - Arena Sandbox y Expedicion]] (S97: Feel; S98: Gestos para beats)
- [[Index/14 - Social V1]]

## Conexiones

- [[MonchiVisualizer]] — acceso al animator y transforms
- [[DragonAnimationDriver]] — si busy, silencia transiciones
- [[MoriMochiAgent]] — prefab que lo contiene, llama `Update()` cada frame, puede wired gestos en eventos de beating
- [[MMF_Player]] — S97 receptor de `onTakeOff` y `onFlyLand` (hijo `Feedbacks/` del prefab)
- [[MonchiMoodDriver]] — S98 potencial suscriptor de `PlayGesture()` para emociones
