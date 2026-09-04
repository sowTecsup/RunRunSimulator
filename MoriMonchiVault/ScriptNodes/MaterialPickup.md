---
tags: [script, world, expedition, recolectable]
---

# MaterialPickup.cs

**Ruta:** `World/Expedition/MaterialPickup.cs`

**Responsabilidad:** Recolectable de expedición: cristal mineral con valor entero. Requiere componente `Perceivable` del mismo GO (para que el agente lo vea). Expone interfaz simple: `Value`, `Taken`, `TryTake(out int)`, `Radius` (perezoso). **S98 NUEVO:** soporta `disableDelay` (corrutina antes de desactivar) y `UnityEvent onTaken` para Feel. **S99 NUEVO:** `Radius` se calcula lazy desde bounds del renderer (porque sandbox escala post-instancia), `standoffRadius` override serializado, `ApproachPoint()` calcula punto de llegada en el borde del mineral.

## Campos Serializados

- `value` (int, min 1, default 1) — puntos que otorga al tomarse
- **S98 NUEVOS:**
  - `disableDelay` (float, min 0, default 0) — segundos antes de desactivar el GO tras `TryTake()`. Si ≤ 0, desactiva inmediato; si > 0, espera con corrutina `DisableAfter()`
  - `onTaken` (UnityEvent) — dispara al recolectar (dentro de `TryTake()`, después de `Taken=true` pero antes de desactivar). Para Feel/VFX.
- **S99 NUEVO:**
  - `standoffRadius` (float, min 0, default 0) — override de radio: si > 0, usa este; si = 0, calcula lazy desde renderer bounds

## Propiedades

- `Value → int` — valor de material que otorga. Default 1. Solo lectura pública.
- `Taken → bool` — bandera de si ya fue recolectado.
- **S99 NUEVO:**
  - `Radius → float` — radio de contacto perezoso. Si `cachedRadius < 0` (sin computar aún), elige: si `standoffRadius > 0` úsalo; sino calcula `ComputeRadius()` (lee bounds del renderer hijo, max(extents.x, extents.z)). Default fallback si sin renderer: 0.5f. Se cachea una sola vez.

## Métodos Públicos

- `TryTake(out int taken) → bool` — recolecta el mineral. Si `Taken`, devuelve false. Sino: set `Taken=true`, guarda valor en out, **S98** invoca `onTaken?.Invoke()`, luego inicia desactivación (inmediato si `disableDelay ≤ 0`, corrutina si > 0). Desactivación dispara `OnDisable` en `Perceivable` que lo desregistra.
- **S99 NUEVO:** `ApproachPoint(Vector3 from, float margin) → Vector3` — calcula punto de llegada en el borde del mineral, visto desde `from`. Resta a `center = transform.position` el vector unitario de llegada (xz only), multiplica por `(Radius + margin)`, devuelve `center + dir * (Radius + margin)`. Si `from ≈ center`, usa `transform.forward` (xz) o fallback `Vector3.forward`.
- `SetValue(int newValue)` — setter interno: clampea a min 1.

## Campos Internos

- `cachedRadius` (float, default -1f) — cache de radio computado

## Ciclo de Vida

```csharp
OnEnable():
  (Perceivable.OnEnable() automático: registra en PerceivableRegistry si Kind=Material)

TryTake():
  1. Si Taken → return false
  2. Set Taken = true
  3. Invoke onTaken (S98, Feel)
  4. Si disableDelay <= 0 → gameObject.SetActive(false) (S98)
  5. Sino → StartCoroutine(DisableAfter(disableDelay)) (S98)

DisableAfter(seconds):  [S98 NUEVO]
  1. yield WaitForSeconds(seconds)
  2. gameObject.SetActive(false)

OnDisable():
  (Perceivable.OnDisable() automático: desregistra del registry)
```

## Invariantes S98 + S99

- **Perceivable requerido:** `[RequireComponent(typeof(Perceivable))]` asegura que exista siempre; sin él, no se percibe.
- **Desactivación = desregistro:** `gameObject.SetActive(false)` dispara `OnDisable` en `Perceivable`, que lo saca del registry; así el agente nunca intenta volver a un mineral ya tomado.
- **S98 Feel vía UnityEvent:** `onTaken` NO cambia estado ni velocidad (idempotente, puede firar múltiples veces); es solo para suscriptor Feel/VFX en prefab.
- **S99 Radius perezoso:** se calcula una sola vez y se cachea. Útil porque `ArenaSandbox` instancia mineral y lo activa, pero el sandbox padre escala DESPUÉS (cambios de escala late-binding). Override `standoffRadius` permite ajustar manualmente sin editar escala.
- **S99 ApproachPoint margin:** `margin` es buffer extra al borde (ej. ancho del agente). Si `margin=0`, toca exacto el borde; `margin>0` lo aleja un poco más para evitar overlap.
- **SetValue interno:** solo `ArenaSandbox.SpawnMinerals()` lo llama para diferenciar central (valor 5) de esquinas (valor default 1).
- **Idempotencia:** `TryTake()` dos veces devuelve false la segunda; seguro de llamar múltiples veces.

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]] (S98: Feel; S99: Radius perezoso y ApproachPoint)

## Conexiones

- [[Perceivable]] (requerido, se registra con `Kind=Material`)
- [[ArenaSandbox]] (instantiador, setter de valor, usa `ApproachPoint()` para navegar)
- [[AgentExpedition]] (lector, calcula score y ejecuta `TryTake()`)
- [[ArenaCueOverlay]] (lector para dibujo de halos, lee `.Taken`, `.Value`, `.Radius`)
- [[PerceivableRegistry]] (registro global automático vía Perceivable)
- **S98:** [[MMF_Player]] (receptor de `onTaken` si wired en prefab)
