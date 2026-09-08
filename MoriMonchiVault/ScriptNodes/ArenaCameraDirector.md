---
tags: [script, world, expedition, camera]
---

# ArenaCameraDirector.cs

**Ruta:** `World/Expedition/ArenaCameraDirector.cs`

**Responsabilidad:** Director de cámara que modula dinámicamente el peso de targets en un grupo Cinemachine. Enfoca (focusWeight) a criaturas que están en estados "interesantes" (Clashing, Dazed, Airborne, Recovering) y sus objetivos de choque; fuera de eso, desenfoca (idleWeight) a los demás. Proporciona cámara "dramatizada" sin intervención manual. **S101:** Introduce `minSwitchSeconds` para histéresis (no cambia de foco más de una vez por intervalo, evita parpadeos). Método público `Suspend(float seconds)` para pausar temporalmente. `OnDisable()` restaura pesos. **S107:** Añade Pin/Unpin/TogglePin para seleccionar un target manualmente y mantenerlo enfocado; `pinIdleWeight` controla peso de otros durante pinning.

## Campos serializados

- **sandbox:** referencia a [[ArenaSandbox]] para acceder a criaturas
- **targetGroup:** referencia a CinemachineTargetGroup (componente que modula pesos)
- **idleWeight:** peso (0-1) para targets no interesantes (default 0.15)
- **focusWeight:** peso (0-1) para targets interesantes (default 1)
- **focusHoldSeconds:** cuánto tiempo mantener enfoque después de que el estado deja de ser interesante (default 2.5s)
- **blendSpeed:** velocidad de transición entre pesos vía Lerp (default 2)
- **minSwitchSeconds:** tiempo mínimo entre cambios de foco (default 3s)
- **pinSeconds:** duración de pinning automático (default 8s); si > 0, Pinned se desactiva tras este tiempo. Si = 0, pinning indefinido.
- **pinIdleWeight:** peso de otros targets mientras Pinned está activo (default 0, invisible) — S107 NUEVO

## Campos privados

- `Pinned` (MoriMochiAgent, público) — target actualmente pinned por UI (ej. tarjeta clickeada)
- `pinUntil` (float) — Time.time hasta el que Pinned mantiene enfoque. Si pinSeconds <= 0, PositiveInfinity.
- `focusUntil` (Dict<Transform, float>) — Time.time hasta el que cada target debe mantener focusWeight (S101)
- `lastSwitch`, `lastSwitchFrame` (float, int) — histéresis S101
- `suspendedUntil` (float) — Time.time hasta el que todos los targets pesan focusWeight

## Lógica (LateUpdate)

1. ValidatePin(now) — si Pinned no está activo en jerarquía o pinUntil venció, Unpin()
2. Por cada criatura en sandbox.Spawned:
   - Si está en estado "interesante" (IsAirborne, IsRecovering, Intent == Clashing/Dazed): Focus(transform, now)
   - Si está mirando un ClashTarget, también Focus(target.transform, now)
3. Calcula `anyFocus` — hay algún target en focusUntil aún activo
4. Por cada target en targetGroup.Targets:
   - Si Pinned != null: desired = (t.Object == Pinned.transform ? focusWeight : pinIdleWeight)
   - Sino si suspended || !anyFocus || focused: desired = focusWeight
   - Sino: desired = idleWeight
   - Interpola t.Weight → desired con blendSpeed

## Métodos Públicos (S107)

- `void Pin(MoriMochiAgent agent)` — fija Pinned = agent, pinUntil = now + pinSeconds (o ∞ si pinSeconds <= 0)
- `void TogglePin(MoriMochiAgent agent)` — Pin si Pinned != agent, Unpin sino
- `void Unpin()` — Pinned = null (cámara vuelve a dinámica)
- `void Suspend(float seconds)` — pausa transiciones (S101)

## Métodos Privados

- `void Focus(Transform t, float now)` — marca transform como interesante con histéresis minSwitchSeconds (S101 lógica)
- `void ValidatePin(float now)` — valida que Pinned siga activo; limpia si destruido o tiempo vencido
- `void OnDisable()` — cleanup: restaura pesos a focusWeight, Unpin()

## Integración

- TogglePin() llamado desde ArenaRoundHud.OnCardTapped() vía ArenaHudCard click
- Pinned leído desde ArenaCueOverlay.LateUpdate() para reveal state de rivales (director.Pinned == controller.Agent)
- Pinned leído desde ArenaRoundHud para actualizar clase CSS "hud-card--selected" / "hud-chip-rival--selected"

## S107 Cambios

- **Campos nuevos:**
  - `Pinned` (propiedad pública, get)
  - `pinIdleWeight` (serializado, [0,1], default 0)
  - `pinUntil` (privado)
  - `ValidatePin()` método privado

- **Lógica de pesos modificada:**
  - Si Pinned != null: aplica pinIdleWeight a todos excepto Pinned (permite focus selectivo)
  - Else: lógica anterior (dinámica automática)

## Invariantes S107

- **Pin permanente:** si pinSeconds <= 0, Pinned nunca expira automáticamente. Unpin() único método de salida.
- **Pin temporal:** si pinSeconds > 0, ValidatePin() expira Pinned tras ese tiempo
- **pinIdleWeight = 0:** otros targets completamente invisibles mientras Pinned. pinIdleWeight = 0.35: visibles pero desenfocados.
- **ValidatePin() llamado cada LateUpdate:** verifica gameObject.activeInHierarchy y timer, desactiva Pinned automáticamente si vencido o destruido

## Valores en Escena (S101/S107)

```
idleWeight       = 0.15  (quietos de fondo, visibles pero no enfocados)
focusWeight      = 1.0   (enfocados completamente)
focusHoldSeconds = 2.5   (gracia: mantiene foco 2.5s tras fin de acción)
blendSpeed       = 2.0   (fade suave)
minSwitchSeconds = 3.0   (espera 3s entre cambios de foco automáticos)
pinSeconds       = 8.0   (Pinned expira tras 8s)
pinIdleWeight    = 0.0   (otros invisibles mientras Pinned)
```

## Vinculado a

- [[Index/23 - Arena Sandbox & Expedicion (S102-S103)]]

## Conexiones

**Entrada:**
- sandbox.Spawned → agent.IsAirborne, agent.IsRecovering, agent.Intent, agent.ClashTarget
- ArenaRoundHud.OnCardTapped() → TogglePin()
- ArenaCueOverlay → lee Pinned para reveal

**Salida:**
- targetGroup.Targets[i].Weight (Cinemachine)
- Pinned público (lectura por HUD/Overlay)
