---
tags: [script, world, expedition, recolectable]
---

# MaterialPickup.cs

**Ruta:** `World/Expedition/MaterialPickup.cs`

**Responsabilidad:** Recolectable de expedición: cristal mineral con valor entero. Requiere componente `Perceivable` del mismo GO (para que el agente lo vea). Expone interfaz simple: `Value`, `Remaining`, `Taken`, `TryMineUnit()`, `Radius` (perezoso). **S98 NUEVO:** soporta `disableDelay` (corrutina antes de desactivar) y `UnityEvent onTaken` para Feel. **S99 NUEVO:** `Radius` se calcula lazy desde bounds del renderer, `standoffRadius` override serializado, `ApproachPoint()` calcula punto de llegada en el borde del mineral. **S101 NUEVO:** usado en 3 contextos de ocupación: Gather (mina valores), Guard (se planta y vigila), Break (acecharé aquí esperando rival).

## Campos Serializados

- `value` (int, min 1, default 1) — puntos que otorga al tomarse. Seteable vía `SetValue()` para minerales central (valor alto, ej. 5) vs esquinas (valor bajo, ej. 1).
- **S98 NUEVOS:**
  - `disableDelay` (float, min 0, default 0) — segundos antes de desactivar el GO tras `TryMineUnit()` agotado. Si ≤ 0, desactiva inmediato.
  - `onTaken` (UnityEvent) — dispara al recolectar completamente.
- **S99 NUEVO:**
  - `standoffRadius` (float, min 0, default 0) — override de radio: si > 0, usa este; si = 0, calcula lazy desde renderer bounds.

## Propiedades

- `Value → int` — valor de material que otorga. Solo lectura pública.
- `Remaining → int` — unidades pendientes de recolectar. Decrece con `TryMineUnit()`.
- `Taken → bool` — bandera de si ya fue completamente recolectado (Remaining <= 0).
- **S99 NUEVO:**
  - `Radius → float` — radio de contacto perezoso (cacheado una sola vez).

## Métodos Públicos

- `TryMineUnit() → bool` — recolecta una unidad del mineral. Si `Taken`, devuelve false. Sino: decrementa `Remaining`, emite `onTaken` si Remaining <= 0, inicia desactivación. Usado por `AgentExpedition.Phase.Mining` cada `MiningSecondsPerUnit`.
- **S99 NUEVO:** `ApproachPoint(Vector3 from, float margin) → Vector3` — calcula punto de llegada en el borde del mineral visto desde `from`. Usado por `AgentExpedition.ApproachPoint()` para evitar apiñamiento.
- `SetValue(int newValue)` — setter interno: clampea a min 1. Llamado por `ArenaSandbox.SpawnMinerals()`.

## Ciclo de Vida

```csharp
OnEnable():
  (Perceivable automático: registra en PerceivableRegistry si Kind=Material)

TryMineUnit():
  1. Si Taken → return false
  2. Decrementa Remaining
  3. Si Remaining <= 0 → set Taken=true, Invoke onTaken
  4. Si disableDelay <= 0 → gameObject.SetActive(false)
  5. Sino → StartCoroutine(DisableAfter(disableDelay))

OnDisable():
  (Perceivable automático: desregistra del registry)
```

## Invariantes S101 + S98 + S99

- **Perceivable requerido:** `[RequireComponent(typeof(Perceivable))]` asegura registro.
- **Remaining es contador:** se decrementa 1 unidad por `TryMineUnit()`. Si `Value=5`, toma 5 llamadas vaciar.
- **Desactivación = desregistro:** al desactivarse, Perceivable.OnDisable lo saca del registry; agentes nunca vuelven.
- **S101 contextos:** Gather lo toma, Guard lo vigila, Break lo acecha esperando rival que llegue a tomarlo.
- **Radio perezoso:** se calcula una sola vez; útil porque sandbox escala post-instancia.
- **S99 ApproachPoint margin:** `margin` es buffer extra (ej. ancho del agente); `margin=0` toca exacto el borde.

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]

## Conexiones

- [[Perceivable]] (requerido)
- [[ArenaSandbox]] (instantiador, SetValue)
- [[AgentExpedition]] (lector, TryMineUnit, ApproachPoint)
- [[Occupation]] (contexto de Guard/Break/Decoy)
- [[PerceivableRegistry]] (automático)
