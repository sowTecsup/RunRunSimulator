---
tags: [script, prototype, anim, deuda]
---

# SpiderArmDriver.cs

**Ruta:** `Prototype/Spider/SpiderArmDriver.cs`

**Responsabilidad:** Controlador de brazos para el prototipo araña. Maneja poses discretas via `ConfigurableJoint` target rotation. Almacena poses rest en `Awake()` (rotaciones base actuales), expone métodos: `PoseRest()` (vuelve a postura neutral), `PoseArmsUp()` (brazos elevados, para celebración/buff), `Swipe(windupSeconds, strikeSeconds, onImpact)` (corrutina de ataque: windup → strike con callback onImpact a mitad de golpe).

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `armL` | `ConfigurableJoint` | Brazo izquierdo |
| `armR` | `ConfigurableJoint` | Brazo derecho |
| `armsUpEuler` | Vector3 | Rotación offset "brazos arriba" (default -70, 0, 0 = inclinado) |
| `windupEuler` | Vector3 | Pose windup ataque (default 50, 0, -30) |
| `strikeEuler` | Vector3 | Pose strike golpe (default -60, 0, 20) |

## Almacenamiento Interno

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `restL` | Quaternion | Pose rest original brazo izq (guardada en Awake) |
| `restR` | Quaternion | Pose rest original brazo der (guardada en Awake) |

## Métodos Públicos

| Método | Parámetros | Retorna | Descripción |
|--------|-----------|---------|-------------|
| `PoseRest` | — | void | Vuelve ambos brazos a pose rest original |
| `PoseArmsUp` | — | void | Aplica rotación offset armsUpEuler a pose rest |
| `Swipe` | float windupSeconds, float strikeSeconds, Action onImpact | Coroutine | Inicia corrutina de ataque |

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `SwipeRoutine` | Windup → espera → strike → callback onImpact a mitad → fin |

## Detalles de Swipe

1. **Windup:** Set brazo derecho a `windupEuler * restR` (50, 0, -30 = levantado), espera `windupSeconds`
2. **Strike:** Set a `strikeEuler * restR` (-60, 0, 20 = extendido golpe), espera `strikeSeconds * 0.5`
3. **Impact:** Dispara callback `onImpact()` a mitad del strike
4. **Recovery:** Espera `strikeSeconds * 0.5` y vuelve a `restR`

## Notas

- **Deuda técnica:** Script experimental para prototipo araña descartado. Eliminar cuando se implemente driver definitivo para modelo final.
- **Asimetría:** Solo controla brazo derecho en Swipe (brazo izquierdo se mantiene en armsUp si estaba en PoseArmsUp). Brazos pueden no ser simétricos.
- **Composición:** Usado por `SpiderAnimationDriver` para ejecutar ataque visual completo.
- **Seguridad null:** Cada método chequea `if (armL/R != null)` antes de tocar.

## Vinculado a

- [[Index/03 - Combat]] — prototipo de animación de combate
- [[SpiderAnimationDriver]] — consumidor

## Conexiones

**Consumido por:**
- `SpiderAnimationDriver` → llama en PlayAttack (Swipe), PlayBuff (PoseArmsUp/PoseRest), PlayVictory (PoseArmsUp), PlayIdle (PoseRest)
