---
tags: [script, world, animation, feel]
---

# MonchiSquashDriver.cs

**Ruta:** `World/Creatures/MonchiSquashDriver.cs`

**Responsabilidad:** Deformación 3D squash & stretch (S103) reemplazando MMF_Scale de Feel. Maneja dos Transform: `pivot` (escala + rotación) y `counter` (contrarotación para hijos). Estira según velocidad (minSpeed threshold), aplica impulsos instantáneos (pulses) vía spring solver (stiffness, damping) en respuesta a eventos (throw, bounce, land, getUp, clashTell, clashHit). El eje de estiramiento sigue la dirección de movimiento o forward en impactos. Estado reset en `OnDisable`.

**Parámetros:**
- `pivot`, `counter` [Required] — Transforms de articulación
- `minSpeed` [Min(0)] = 3 — threshold para activar estiramiento por velocidad
- `stretchPerSpeed` [Min(0)] = 0.045 — escala extra por u/s sobre minSpeed
- `maxStretch` [Range(1, 2.5)] = 1.6 — tope del estiramiento
- `stretchSmoothing` [Min(0)] = 14 — velocidad de cambio de escala
- `axisSmoothing` [Min(0)] = 18 — velocidad de rotación del eje
- `springStiffness` [Min(0)] = 260, `springDamping` [Min(0)] = 10 — dinámicas del resorte
- Pulsos: `throwPulse=0.28`, `bouncePulse=-0.3`, `landPulse=-0.38`, `getUpPulse=0.18`, `tellPulse=-0.14`, `hitPulse=0.32` — offset instantáneo (+ = stretch, - = squash)

**Métodos internos:**
- `LateUpdate()` — calcula targetStretch, integra spring, aplica escala final
- `Apply(float s)` — escala pivot como (side=1/√s, s, side), invierte rotación en counter
- `Kick(float amount)` — dispara impulso (pulse + velVelocity=0)
- `OnThrow/Bounce/Land/GetUp/ClashTell/ClashHit()` — listeners que llaman `Kick()` con pulso apropiado

**Physics:**
- Spring solver: stepsize 1/120s, dt máximo 0.034s
- Escala final clamped [0.45, 1.9]
- Settle cuando |s-1| < 0.004 y |velVelocity| < 0.02

**S103:** Reemplaza animaciones de escala procedurales. Suscrito a `agent` events en OnEnable, desuscrito en OnDisable. El torque de knockback en AgentPhysics.Knock() genera rotación visual coordinada.

**Vinculado a:** [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]], [[Index/10 - Visualization]]

**Conexiones:** [[MoriMochiAgent]], [[AgentPhysics]], [[MonchiVisualizer]]
