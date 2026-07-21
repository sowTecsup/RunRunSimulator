---
tags: [script, animation, combat, rig]
---

# DragonAnimationDriver.cs

**Ruta:** `World/DragonAnimationDriver.cs`

**Responsabilidad:** Implementación Animator-based del contrato `MonchiAnimationDriver` para rig Suriyun (S58+). Orquesta corrutinas de movimiento y combate con callbacks precisos. PlayAttack es coreografía voladora completa: anticipación con retroceso → despegue FlyUp → vuelo Fly → ataque FlyFire con onImpact al 45% + hit-stop (Anim.speed=0) → regreso volando → aterrizaje FlyDown con rotación restaurada. SetTimeScale(float) escala Animator.speed y todas las esperas vía Scaled() — central para sync con replay playback. MoveTo camina hacia destino con giro continuo. PlayHit dispara Damage. PlayBuff dispara Yes. PlayDefeat va a Die. PlayVictory dispara Roar once. ClipLength() busca clip más corto que contenga nombre de estado (fuzzy match ignorando guiones bajos). Setea moods del visualizer durante acciones (Enojado ataque, Dolor daño, Neutral reposo).

## Métodos Públicos (Contrato MonchiAnimationDriver)

| Método | Parámetros | Descripción |
|--------|-----------|-------------|
| `IsBusy` | — | Propiedad (bool): true si hay corrutina activa |
| `SetTimeScale(float)` | `value` | **S59 NEW** Escala Animator.speed y todas esperas (Scaled). Clamp 0.25–4 |
| `PlayAttack(Vector3, Action, Action)` | `targetPosition, onImpact, onFinished` | Coreografía voladora: anticipación + despegue + vuelo + golpe (onImpact 45%) + hit-stop + retorno + aterrizaje |
| `PlayHit(float)` | `intensity` | Dispara anim Damage, setea mood Dolor |
| `PlayBuff(Action)` | `onFinished` | Dispara anim Yes, dispara onFinished al terminar |
| `PlayDefeat()` | — | Va a anim Die terminal, silenció corrutina |
| `PlayVictory()` | — | Dispara Roar once luego loop Jump |
| `PlayIdle()` | — | Interrumpe todo, fade Idle |
| `MoveTo(Vector3, Action)` | `destination, onArrived` | Camina con giro continuo hacia destino, dispara onArrived |

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `visualizer` | `MonchiVisualizer` | Ref al modelo para SetMood y GetComponent Animator |
| `moveSpeed` | `float` | Velocidad Walk (default 2.2) |
| `turnSpeed` | `float` | Velocidad rotación (degrees/sec, default 540) |
| `arriveDistance` | `float` | Tolerancia llegada Move (default 0.06) |
| `attackImpactFraction` | `float` | Fracción FlyFire que onImpact dispara (default 0.45 = 45%) |
| `anticipationSeconds` | `float` | **S59** Duración anticipación pullback (default 0.3) |
| `anticipationPullback` | `float` | **S59** Distancia retroceso anticipación (default 0.35) |
| `takeoffSeconds` | `float` | Duración FlyUp (default 0.3) |
| `flightHeight` | `float` | Altura vuelo y ataque (default 1.1) |
| `flightSpeed` | `float` | Velocidad vuelo Fly (default 5.5) |
| `strikeDistance` | `float` | Distancia antes de target donde golpear (default 1.1) |
| `hitStopSeconds` | `float` | Duración hit-stop (Anim.speed=0) al impacto (default 0.12) |
| `landSeconds` | `float` | Duración FlyDown retorno a home (default 0.28) |

## Estados Animator (S59)

```
Idle (default)
Walk (looping, MoveTo)
Damage (hit anim)
Die (terminal, PlayDefeat)
Yes (PlayBuff)
Roar (PlayVictory inicial)
Jump (PlayVictory loop)
FlyUp (despegue, PlayAttack fase 1)
Fly (vuelo, PlayAttack fase 2–4)
FlyFire (ataque, PlayAttack fase 3 — onImpact @ 45%)
FlyDown (aterrizaje, PlayAttack fase 5)
```

## Coreografía PlayAttack (S59)

**Fase 1 — Anticipación:**
1. FaceTowards(targetPosition) — gira hacia target
2. SetMood(Enojado)
3. Calcula pullbackPos = homePos - forward * anticipationPullback
4. MoveLinear(homePos → pullbackPos, anticipationSeconds*0.6, EaseOut)
5. WaitForSeconds(anticipationSeconds*0.4)

**Fase 2 — Despegue:**
6. Fade("FlyUp", 0.1)
7. Calcula riseEnd = riseStart + Y flightHeight
8. MoveLinear(riseStart → riseEnd, takeoffSeconds, EaseOut)

**Fase 3 — Vuelo:**
9. Fade("Fly", 0.15)
10. Calcula strikePos = flatTarget - attackDir * strikeDistance, elevado a flightHeight
11. MoveAtSpeed(strikePos, flightSpeed, faceTarget=targetPosition)

**Fase 4 — Golpe (Hit-Stop):**
12. Fade("FlyFire", 0.08)
13. len = ClipLength("FlyFire")
14. WaitForSeconds(len * attackImpactFraction = len*0.45)
15. **onImpact?.Invoke()** ← aquí dispara callback
16. Anim.speed = 0 (hit-stop)
17. WaitForSeconds(hitStopSeconds)
18. Anim.speed = timeScale (restaura)
19. WaitForSeconds(len * (1-0.45) = len*0.55)

