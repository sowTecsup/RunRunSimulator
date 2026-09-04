---
tags: [script, world, expedition, camera]
---

# ArenaCameraDirector.cs

**Ruta:** `World/Expedition/ArenaCameraDirector.cs`

**Responsabilidad:** Director de cámara que modula dinámicamente el peso de targets en un grupo Cinemachine. Enfoca (focusWeight) a criaturas que están en estados "interesantes" (Clashing, Dazed, Airborne, Recovering) y sus objetivos de choque; fuera de eso, desenfoca (idleWeight) a los demás. Proporciona cámara "dramatizada" sin intervención manual.

## Campos serializados

- **sandbox:** referencia a [[ArenaSandbox]] para acceder a criaturas
- **targetGroup:** referencia a CinemachineTargetGroup (componente que modula pesos)
- **idleWeight:** peso (0-1) para targets no interesantes (default 0.15, quietos de fondo)
- **focusWeight:** peso (0-1) para targets interesantes (default 1, enfocados)
- **focusHoldSeconds:** cuánto tiempo mantener enfoque después de que el estado deja de ser interesante (default 2.5s)
- **blendSpeed:** velocidad de transición entre pesos vía Lerp (default 2f, 0-1 por frame)

## Lógica (LateUpdate)

1. Por cada criatura en sandbox.Spawned:
   - Si está en estado "interesante" (IsAirborne, IsRecovering, Intent == Clashing/Dazed):
     - Marca su transform en focusUntil[transform] = now + focusHoldSeconds
   - Si está mirando un ClashTarget, también marca al target
2. Por cada target en targetGroup.Targets:
   - Interpola peso entre focusWeight e idleWeight
   - Si target está en focusUntil y aún activo (until > now), weight → focusWeight
   - Si no hay ningún focus activo, all weights → idleWeight
   - Si hay focus pero este target no está en él, weight → idleWeight

## Efecto

- Cámara suavemente enfoca al combatiente y su rival cuando colisionan
- Se desenfoca gradualmente (2.5s de gracia) cuando termina la acción
- Mantiene balance entre acción y ambiente

## Invariantes S100

- focusUntil es un Dictionary que se limpia implícitamente en próxima actualización (solo vive este frame)
- No hay reset explícito; la lógica "olvida" targets automáticamente al no encontrarlos en el siguiente LateUpdate
- `anyFocus` previene que todos los targets se desenfoquen si hay transición rápida

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
