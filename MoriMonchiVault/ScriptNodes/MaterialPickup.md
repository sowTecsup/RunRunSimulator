---
tags: [script, world, expedition, recolectable]
---

# MaterialPickup.cs

**Ruta:** `World/Expedition/MaterialPickup.cs`

**Responsabilidad:** Recolectable de expedición: cristal mineral con valor entero. Requiere componente `Perceivable` del mismo GO (para que el agente lo vea). Expone interfaz simple: `Value`, `Taken`, `TryTake(out int)`. Al tomar, se marca como `Taken=true` y se desactiva el GameObject; con eso su `Perceivable` también se desregistra automáticamente de la percepción global.

## Métodos Públicos

- `TryTake(out int taken) → bool` — intenta tomar el material. Si ya está `Taken`, devuelve false. Si es la primera vez, set `Taken=true`, guarda el valor en `taken`, desactiva el GO, y devuelve true. El desactive dispara `OnDisable` en `Perceivable`, que lo desregistra.
- `SetValue(int newValue)` — setter interno para `Value` (usado por `ArenaSandbox` para minerales de esquina vs central). Clampea a min 1.

## Propiedades

- `Value → int` — valor de material que otorga el mineral. Default 1. Solo lectura pública.
- `Taken → bool` — bandera de si ya fue recolectado.

## Campos Configurables (Inspector)

- `value` (int, min 1, default 1) — puntos que otorga al tomarse.

## Ciclo de Vida

```csharp
Start():
  (sin comportamiento propio)

OnEnable():
  (Perceivable.OnEnable() automático: registra en PerceivableRegistry si Kind=Material)

OnDisable():
  (Perceivable.OnDisable() automático: desregistra del registry)
```

## Invariantes S97

- **Perceivable requerido:** `[RequireComponent(typeof(Perceivable))]` asegura que exista siempre; sin él, no se percibe.
- **Desactivación = desregistro:** `gameObject.SetActive(false)` dispara `OnDisable` en `Perceivable`, que lo saca del registry; así el agente nunca intenta volver a un mineral ya tomado.
- **SetValue interno:** solo `ArenaSandbox.SpawnMinerals()` lo llama para diferenciar central (valor 5) de esquinas (valor default 1).
- **Idempotencia:** `TryTake()` dos veces devuelve false la segunda; seguro de llamar múltiples veces.

## Vinculado a

[[Index/23 - Arena Sandbox y Expedicion]]

## Conexiones

- [[Perceivable]] (requerido, se registra con `Kind=Material`)
- [[ArenaSandbox]] (instantiador, setter de valor)
- [[AgentExpedition]] (lector, calcula score y ejecuta `TryTake()`)
- [[ArenaCueOverlay]] (lector para dibujo de halos, lee `.Taken` y `.Value`)
- [[PerceivableRegistry]] (registro global automático vía Perceivable)
