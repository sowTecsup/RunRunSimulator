---
tags: [script, combat, cinemachine]
---

# CombatCameraDirector.cs

**Ruta:** `Systems/CombatVisualizer/CombatCameraDirector.cs`

**Responsabilidad:** Orquesta las prioridades de las vcams Cinemachine durante el replay 3v3 (S43 NUEVO). En `OnActiveUnit` sube la vcam de la unidad activa a `activePriority=20` vía `CombatVisualizerService.VCamOf()`, reestableciendo la anterior a 0. La cámara de escena general (scenePriority=10) toma el mando en inicio/fin del combate.

**Responsabilidad ampliada:** Suscriptor de `CombatVisualEvents.OnActiveUnit/OnVisualCombatStart/OnVisualCombatEnd`. Mantiene estado de la última vcam activa (para reestablecer su prioridad al cambiar de unidad). Sin comportamiento de animación — solo gestiona prioridades (Cinemachine elige automáticamente la de prioridad mayor).

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `sceneCamera` | `Unity.Cinemachine.CinemachineCamera` | Ref a vcam de escena general (para inicio/fin combate) |
| `scenePriority` | `int` | Prioridad de cámara escena (default 10) |
| `activePriority` | `int` | Prioridad de unidad activa (default 20) — debe ser mayor que scenePriority |

## Campos Privados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `lastActive` | `Unity.Cinemachine.CinemachineCamera` | Ref a la última vcam que estaba activa (para reestablecer prioridad) |

## Métodos

| Método | Descripción |
|--------|-------------|
| `OnEnable()` | Suscribe a OnActiveUnit, OnVisualCombatStart, OnVisualCombatEnd |
| `OnDisable()` | Desuscribe eventos (regla 9: desuscribir en OnDisable) |
| `HandleStart(CombatVisualContext ctx)` | Sube sceneCamera a scenePriority, reseta lastActive |
| `HandleActiveUnit(CombatVisualSide side, int index)` | Baja lastActive a 0, obtiene vcam via VCamOf, sube a activePriority, guarda en lastActive |
| `HandleEnd(CombatVisualSide winner, bool isDraw)` | Baja lastActive a 0, reseta lastActive |

## Flujo de Prioridades

**Inicio (OnVisualCombatStart):**
```csharp
sceneCamera.Priority = scenePriority;  // 10
lastActive = null;
```

**Turno activo (OnActiveUnit):**
```csharp
var vcam = CombatVisualizerService.Instance.VCamOf(side, index);
if (lastActive != null && lastActive != vcam) lastActive.Priority = 0;
if (vcam != null) vcam.Priority = activePriority;  // 20
lastActive = vcam;
```

**Fin (OnVisualCombatEnd):**
```csharp
if (lastActive != null) lastActive.Priority = 0;
lastActive = null;
```

**Invariante Cinemachine:** La vcam con Priority más alta siempre es la activa. Sistema multi-vcam permite animaciones suaves entre cortes.

## Dependencias

- **CombatVisualizerService.Instance** — accedido para VCamOf(side, index) dentro HandleActiveUnit
- **CombatVisualEvents** — eventos estáticos (OnActiveUnit, OnVisualCombatStart, OnVisualCombatEnd)
- **CombatVisualUnits** — cada unit spawneada en S42/S43 tiene field VCam (CinemachineCamera child)

## S43 Novedades

- **Clase completa nueva (S43)** — S41/S42 no tenían gestión de vcam priorities
- **Patrón:** Suscriptor de eventos vs. hardcoded lookups (regla 1: comunicación vía bus)
- **Null-safe:** Chequea VCamOf() retorna null antes de setear Priority
- **Compatibilidad:** Usa Instance.VCamOf() — permite múltiples visualizers en futuro (actualmente uno singleton)

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]
- [[CombatVisualEvents]] — suscriptor (OnActiveUnit, OnVisualCombatStart, OnVisualCombatEnd)
- [[CombatVisualizerService]] — accede VCamOf() durante turno activo
- [[CombatVisualUnits]] — cada unit tiene VCam child (creada en Spawn, S42)

## Conexiones

**Entrada:**
- `CombatVisualEvents.OnActiveUnit(side, index)` — disparado cuando cambia el atacante del turno
- `CombatVisualEvents.OnVisualCombatStart(ctx)` — inicio replay
- `CombatVisualEvents.OnVisualCombatEnd(winner, isDraw)` — fin replay

**Salida:**
- `CinemachineCamera.Priority` — modificadas las prioridades (Cinemachine CM automáticamente cambia vcam activa)
- Impacto visual: cortes dinámicos de cámara por turno (cuando cambia atacante)

## Notas

- **Sin desacoplamiento de Scene refs:** sceneCamera debe estar wireado en escena (inspeccionable, no lookup)
- **Timing:** Cambio de vcam es instantáneo (sin lerp) — para suavidad, Cinemachine interpola posición/rotación post-cambio
- **Fallback:** Si VCamOf retorna null (unit no spawneada o destroyed), simplemente no sube nada (sceneCamera sigue)
- **Extensibilidad:** Valores scenePriority/activePriority serializados permiten tweaking (ej: spectator cam con prioridad 5)
