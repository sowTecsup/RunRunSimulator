---
tags: [script, world, expedition, camera]
---

# ArenaCameraDirector.cs

**Ruta:** `World/Expedition/ArenaCameraDirector.cs`

**Responsabilidad:** Director de cámara que modula dinámicamente el peso de targets en un grupo Cinemachine. Enfoca (focusWeight) a criaturas que están en estados "interesantes" (Clashing, Dazed, Airborne, Recovering) y sus objetivos de choque; fuera de eso, desenfoca (idleWeight) a los demás. Proporciona cámara "dramatizada" sin intervención manual. **S101:** Introduce `minSwitchSeconds` para histéresis (no cambia de foco más de una vez por intervalo, evita parpadeos). Método público `Suspend(float seconds)` para pausar temporalmente (pesa todos a focusWeight). `OnDisable()` restaura pesos a focusWeight (cleanup).

## Campos serializados

- **sandbox:** referencia a [[ArenaSandbox]] para acceder a criaturas
- **targetGroup:** referencia a CinemachineTargetGroup (componente que modula pesos)
- **idleWeight:** peso (0-1) para targets no interesantes (default 0.35, quietos de fondo)
- **focusWeight:** peso (0-1) para targets interesantes (default 1, enfocados)
- **focusHoldSeconds:** cuánto tiempo mantener enfoque después de que el estado deja de ser interesante (default 4s)
- **blendSpeed:** velocidad de transición entre pesos vía Lerp (default 0.7, 0-1 por frame)
- **minSwitchSeconds:** **S101 NUEVO:** tiempo mínimo entre cambios de foco (default 3s). Si dos targets se vuelven interesantes dentro del mismo intervalo, el segundo espera hasta que venza el timer.

## Campos privados

- `focusUntil` (Dict<Transform, float>) — Time.time hasta el que cada target debe mantener focusWeight
- `lastSwitch` (float) — Time.time del último cambio de foco
- **S101 NUEVO:** `lastSwitchFrame` (int) — frame del último cambio (para detectar múltiples Focus en el mismo frame)
- `suspendedUntil` (float) — Time.time hasta el que todos los targets pesan focusWeight

## Lógica (LateUpdate)

1. Por cada criatura en sandbox.Spawned:
   - Si está en estado "interesante" (IsAirborne, IsRecovering, Intent == Clashing/Dazed):
     - **S101:** Valida histéresis (minSwitchSeconds, permite múltiples en el mismo frame)
     - Marca su transform en focusUntil[transform] = now + focusHoldSeconds
   - Si está mirando un ClashTarget, también marca al target
2. Por cada target en targetGroup.Targets:
   - Interpola peso entre focusWeight e idleWeight
   - Si target está en focusUntil y aún activo (until > now), weight → focusWeight
   - Si no hay ningún focus activo, all weights → idleWeight
   - Si hay focus pero este target no está en él, weight → idleWeight

## Método público Suspend S101 NUEVO

```csharp
public void Suspend(float seconds) => suspendedUntil = Mathf.Max(suspendedUntil, Time.time + seconds);
```

**Propósito:** Pausa transiciones de cámara, mantiene todos los targets enfocados durante el tiempo especificado. Usado por:
- Cutscenes de arena
- Animaciones de victoria/derrota
- Efectos especiales que requieren cámara estable

**Efecto:** Mientras `now < suspendedUntil`, todos los targets reciben focusWeight (se ignoran cálculos de interés).

## Método OnDisable S101 NUEVO

```csharp
private void OnDisable()
{
    if (targetGroup == null) return;
    var targets = targetGroup.Targets;
    for (int i = 0; i < targets.Count; i++)
    {
        var t = targets[i];
        t.Weight = focusWeight;
        targets[i] = t;
    }
    focusUntil.Clear();
}
```

**Propósito:** Cleanup al destruir component. Restaura todos los pesos a focusWeight (estado neutro, visible) y limpia diccionario.

## S101: Histéresis con minSwitchSeconds

**Línea 16: Campo nuevo**

```csharp
[SerializeField, Min(0f)] private float minSwitchSeconds = 3f;
```

**Línea 19-20: Campos privados para tracking**

```csharp
private float lastSwitch = -999f;
private int   lastSwitchFrame = -1;
```

**Línea 57-66: Lógica de histéresis en Focus()**

```csharp
private void Focus(Transform t, float now)
{
    bool already = focusUntil.TryGetValue(t, out float until) && until > now;
    if (!already)
    {
        bool sameFrame = Time.frameCount == lastSwitchFrame;
        if (!sameFrame && now - lastSwitch < minSwitchSeconds) return;
        if (!sameFrame) { lastSwitch = now; lastSwitchFrame = Time.frameCount; }
    }
    focusUntil[t] = now + focusHoldSeconds;
}
```

**Significado:**
- Si el target ya estaba enfocado, solo refresca su timer (sin validar histéresis)
- Si es un target nuevo:
  - Si en el mismo frame que el último cambio: permite (sameFrame = true, no valida minSwitchSeconds)
  - Si en frame diferente:
    - Si `now - lastSwitch < minSwitchSeconds`: rechaza (espera hasta vencer el cooldown)
    - Si vencido: acepta, actualiza lastSwitch y lastSwitchFrame
- Razón: Evita cambios de cámara constantes (parpadeo); permite múltiples targets "nuevos" en el mismo frame (p.ej. dos MoriMonchis chocan simultáneamente)

## Valores en Escena S101

```
idleWeight       = 0.35  (quietos de fondo, visibles pero no enfocados)
focusWeight      = 1.0   (enfocados completamente)
focusHoldSeconds = 4.0   (gracia: mantiene foco 4s tras fin de acción)
blendSpeed       = 0.7   (fade suave, ~0.7 unidades/frame)
minSwitchSeconds = 3.0   (espera 3s entre cambios de foco)
```

## Efecto

- Cámara suavemente enfoca al combatiente y su rival cuando chocan
- Se desenfoca gradualmente (4s de gracia) cuando termina la acción
- Histéresis evita cambios de cámara continua si hay muchas acciones simultáneas
- Suspend() permite pausar dinamismo cuando se necesita cámara estable
- Mantiene balance entre acción y ambiente

## Invariantes S101

- focusUntil es un Dictionary que se limpia implícitamente en próxima actualización (solo vive este frame)
- No hay reset explícito; la lógica "olvida" targets automáticamente al no encontrarlos en el siguiente LateUpdate
- `anyFocus` previene que todos los targets se desenfoquen si hay transición rápida
- **S101:** minSwitchSeconds SOLO valida targets nuevos; targets ya enfocados refrescan su timer sin cooldown
- **S101:** Múltiples targets en el mismo frame Always cuentan como un solo "cambio" (no se repite el cooldown)
- **S101:** OnDisable restaura focusWeight (seguridad si component se disables antes de terminar sesión)

## Conexiones

**Entrada:**
- **Lectura:** sandbox.Spawned → agent.IsAirborne, agent.IsRecovering, agent.Intent, agent.ClashTarget
- **Escritura:** targetGroup.Targets[i].Weight (Cinemachine)

**Salida:**
- CinemachineTargetGroup ajusta blend de cámara en tiempo real

## Vinculado a

- [[Index/23 - Arena Sandbox y Expedicion]]
- [[ArenaSandbox]]
- [[MoriMochiAgent]]
- [[AgentClash]]
