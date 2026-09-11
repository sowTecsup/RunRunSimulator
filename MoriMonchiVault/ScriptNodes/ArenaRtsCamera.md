---
tags: [script, camera, world]
---

# ArenaRtsCamera.cs

**Ruta:** `World/Expedition/ArenaRtsCamera.cs`

**Responsabilidad:** Cámara estilo RTS (League of Legends) con paneo, zoom y confinamiento al polígono de la sala. Lee teclado (WASD/flechas), ratón (borde, rueda, botón central arrastrable), y puede bloquearse a un agente objetivo (space o si hay director pinned). El zoom acelera el paneo. Los límites se aplican con resistencia suave al acercarse al borde (no es un muro duro). Recupera la vista inicial cada nueva sala (eje largo alineado, pivot a 0.3 del camino spawn→centro).

**Vinculado a:** [[Index/06 - World Architecture]], S114

**Conexiones:** [[ArenaSandbox]], [[ArenaLayoutBuilder]], [[ArenaShapeAxes]], [[MoriMochiAgent]]

**Campos Configurables (Odin)**

| Sección | Campos |
|---------|--------|
| Vista | `pitch` 56°, `zoomMin`/`zoomMax`/`zoomStart`, `zoomStep`, `zoomSmoothing` |
| Paneo | `panSpeed`, `edgePanPixels`, `edgePanEnabled`, `panSmoothing` |
| Límite | `boundsInset`, `boundsOvershoot`, `boundsSpring`, `boundsResistance` |
| Bloqueo | `followSmoothing` (cuando sigue un agente) |

**Métodos Privados Clave**

| Método | Descripción |
|--------|-------------|
| `ResetView()` | Calcula yaw inicial por eje de la sala y posiciona pivot |
| `ComputeInitialView()` | Orienta hacia el centro desde spawn del jugador |
| `ResolveFollowTarget()` | Busca agente del equipo Player para bloquear |
| `ApplyBounds()` | Resorte que rechaza el pivot fuera de la sala |
| `ApplyResistance()` | Amortigua deltas de movimiento al acercarse al borde |
| `SignedDistance()` | Distancia con signo a la malla del polígono |

**Detalles S114**

Inicio: `ResetView()` al detectar nueva semilla o al presionar R. La proyección usa `Quaternion.Euler(pitch, yaw, 0)` y la cámara se coloca a `pivot - rotation * Vector3.forward * zoom`. Paneo suave: `Lerp` exponencial con `panSmoothing` para evitar tirones. Física de límites: calcula gradiente de distancia firmada, aplica muelle si está fuera de `boundsInset`. Resolución de conflictos: seguimiento (`followTarget`) desactiva paneo manual.
