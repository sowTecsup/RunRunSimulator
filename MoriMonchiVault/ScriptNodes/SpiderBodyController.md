---
tags: [script, prototype, core]
---

# SpiderBodyController.cs

**Ruta:** `Prototype/Spider/SpiderBodyController.cs`

**Responsabilidad:** Orquestador principal del prototipo de araña. Lee input WASD directo de `Keyboard.current` + propiedades `AutoWalk`/`AutoTurn` para verificación, o inputs **codificados** via `SetExternalDrive(forward, turn)` si está activo. Computa flag `turning` a partir del eje de giro (umbral |turn| > 0.01). Mantiene el root (rotación, translación forward, altura por raycast a ground). Integra con `SpiderJump`: suma `jump.HeightOffset` al raycast height para permitir salto. Implementa selector de pata "most-overdue" por grupo de gait: itera legs, busca la que más quiera pisar (máximo Drag) dentro de su grupo de gait sin bloquear otros grupos activos. Tickea cada `SpiderLegStepper` pasando `mayStep` (verdadero solo si es candidata) y `turning` (flag de giro actual). **NUEVO (S52):** Flag `externalMotion` que cambia el sentido del `turning`: si true, `turning` se computa del yaw observado (rate > 25°/s) en lugar de input directo (permite que el gait reaccione a movimiento impuesto externamente).

**Notas de prototipo:** Escena aislada sin action maps; lee directo de Input System nuevo por comodidad de prueba. En juego real se usaría action map. Gizmos de debug muestran raycast y grupos de gait por color. El flag `turning` gatealiza el comportamiento de anticipación de torsión en las patas. External drive = modo "conducción por código" para animaciones o pruebas.

**Cambios S50:** Se agregó ref serializada `SpiderJump jump`; en el snap de altura por raycast ahora suma `jump.HeightOffset` a `hit.point.y + ride`.

**Cambios S52:** Se agregaron métodos `SetExternalDrive(forward, turn)` y `ClearExternalDrive()` para inyección de input desde código (e.g., animaciones). Se agregó flag `externalMotion` que muda el cálculo de `turning`: si true, `turning` se basa en yaw rate observado (>25°/s) en lugar de input de giro, permitiendo que el gait reaccione a rotación impuesta externamente.

## Campos Serializados

| Campo | Tipo | Default | Descripción |
|-------|------|---------|-------------|
| `tuning` | `SpiderTuningSO` | — | SO con parámetros de locomotion |
| `jump` | `SpiderJump` | — | Sistema de salto (S50) |
| `legs` | `SpiderLegStepper[]` | — | Array de piernas |
| `legIks` | `SpiderLegIK[]` | — | Array de IK solvers |
| `gaitGroup` | `int[]` | [0,1,2] | Grupos de gait por índice pata |
| `moveSpeed` | float | 1.5 | Velocidad marcha (fallback si no tuning) |
| `turnSpeed` | float | 90 | Velocidad giro °/s |
| `rideHeight` | float | 0.55 | Altura de la cadera |
| `groundMask` | LayerMask | 1 | Mask para raycast ground |
| `rayUp` | float | 2 | Offset raycast hacia arriba |
| `rayLength` | float | 8 | Largo raycast |
| `drawGizmos` | bool | true | Debug visualización |
| `autoWalk` | bool | false | Walking continuo (alt a WASD) |
| `autoTurn` | float | 0 | Giro continuo (alt a WASD) |
| `externalMotion` | bool | false | **S52 NEW** Mode gait reactivo a movimiento impuesto |

## Propiedades Públicas

| Propiedad | Tipo | Descripción |
|-----------|------|-------------|
| `AutoWalk` | bool { get; set; } | Activa/desactiva caminata automática |
| `AutoTurn` | float { get; set; } | Establece giro automático |
| `ExternalMotion` | bool { get; set; } | **S52 NEW** Activa modo gait reactivo a yaw observado |

## Métodos Públicos

| Método | Parámetros | Retorna | Descripción |
|--------|-----------|---------|-------------|
| `SetExternalDrive` | float forward, float turn | void | **S52 NEW** Inyecta forward/turn desde código (p.ej. animación); clampea a [-1, 1] |
| `ClearExternalDrive` | — | void | **S52 NEW** Borra input externo, vuelve a teclado |

## Lógica de Input (Update)

**Precedencia:**
1. Si `externalMotion` true: ignorar teclado, calcular `turning` de yaw rate observado
2. Si `useExternalDrive` true: usar `externalForward/externalTurn` en lugar de teclado
3. Si no external: leer WASD directo de `Keyboard.current`
4. `autoWalk` fuerza forward = 1
5. `autoTurn` != 0 fuerza turn = Clamp(autoTurn, -1, 1)

**Cálculo de `turning`:**
- Si `externalMotion` false: `turning = |turn| > 0.01` (del input)
- Si `externalMotion` true: `turning = |yawRate| > 25°/s` (observado del root)

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `Update()` | Loop principal: input → física → selector gait → tick legs + IK |
| `OnDrawGizmos()` | Debug: raycast ground, forward vector, grupo gait coloreado |

## Notas

- **External Drive:** Permite que animaciones (e.g., `SpiderAnimationDriver`) controlen directamente el movimiento sin perder la integración IK.
- **External Motion:** Nuevo modo (S52) que permite que el gait reaccione inteligentemente a movimiento impuesto (e.g., si algo exterior rota la araña, el gait anticipa la torsión vía yaw rate observado).
- **Deuda técnica:** Prototipo araña descartado; cuando se implemente locomotion definitiva para modelo final, este se eliminará.
- **Seguridad null:** Valida legs/legIks antes de iterar.

## Vinculado a

- [[Index/06 - Player & World]] — prototipo
- Prototype/Spider

## Conexiones

**Usa:**
- [[SpiderTuningSO]] — parámetros
- [[SpiderLegStepper]] → Tick(mayStep, turning)
- [[SpiderLegIK]] → SolveTo(footPosition)
- [[SpiderJump]] → HeightOffset (S50)

**Usado por:**
- [[SpiderAnimationDriver]] → SetExternalDrive/ClearExternalDrive (S52)
- [[SpiderDevPanel]] → AutoWalk property, botón Launch
