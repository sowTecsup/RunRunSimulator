---
tags: [script, world, animations, dev, tooling]
---

# SlimeAnimLabLoop.cs

**Ruta:** `World/Creatures/SlimeAnimLabLoop.cs`

**Responsabilidad:** Bucle de reproducción automático para animaciones del slime en el banco de pruebas. Ejecuta un clip de animación (por nombre de estado) en bucle, con pausa configurable entre repeticiones y transiciones suaves vía `CrossFadeInFixedTime`. Usado en `SlimeAnimLab.unity` (escena de testing); vinculado a [[Index/30 - Huevos y Slimes (pipeline Blender)]] §5.

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `animator` | Animator | **[Required]** Animator del slime a reproducir (típicamente el root o cuerpo principal) |
| `state` | string | Nombre del estado a reproducir en bucle (e.g., "Idle", "Walk", "Eat") |
| `pause` | float | Pausa (segundos) entre repeticiones del clip (default 0.6) |
| `crossFade` | float | Duración de la transición suave al iniciar/volver a Idle (default 0.1) |

## Métodos Públicos

| Método | Descripción |
|--------|-------------|
| `Replay()` | Reinicia el bucle desde el principio (detiene la corrutina actual y crea una nueva) |
| `Label` (propiedad) | Extrae etiqueta legible del nombre del GO: si comienza con `Slime_`, devuelve el resto; si no, devuelve el nombre completo |

## State Internals

- `routine` (Coroutine) — referencia a la corrutina activa (init en OnEnable, limpieza en OnDisable)
- `replaying` (bool) — flag para forzar reproducción desde el fotograma 0 en la próxima iteración (usado por Replay)

## Flujo Interno

1. **OnEnable:** inicia `LoopRoutine()`
2. **LoopRoutine():**
   - Si `replaying == true`: reproduce desde fotograma 0 con `Play()` y pone `replaying = false`
   - Si no: usa `CrossFadeInFixedTime()` para transición suave
   - Espera hasta que el estado sea **Entered** (loop de hasta 30 frames comprobando hash)
   - Resuelve el clip actual (`ResolveClip()`)
   - Si el clip es looping (ej: Idle), sale del bucle; si no:
     - Espera `clip.length` segundos
     - Transiciona a Idle con `CrossFadeInFixedTime()`
     - Espera pausa
     - Repite
3. **OnDisable:** detiene la corrutina

## Métodos Privados

| Método | Descripción |
|--------|-------------|
| `Entered(int hash)` : bool | Verifica si el Animator ha entrando al estado solicitado (durante transición devuelve next state; si está ya, devuelve current state) |
| `ResolveClip()` : AnimationClip | Obtiene el clip actual: si está en transición, intenta next clip; si no, current clip. Devuelve null si no hay clip |

## Uso en Escena

Arrastrar como componente en un GO hijo dentro de la jerarquía del slime (ej: `Slime_Idle`, `Slime_Walk`). Asignar:
- `animator` → referencia al Animator root del slime
- `state` → nombre del estado en el controller (ej: "Idle")
- `pause` / `crossFade` → sintonización (dejar defaults si no hay razón)

El nombre del GO se usa para generar `Label` (botón en `SlimeAnimLabPanel`).

## Notas

- **Clips looping:** Si el clip marcado como looping, el bucle termina al entrarlo (sale del `LoopRoutine`); es responsabilidad del animator controller marcar clips no-looping si debe repetirse con pausa.
- **Transiciones:** La transición a Idle es fija; se ajusta `crossFade` para suavidad.
- **Quirk S133:** Si cambia el largo de un clip, el importador puede no actualizar el rango correctamente en `clipAnimations`; verificar `firstFrame`/`lastFrame` en el FBX importer.

## Vinculado a

- [[Index/30 - Huevos y Slimes (pipeline Blender)]] §5 (banco de pruebas de animaciones del slime)
- [[SlimeAnimLabPanel]] — panel que genera botones para este componente

## Conexiones

**Usado por:**
- `SlimeAnimLabPanel` → obtiene lista de loops, llama `Replay()` desde botones

**Depende de:**
- Animator (componente estándar de Unity, state machine con estados nombrados y clips)
