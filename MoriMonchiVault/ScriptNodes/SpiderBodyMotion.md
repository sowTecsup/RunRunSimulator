---
tags: [script, prototype, anim]
---

# SpiderBodyMotion.cs

**Ruta:** `Prototype/Spider/SpiderBodyMotion.cs`

**Responsabilidad:** Anima solo el pivote visual `BodyVisual` (no el root del controller). Aplica: respiración (idle sinusoidal), movimiento procedural (Perlin noise idle pitch/roll), bob (onda por velocidad actual), lean (inclinación por velocidad forward y yaw rate), y **NUEVO (S52):** impulsos de acción (pitch momentáneo con resorte subamortiguado). Toma velocidad 3D de la cadera, yaw rate, y aplica suavizado exponencial. Se apaga totalmente en ragdoll (resetea a base pose). Usa `SpiderTuningSO.idleAmount`, `bobAmount`, `leanAmount` como factores. **NUEVO (S52):** `AddPitchImpulse(degrees)` inyecta inclinación momentánea (positivo = adelante, negativo = atrás) con resorte subamortiguado parametrizado por `actionFrequency` y `actionDamping`, permitiendo que animaciones (e.g., ataque, golpe recibido) le den expresión corporal sin scripting complejo.

**Notas de prototipo:** Solo modifica `BodyVisual.localPosition` y `BodyVisual.localRotation`; el root lo mueve `SpiderBodyController`. Frecuencias internas: respiration 1.4Hz, bob 9Hz (scaled by speed). Action impulse usa classical damped harmonic oscillator (omega-based).

**Cambios S50:** Script existente con movimiento base.

**Cambios S52:** Se agregó sistema de impulso de acción (actionPitch + actionPitchVelocity). Se agregó método público `AddPitchImpulse(float degrees)` para inyectar inclinación momentánea. Se agregaron campos serializados `actionFrequency` (default 2.5) y `actionDamping` (default 0.35) para calibración.

## Campos Serializados

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `tuning` | `SpiderTuningSO` | — | SO parámetros |
| `visualPivot` | `Transform` | — | Root visual a animar |
| `ragdollMode` | `SpiderRagdollMode` | — | Check ragdoll para reseteo |
| `breatheFrequency` | float | 1.4 | Hz respiración idle |
| `bobFrequency` | float | 9 | Hz bob movimiento |
| `maxSpeedReference` | float | 1.5 | m/s referencia bob |
| `actionFrequency` | float | 2.5 | **S52 NEW** Hz resorte impulso |
| `actionDamping` | float | 0.35 | **S52 NEW** Factor amortiguación impulso |

## Almacenamiento Interno

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `basePos` | Vector3 | Posición local visual base (guardada Awake) |
| `baseRot` | Quaternion | Rotación local visual base (guardada Awake) |
| `lastPos` | Vector3 | Posición frame anterior (para vel) |
| `lastYaw` | float | Yaw frame anterior (para yaw rate) |
| `vel` | Vector3 | Velocidad suavizada |
| `yawRate` | float | Tasa giro suavizada |
| `bobPhase` | float | Fase bob (acumulada) |
| `pitch` | float | Inclinación forward/back (lean) |
| `roll` | float | Inclinación lateral (lean) |
| `actionPitch` | float | **S52 NEW** Inclinación acción momentánea |
| `actionPitchVelocity` | float | **S52 NEW** Velocidad resorte inclinación acción |

## Métodos Públicos

| Método | Parámetros | Retorna | Descripción |
|--------|-----------|---------|-------------|
| `AddPitchImpulse` | float degrees | void | **S52 NEW** Inyecta impulso pitch momentáneo (positivo = adelante) |

## Métodos Privados (Update)

### Estructura Update

1. **Guard:** Si visualPivot == null o tuning == null, salir
2. **Ragdoll check:** Si ragdoll activo, resetear todo y volver
3. **Velocidad:** Calcula vel 3D suavizada del root y yaw rate suavizado
4. **Idle:** Respiration sinusoidal + Perlin noise pitch/roll
5. **Bob:** Sinusoide por velocidad actual (escala con speed01)
6. **Lean:** Forward pitch (fwd speed) + lateral roll (yaw rate)
7. **Action impulse:** Resorte subamortiguado hacia 0, clampea [-45, 45]
8. **Aplicar:** Posición = basePos + bob/breathe; Rotación = Euler(pitch+idlePitch+actionPitch, 0, roll+idleRoll)

### Ecuación Resorte Impulso (Damped Harmonic Oscillator)

```
omega = 2π * actionFrequency
actionPitchVelocity += (0 - actionPitch) * omega² * dt
actionPitchVelocity -= actionPitchVelocity * 2 * actionDamping * omega * dt
actionPitch += actionPitchVelocity * dt
actionPitch = Clamp(actionPitch, -45, 45)
```

Permite inclinaciones suaves con overshoot controlado por `actionDamping`.

## Notas

- **Deuda técnica:** Prototipo araña descartado; sistema de impulso no se usa en modelo final.
- **Composición:** Impulso se suma al pitch final = lean + idle + action, permitiendo múltiples capas de animación.
- **Anti-teleport:** Ignora vel vertical > 12 m/s para evitar popeos en discontinuidades.
- **Parámetros calibrados:** actionFrequency 2.5 Hz + actionDamping 0.35 dan sensación "blanda" pero responsiva.

## Vinculado a

- [[Index/06 - Player & World]] — prototipo
- Prototype/Spider

## Conexiones

**Usa:**
- [[SpiderTuningSO]] — factores animation
- [[SpiderRagdollMode]] → check IsRagdoll

**Usado por:**
- [[SpiderAnimationDriver]] → AddPitchImpulse (S52) en PlayAttack/PlayHit
