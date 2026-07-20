---
tags: [script, prototype, anim, deuda]
---

# SpiderAnimationDriver.cs

**Ruta:** `Prototype/Spider/SpiderAnimationDriver.cs`

**Responsabilidad:** Implementación procedural del contrato `MonchiAnimationDriver` para el prototipo araña. Orquesta animaciones complejas via corrutinas: `MoveTo()` aproxima (forward/turn via `controller.SetExternalDrive`), `PlayAttack()` se acerca al target (50/50 caminata/saltitos), gira para encarar, inyecta impulsos en escala (elastic) y pitch (motion), y ejecuta swipe de brazo (arms) con callback de impacto; `PlayHit()` squasea el cuerpo y echa pitch atrás; `PlayBuff()` posa brazos arriba, meandrea con SetExternalDrive, y salta; `PlayDefeat()` activa ragdoll e inyecta impulso vertical; `PlayVictory()` brazos arriba y bota sin fin; `PlayIdle()` interrumpe todo y resetea.

## Campos Serializados (Knobs de Comportamiento)

| Campo | Tipo | Default | Rango Típico | Descripción |
|-------|------|---------|--------------|-------------|
| `controller` | `SpiderBodyController` | — | — | Motor de locomoción |
| `jump` | `SpiderJump` | — | — | Sistema de salto |
| `elastic` | `SpiderElasticBody` | — | — | Deformación corporal |
| `ragdollMode` | `SpiderRagdollMode` | — | — | Switch ragdoll |
| `arms` | `SpiderArmDriver` | — | — | Swipper de brazos |
| `motion` | `SpiderBodyMotion` | — | — | Pivot visual (pitch/roll) |
| `rootBody` | `Rigidbody` | — | — | Physics para ragdoll |
| `arriveRadius` | float | 0.25 | [0.1, 0.5] | Distancia de llegada |
| `faceAngle` | float | 35 | [0, 90] | Umbral encarado (grados) |
| `attackWindupSeconds` | float | 0.35 | [0.1, 1] | Tiempo prep ataque |
| `attackStrikeSeconds` | float | 0.25 | [0.1, 1] | Tiempo golpe |
| `attackPunch` | float | 1.5 | [0.5, 3] | Magnitud impulso scale |
| `hitSquash` | float | 2.5 | [1, 5] | Squash por hit |
| `victoryBounceDelay` | float | 0.25 | [0.1, 1] | Delay entre saltos |
| `turnGain` | float | 0.04 | [0.01, 0.1] | Suavizado giro |
| `attackRange` | float | 1.1 | [0.5, 2] | Rango ataque |
| `approachTimeout` | float | 6 | [1, 10] | Timeout aproximación |
| `buffWiggleSeconds` | float | 0.15 | [0.05, 0.5] | Duración meneo |
| `buffCycles` | int | 3 | [1, 5] | Ciclos meneo buff |
| `attackLeanDegrees` | float | 28 | [10, 45] | Lean durante ataque |
| `hitLeanDegrees` | float | 22 | [5, 45] | Lean durante golpe |
| `defeatUpImpulse` | float | 5 | [2, 10] | Impulso vertical derrota |

## Máquina de Estados (via Corrutinas)

- **`MoveTo(dest)`:** Itera navega (forward/turn regulados) hasta llegar → dispara onArrived
- **`PlayAttack(targetPos)`:** Fase 1: aproxima con 50/50 walk/jump; Fase 2: encarado fino (timeout 1.5s); Fase 3: impulsos (elastic, motion pitch), swipe brazo (onImpact), fin
- **`PlayHit(intensity)`:** Instant: elastic squash inverso, motion pitch atrás, escalado por intensidad
- **`PlayBuff()`:** Posa brazos arriba → meandrea 3x con SetExternalDrive → salta → resetea brazos → fin
- **`PlayDefeat()`:** Activa ragdoll, impulso vertical aleatorio rotacional
- **`PlayVictory()`:** Brazos arriba → loop saltos infinito
- **`PlayIdle()`:** Resetea ragdoll (si está), brazos rest, interrumpe todo

## Métodos Públicos

| Método | Parámetros | Retorna | Descripción |
|--------|-----------|---------|-------------|
| `IsBusy` | — | bool | Propiedad: `current != null` |
| `MoveTo` | Vector3 dest, Action onArrived | void | Inicia corrutina MoveToRoutine |
| `PlayAttack` | Vector3 targetPos, Action onImpact, Action onFinished | void | Inicia PlayAttackRoutine |
| `PlayHit` | float intensity | void | Instant (sin corrutina) |
| `PlayBuff` | Action onFinished | void | Inicia PlayBuffRoutine |
| `PlayDefeat` | — | void | Instant |
| `PlayVictory` | — | void | Inicia PlayVictoryRoutine (loop) |
| `PlayIdle` | — | void | Interrumpe todo |

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `Interrupt()` | Para corrutina activa, limpia external drive, nullea current |
| `MoveToRoutine()` | Loop: busca ángulo a target, regula forward/turn, para cuando llega |
| `PlayAttackRoutine()` | Aproxima → encarado → impulsos + swipe |
| `PlayBuffRoutine()` | Brazos arriba → meandrea 3 ciclos → salta → brazos reposo |
| `PlayVictoryRoutine()` | Loop infinito de saltos |

## Notas

- **Deuda técnica (marcado para eliminar):** Script experimental para prototipo araña descartado. Cuando se implemente driver definitivo para modelo final, este se eliminará (depuración pendiente en fase de assets definitivos).
- **Corrutinas:** Usa `current` para rastrear, `Interrupt()` para limpiar con seguridad (evita múltiples corrutinas).
- **Sincronización de callbacks:** Los callbacks (onImpact, onFinished, onArrived) se invocan en momentos clave de la corrutina; el consumidor no debe asumir orden específico.
- **Desuscripción:** `OnDisable()` llama `Interrupt()` para evitar leaks.

## Vinculado a

- [[Index/03 - Combat]] — consumido por CombatVisualizer (aunque prototipo)
- [[MonchiAnimationDriver]] — implementa contrato
- Prototype/Spider (prototipo descartado)

## Conexiones

**Usa:**
- [[SpiderBodyController]] → SetExternalDrive/ClearExternalDrive
- [[SpiderElasticBody]] → AddImpulse (escala)
- [[SpiderBodyMotion]] → AddPitchImpulse (pitch)
- [[SpiderArmDriver]] → Swipe (ataque)
- [[SpiderJump]] → Jump (buff, victoria)
- [[SpiderRagdollMode]] → SetRagdoll (derrota)

**Consumido por:**
- Potencialmente `CombatVisualizer` (aunque actualmente no se usa; spider descartado)