**Fase 5 — Retorno:**
20. Fade("Fly", 0.15)
21. returnPos = homePos elevado a flightHeight
22. MoveAtSpeed(returnPos, flightSpeed, faceTarget=homePos)

**Fase 6 — Aterrizaje:**
23. Fade("FlyDown", 0.1)
24. MoveLinearAndRotate(landStart, homePos, rotStart, homeRot, landSeconds, EaseIn)
25. SetMood(Neutral)
26. Fade("Idle")
27. **onFinished?.Invoke()** ← callback final

## Métodos Privados Clave

| Método | Descripción |
|--------|-------------|
| `Begin(IEnumerator)` | Inicia corrutina, mata anterior si existe |
| `Finish()` | Setea current = null (marca no-busy) |
| `Scaled(float seconds)` | **S59** Retorna seconds / timeScale (escala esperas) |
| `ClipLength(string stateName)` | Busca clip match fuzzy (ignorando guiones bajos), retorna length o 1.0 default |
| `Fade(string state, float blend)` | Animator.CrossFadeInFixedTime con transición smooth |
| `FaceTowards(Vector3)` | Corrutina que lerpa rotación hacia target (yaw flat) |
| `MoveLinear(from, to, duration, ease)` | Corrutina lerp position lineal con easing |
| `MoveLinearAndRotate(...)` | Corrutina lerp posición + rotación simultáneamente |
| `MoveAtSpeed(from, to, speed, faceTarget)` | Corrutina move a velocidad constante + giro continuo |
| `MoveToRoutine(...)` | Estructura: FaceTowards → Fade Walk → MoveAtSpeed loop → Fade Idle → onArrived |
| `PlayAttackRoutine(...)` | Estructura 6 fases como describió arriba |
| `PlayHitRoutine()` | SetMood Dolor → Fade Damage → WaitForSeconds(ClipLength) → SetMood Neutral → Fade Idle |

## Cambios S59

**SetTimeScale(float) override:**
- Línea 51–57:
  ```csharp
  public override void SetTimeScale(float value)
  {
      timeScale = Mathf.Clamp(value, 0.25f, 4f);
      if (Anim != null)
          Anim.speed = timeScale;
  }
  ```
- Clamp al rango [0.25, 4]
- Escala Animator.speed directamente (todas las animaciones)
- Todas las esperas usan Scaled() → se dividen por timeScale

**PlayAttack anticipación:**
- Línea 16–17: knobs nuevos `anticipationSeconds`, `anticipationPullback`
- Línea 229–231: pullback retroceso hacia atrás antes de despegue
  ```csharp
  Vector3 pullbackPos = homePos - transform.forward * anticipationPullback;
  yield return MoveLinear(homePos, pullbackPos, anticipationSeconds * 0.6f, EaseOut);
  yield return new WaitForSeconds(Scaled(anticipationSeconds * 0.4f));
  ```
- Propósito: feedback visual anticipatorio (como en pasivas del replay)

**Hit-Stop en FlyFire:**
- Línea 259–265:
  ```csharp
  yield return new WaitForSeconds(Scaled(len * attackImpactFraction));
  onImpact?.Invoke();
  if (Anim != null) Anim.speed = 0f;  // Hit-stop
  yield return new WaitForSeconds(Scaled(hitStopSeconds));
  if (Anim != null) Anim.speed = timeScale;
  yield return new WaitForSeconds(Scaled(len * (1f - attackImpactFraction)));
  ```
- Pausa animador durante hitStopSeconds (feel crunchy del golpe)

**ClipLength fuzzy match:**
- Línea 70: `string compactName = clip.name.Replace("_", string.Empty);`
- Match case-insensitive ignorando guiones bajos
- Retorna el clip más corto que coincida (preferencia por exactitud)

**Scaled() central:**
- Línea 45: `private float Scaled(float seconds) => seconds / timeScale;`
- Todas las WaitForSeconds, MoveLinear durations usan Scaled()
- Resultado: replay @ 2x speed = animaciones 2x rápido (aunque WaitForSeconds se divida por 2)

## Vinculado a

- [[Index/13 - Combat Design Direction]]
- [[MonchiAnimationDriver]] — contrato base; **S59** SetTimeScale override
- [[CombatVisualizerService]] — **S59** llama SetTimeScale(Speed) vía PushTimeScale
- [[MonchiVisualizer]] — SetMood(mood) durante acciones

## Conexiones

**Consumido por:**
- `CombatVisualUnits.Spawn()` → obtiene driver via GetComponent<MonchiAnimationDriver>()
- `CombatVisualizerService.ForwardRoutine()` → llama PlayAttack/PlayHit/PlayDefeat/PlayBuff/PlayVictory/PlayIdle
- **S59:** `CombatVisualizerService.PushTimeScale()` → propaga playback speed

**Interfaz:**
- Implementa `MonchiAnimationDriver` (abstract)
- PlayAttack/PlayHit/PlayBuff/etc. firman callbacks exactos para orquestación replay

## Notas S59

- Coreografía voladora completa: anticipación + despegue + vuelo + golpe + retorno + aterrizaje
- Hit-stop: pausa animador 0.12s al impacto (juicy feedback)
- OnImpact: dispara al 45% del clip FlyFire (timing determinista para damage sync)
- Scaled(): central para replay speed — todas las esperas y velocidades escaladas
- ClipLength fuzzy: flexible para assets con nombres variados
- Moods: Enojado (ataque), Dolor (daño), Neutral (reposo) — visual feedback continuidad
