---
tags: [script, prototype, anim]
---

# SpiderElasticBody.cs

**Ruta:** `Prototype/Spider/SpiderElasticBody.cs`

**Responsabilidad:** Dueño EXCLUSIVO de `localScale` del pivote visual `BodyVisual`. Aplica resorte subamortiguado (parámetros `frequency` y `dampingRatio` serializados) que persigue target derivado de la velocidad vertical REAL del root (capturada cada frame vía diferencia de Y). La cantidad de compresión/estiramiento se controla por `tuning.elasticAmount` [0,1] (default 0.5), escalando el efecto de velocidad vertical. Conserva volumen: `xz = 1/sqrt(y)`, asegurando que la deformación parezca masa constante. Clamp final [0.55, 1.6] previene valores extremos. Guarda anti-teleport: ignora cambios de velocidad vertical >12 m/s. Resetea completamente en ragdoll (vuelve a pose base). Las posiciones y rotaciones del root las controla `SpiderBodyMotion`; este script SOLO toca scale. **NUEVO (S52):** `AddImpulse(velocity)` inyecta velocidad directa al resorte, permitiendo que animaciones (e.g., ataque, golpe, derrota) se traduzcan en compresión/estiramiento corporal sin scripting adicional.

**Notas de prototipo:** El resorte es smooth spring via classical dampened harmonic oscillator math (omega-based). Parámetros calibrados para que se sienta elástico pero no inestable. Detecta teleport por delta Y/dt para evitar popeos en discontinuidades de la escena.

**Cambios S50:** Script nuevo (primera implementación). Deformación elástica vía Hooke's law + conservación de volumen.

**Cambios S52:** Se agregó método público `AddImpulse(float velocity)` para inyectar velocidad del resorte desde código (e.g., impactos de ataque).

## Campos Serializados

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `tuning` | `SpiderTuningSO` | — | SO parámetros |
| `visualPivot` | `Transform` | — | Root visual a animar scale |
| `ragdollMode` | `SpiderRagdollMode` | — | Check ragdoll para reseteo |
| `frequency` | float | 3 | Hz resorte |
| `dampingRatio` | float | 0.25 | Factor amortiguación (0=no amort, 1=crítico) |
| `speedReference` | float | 3 | m/s referencia para normalizar vy |

## Almacenamiento Interno

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `baseScale` | Vector3 | Scale local visual base (guardada Awake) |
| `scaleValue` | float | Escala Y actual (multiplicador) |
| `scaleVelocity` | float | Velocidad resorte (para integración) |
| `lastY` | float | Posición Y frame anterior |

## Métodos Públicos

| Método | Parámetros | Retorna | Descripción |
|--------|-----------|---------|-------------|
| `AddImpulse` | float velocity | void | **S52 NEW** Inyecta velocidad al resorte (positivo = estiramiento, negativo = compresión) |

## Métodos Privados (Update)

### Estructura Update

1. **Guard:** Si visualPivot == null o tuning == null, salir
2. **Ragdoll check:** Si ragdoll activo, resetear todo y volver
3. **Velocidad vertical:** Calcula vy = (Y - lastY) / dt; ignora si |vy| > 12 m/s
4. **Target scale:** Calcula target = 1 + tuning.elasticAmount * 0.35 * Clamp(vy/speedReference, -1, 1)
   - vy positiva (salto) → scale Y > 1 (estiramiento)
   - vy negativa (caída) → scale Y < 1 (compresión)
5. **Resorte:** Integra damped harmonic oscillator (omega-based)
6. **Conservación volumen:** xz = 1/sqrt(scaleValue), mantiene volumen aparente
7. **Clamp y aplicar:** scaleValue [0.55, 1.6]; aplica a localScale

### Ecuación Resorte (Damped Harmonic Oscillator)

```
omega = 2π * frequency
scaleVelocity += (target - scaleValue) * omega² * dt
scaleVelocity -= scaleVelocity * 2 * dampingRatio * omega * dt
scaleValue += scaleVelocity * dt
scaleValue = Clamp(scaleValue, 0.55, 1.6)

xz = 1/sqrt(scaleValue)
localScale = (baseScale.x*xz, baseScale.y*scaleValue, baseScale.z*xz)
```

## Notas

- **Deuda técnica:** Prototipo araña descartado; sistema elástico no se usa en modelo final.
- **Conservación volumen:** Fórmula `xz = 1/sqrt(y)` asegura que deformación se ve como masa que se reordena, no gana/pierde volumen.
- **Parámetros:** frequency 3 Hz + dampingRatio 0.25 dan movimiento elástico responsive sin oscilación extrema.
- **Anti-teleport:** Ignora vy > 12 m/s para evitar popeos en discontinuidades (teletransportes, cambios de escena).
- **AddImpulse (S52):** Permite que SpiderAnimationDriver inyecte "golpes" sin código adicional — solo suma velocidad al resorte.

## Vinculado a

- [[Index/06 - Player & World]] — prototipo
- Prototype/Spider

## Conexiones

**Usa:**
- [[SpiderTuningSO]] → elasticAmount
- [[SpiderRagdollMode]] → check IsRagdoll

**Usado por:**
- [[SpiderAnimationDriver]] → AddImpulse (S52) en PlayAttack/PlayHit/PlayDefeat
