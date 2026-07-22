---
tags: [script, combat, cinemachine, visualization]
---

# CombatCameraDirector.cs

**Ruta:** `Systems/CombatVisualizer/CombatCameraDirector.cs`

**Responsabilidad:** **S61b SIMPLIFICADO:** Conmuta prioridades de 3 cámaras Cinemachine estáticas (sceneCamera, allyCamera, enemyCamera) según etapa del turno emitida por `OnPhase(phase, actorSide)`. Sin seguimiento por unidad activa — las cámaras son fijas por tablero. Suscriptor de `OnPhase`, `OnVisualCombatStart`, `OnVisualCombatEnd`.

**Vs. antes S61b:** Eliminada lógica de seguimiento por unidad activa (VCamOf, lastActive, activePriority). Ahora pasivo: solo gestiona prioridades Cinemachine según fase del turno.

## Campos Serializados

| Campo | Tipo | Descripción |
|-------|------|-------------|
| `sceneCamera` | `Unity.Cinemachine.CinemachineCamera` | Ref a vcam general (escena completa, prio base 10) |
| `allyCamera` | `Unity.Cinemachine.CinemachineCamera` | **S61b NEW** Ref a vcam tablero A (aliados), default prio 0 |
| `enemyCamera` | `Unity.Cinemachine.CinemachineCamera` | **S61b NEW** Ref a vcam tablero B (enemigos), default prio 0 |
| `scenePriority` | `int` | Prioridad sceneCamera base (default 10) |
| `phasePriority` | `int` | Prioridad de etapa (default 30) — usada por allyCamera/enemyCamera en Passives/Attack |

## Métodos

| Método | Descripción |
|--------|-------------|
| `OnEnable()` | Suscribe a OnVisualCombatStart, OnVisualCombatEnd, OnPhase |
| `OnDisable()` | Desuscribe eventos |
| `HandleStart(CombatVisualContext ctx)` | Inicio combate: sceneCamera a scenePriority, allyCamera/enemyCamera a 0 |
| `HandleEnd(CombatVisualSide winner, bool isDraw)` | Fin combate: allyCamera/enemyCamera a 0, sceneCamera queda en scenePriority |
| `HandlePhase(CombatTurnPhase phase, CombatVisualSide actorSide)` | **S61b NEW** Conmuta cámaras según etapa del turno |

## Flujo S61b (por etapa del turno)

**OnVisualCombatStart (inicio):**
```csharp
sceneCamera.Priority = scenePriority;      // 10 — cámara general activa
allyCamera.Priority = 0;
enemyCamera.Priority = 0;
```

**OnPhase(Passives, actorSide):**
- Si `actorSide == A`: 
  - `allyCamera.Priority = phasePriority;` (30) — tablero A visible
  - `enemyCamera.Priority = 0;`
- Si `actorSide == B`:
  - `enemyCamera.Priority = phasePriority;` (30) — tablero B visible
  - `allyCamera.Priority = 0;`
- **Propósito:** Mostrar tablero del actor durante sus pasivas aliadas

**OnPhase(Attack, actorSide):**
- Si `actorSide == A`:
  - `enemyCamera.Priority = phasePriority;` (30) — tablero B visible (objetivo)
  - `allyCamera.Priority = 0;`
- Si `actorSide == B`:
  - `allyCamera.Priority = phasePriority;` (30) — tablero A visible (objetivo)
  - `enemyCamera.Priority = 0;`
- **Propósito:** Mostrar tablero del defensor durante ataque

**OnPhase(Rest, _):**
```csharp
allyCamera.Priority = 0;
enemyCamera.Priority = 0;
// sceneCamera sigue en scenePriority (10) — cámara general
```
- **Propósito:** Volver a vista general entre turnos, al inicio/fin combate

**OnVisualCombatEnd (fin):**
```csharp
allyCamera.Priority = 0;
enemyCamera.Priority = 0;
// sceneCamera mantiene scenePriority (10)
```

## Invariante Cinemachine

- **La vcam con Priority más alta siempre es activa**
- Cinemachine CM automáticamente interpola (blend 0.6s configurado en escena) entre cortes
- Sin lerp manual — transiciones suaves por interpolación nativa

## Dependencias Removidas (S61b)

- **RIP CombatVisualizerService.VCamOf()** — ya no accedida
- **RIP lastActive field** — ya no necesario
- **RIP activePriority field** — lógica reemplazada por phasePriority (global para etapas)
- **RIP HandleActiveUnit()** — no suscribe a OnActiveUnit, event removido

## Vinculado a

- [[Index/03 - Combat System]]
- [[Index/13 - Combat Design Direction]]

## Conexiones

**Entrada:**
- `CombatVisualEvents.OnPhase(phase, actorSide)` — **S61b PRIMARY** disparado por CombatVisualizerService.ForwardRoutine()
- `CombatVisualEvents.OnVisualCombatStart(ctx)` — inicio replay
- `CombatVisualEvents.OnVisualCombatEnd(winner, isDraw)` — fin replay

**Salida:**
- `CinemachineCamera.Priority` — modificadas prioridades (CM automáticamente cambia vcam activa)
- Impacto visual: cortes dinámicos de cámara por etapa (pasivas → ataque → rest)

## Notas S61b

- **Diseño pasivo:** Service emite eventos, Director escucha y reacciona — desacoplamiento limpio
- **Sin State:** Director no mantiene estado (lastActive, etc.) — stateless
- **Configuración flexible:** scenePriority y phasePriority serializados para tuneable por Juan
- **Null-safe:** Chequea referencias de cámaras antes de setear Priority
- **Fallback:** Si una ref es null, simplemente ignora (nada crash)

## Cambios vs. S43

**S43 (antigua versión):**
- VCamOf(side, index) retornaba vcam de unidad específica
- OnActiveUnit disparaba cambio de unidad activa
- lastActive mantenía estado previo
- activePriority valor individual por unit

**S61b (simplificación):**
- OnPhase event-driven, sin VCamOf lookups
- 3 cámaras estáticas pre-wireadas en escena
- Prioridades globales (scenePriority, phasePriority)
- State-free — Handler puro reactivo
