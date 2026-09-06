---
tags: [script, world, expedition, camera, input]
---

# ArenaCameraControl.cs

**Ruta:** `World/Expedition/ArenaCameraControl.cs`

**Responsabilidad:** Controlador de inputs en Play para cámara Cinemachine orbital. Mapea ratón + teclado a los ejes de `CinemachineOrbitalFollow`: scroll wheel → zoom (RadialAxis), botón derecho + movimiento → órbita horizontal (HorizontalAxis) y pitch vertical (VerticalAxis). Respeta límites Cinemachine (ClampValue). Tecla F alterna director de cámara. Tecla R restituye pose home (valores capturados en Awake). Contacto player → suspende director por `suspendSeconds` para evitar conflicto entre input manual y dramatización automática.

## Campos serializados

- **orbital:** referencia a CinemachineOrbitalFollow (Required)
- **director:** referencia a [[ArenaCameraDirector]] (opcional, para toggle F y suspensión)
- **zoomStep:** delta por scroll (default 0.08f, Min 0.01f)
- **orbitDegreesPerPixel:** sensibilidad horizontal (default 0.25°/px, Min 0.01f)
- **pitchDegreesPerPixel:** sensibilidad vertical (default 0.15°/px, Min 0.01f)
- **suspendSeconds:** duración de suspensión del director post-input (default 3f, Min 0f)

## Propiedades privadas (guardadas en Awake)

- **homeHorizontal, homeVertical, homeRadial** — posición inicial de ejes capturada en Awake

## Métodos públicos

- (Ninguno público; solo suscriptores internos a `Mouse.current` / `Keyboard.current`)

## Flujo (Update)

1. **Sanidad:** si orbital == null, retorna (seguridad)
2. **Scroll (zoom):** si Mouse.scroll.y ≠ 0
   - delta = -Mathf.Sign(scroll) * zoomStep
   - orbital.RadialAxis.Value = ClampValue(RadialAxis.Value + delta)
   - touched = true
3. **Botón derecho (órbita):**
   - si mouse.rightButton.isPressed y delta.sqrMagnitude > 0.01f:
     - HorizontalAxis += delta.x * orbitDegreesPerPixel (clamped)
     - VerticalAxis -= delta.y * pitchDegreesPerPixel (clamped, negado porque Y del ratón es inverso)
     - touched = true
4. **Tecla F:** si keyboard.fKey.wasPressedThisFrame → director.enabled = !director.enabled
5. **Tecla R:** si keyboard.rKey.wasPressedThisFrame → restituye (HorizontalAxis, VerticalAxis, RadialAxis) = (home*)
6. **Suspensión:** si touched y director ≠ null → director.Suspend(suspendSeconds)

## Invariantes S101

- homeHorizontal/homeVertical/homeRadial son inmutables tras Awake (captura única de pose inicial)
- ClampValue() es función de Cinemachine (respeta rango configurable per-eje)
- scroll.ReadValue().y es positivo al scroll up, negativo al scroll down → se invierte con -Mathf.Sign()
- VerticalAxis.Value se resta (pitch inverso respecto a movimiento del ratón)
- `touched` es local a cada frame (reset implícito)
- director.Suspend() puede ser llamado múltiples veces sin efecto acumulativo (solo extiende el timer)

## Conexiones

**Entrada:**
- Input System: `Mouse.current.scroll`, `Mouse.current.delta`, `Mouse.current.rightButton`
- Input System: `Keyboard.current.fKey`, `Keyboard.current.rKey`
- Lectura: `orbital.HorizontalAxis`, `orbital.VerticalAxis`, `orbital.RadialAxis` (para clamp y home)

**Salida:**
- Escritura: `orbital.HorizontalAxis.Value`, `orbital.VerticalAxis.Value`, `orbital.RadialAxis.Value`
- Llamada: `director.enabled = boolean`
- Llamada: `director.Suspend(float)` para histéresis

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]
- [[ArenaCameraDirector]]
- [[MoriMonchiVault/Index/12 - Unity MCP]] (quirks de Input System)
