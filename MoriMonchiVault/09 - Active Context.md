---
tags: [memory-bank, active, session]
---

# 09 — Active Context

> Esta nota se actualiza CADA SESIÓN. Refleja qué estoy programando ahora mismo, qué archivos toco, y cuáles son los próximos pasos.

## Sesión actual

**Fecha**: 2026-06-06  
**Foco**: Sistema de Combate — refactor UI CombatPanel + fixes cloud + detalle de criatura extendido.

### Qué se hizo (esta sesión)

**HP × 5 en combate:**
- `CombatService.cs`: `private const float BaseHpCombatMultiplier = 5f;` aplicado en `ComputeStats`. Solo en runtime, no almacenado.
- `process-matchmaking.js` y `run-combat.js`: misma lógica `hp: (dna.BaseHP || 5) * 5 + bonuses`.

**Pool isolation — instant vs timer:**
- `run-combat.js` usa `instant_pool` (Custom Data key). `enqueue-combat.js` + `process-matchmaking.js` usan `matchmaking_pool`. Pools 100% separadas → un instante no interfiere con el queue de timer.
- `get-queue-status.js` y `dequeue-combat.js` cubren **ambas** keys.

**Fix: timer-queued creatures wrongly dequeued (bug raíz: multi-key getCustomItems):**
- `get-queue-status.js` reescrito: lee cada pool con su propio `getCustomItems([key])` en un loop. Devuelve `ok: false` si alguna lectura falla.
- `AsyncCombatService.FetchQueuedIdsAsync`: devuelve `null` si `resp == null || !resp.Ok` → `ReconcileGhostsAsync` se salta si la vista es parcial.
- `SemaphoreSlim(1,1) enqueueGate` en `AsyncCombatService` para serializar llamadas cloud concurrentes.

**Server manda timestamp real:**
- JS scripts envían `Date: new Date().toISOString()` en `buildResult`.
- `AsyncCombatService.ApplyResult` usa `ParseUtcOrNow(r.Date)` para `CombatRecord.Date`.

**CreatureDNA.QueuedAt:**
- Campo `public DateTime QueuedAt` (metadata display-only, no entra al DNA string). Se setea en `EnqueueInternal`.

**CombatPanelUITK — reestructuración (ahora 4 tabs):**
- Tab 3 "Resultados": solo muestra criaturas en cola + countdown al próximo :00 UTC + hora de encolado ("encolado HH:mm") por fila.
- Tab 4 "Historial": lista global de combates pasados, filtro por criatura (DropdownField), panel derecho con log turno a turno replayable.

**MorimonchiDetailInfoUITK — tabs implementadas:**
- **Combate**: foldout por pelea (más reciente primero), coloreado Win/Lose, turno a turno.
- **Linaje**: árbol hacia arriba (yo → padres → abuelos), chips con swatch de color, criaturas muertas/ausentes resueltas desde su UniqueID.
- **Breed**: árbol hacia abajo (yo → parejas → crías por pareja), escanea el registry por `MotherID`/`FatherID`.
- **Info**: sección Personalidad agregada (nombre + descripción en español).

**Redeploy requerido (pendiente):**
```
ugs deploy CloudCode/run-combat.js
ugs deploy CloudCode/process-matchmaking.js
ugs deploy CloudCode/get-queue-status.js
ugs deploy CloudCode/dequeue-combat.js
```

## Próximos pasos (retomar acá la próxima sesión)

**Pendientes de código — combate:**
- Batalla instantánea: mostrar `"Instantánea"` en lugar del countdown (la criatura instant no espera ningún cron).
- Ordenar lista de Resultados (Tab 3) de más antiguo a más nuevo por `QueuedAt`.
- Mejorar el sistema de renderizado de árboles de descendencia/ascendencia (scroll + nodos grandes → layout más compacto o canvas scrollable).
- **Prewarm de pool de MoriMonchis** (`MoriMochiSpawner`): instanciar X agentes vacíos al inicio para que el primer spawn no haga `Instantiate` en caliente.
- Redeploy cloud: `run-combat.js`, `process-matchmaking.js`, `get-queue-status.js`, `dequeue-combat.js`.

**Setup de escena (tuyo — código listo):**
- **NameTag**: crear objeto hijo en prefab de criatura; agregar `UIDocument` (WorldUIPanelSettings + `NameTagUITK.uxml`); posicionar ~1.2u arriba; cablearlo en `MoriMochiAgent.nameTag`.
- **Estaciones** (`Feeder`/`RestZone`/`PlayZone`): agregar hijos vacíos como use points en los prefabs.
- `PlacementGrid`: asignar `Floor Mask` + ajustar `Max Slope Angle`.
- `BuildModeController`: asignar `Obstacle Mask`.
- `MoriMochiSpawner`: asignar `launchPoint`; tunear `launchAngle`, `launchForce`, `spawnInterval`, `startDelay`, `spawnPerTick`.

**Pendientes de código — world:**
- Bloquear `TryLift` de un corral ocupado en `BuildModeController`/`FurnitureService`.
- Cablear `FlushToCloud()` en el logout de `CloudSyncService`.
- Futuro: petting directo (E sobre criatura); recursos consumibles en estaciones; muerte por inanición; decay offline.

## Archivos tocados esta sesión

| Archivo | Por qué |
|---------|---------|
| `Systems/Combat/CombatService.cs` | `BaseHpCombatMultiplier = 5f` en `ComputeStats` |
| `Systems/Combat/AsyncCombatService.cs` | `SemaphoreSlim` gate + `ok` flag + `QueuedAt` + `ParseUtcOrNow` |
| `Data/CreatureDNA.cs` | Campo `QueuedAt` (DateTime, display-only) |
| `UI/CombatPanelUITK.cs` | Tabs 3 (queue + clock) y 4 (historial) completas |
| `UI/MorimonchiDetailInfoUITK.cs` | Tabs Combate / Linaje / Breed / Personalidad implementadas |
| `UI Toolkit/CombatPanelUITK.uxml` | Tab 3 reestructurada + Tab 4 agregada |
| `UI Toolkit/CombatPanelUITKStyle.uss` | Estilos clock + queue + historial |
| `UI Toolkit/MorimonchiDetailInfoUITK.uxml` | Tabs combat-history / lineage-tree / breed-tree |
| `UI Toolkit/MorimonchiDetailInfoUITKStyle.uss` | Estilos foldout combat + tree chips |
| `CloudCode/run-combat.js` | `instant_pool` key + HP×5 + `Date` field |
| `CloudCode/process-matchmaking.js` | HP×5 + `Date` field |
| `CloudCode/get-queue-status.js` | Single-key per pool loop + `ok` flag |
| `CloudCode/dequeue-combat.js` | Cubre `instant_pool` + `matchmaking_pool` |

## Cómo usar esta nota en sesiones futuras

Cuando arranque una sesión nueva:
1. Leo este archivo primero (después del `CLAUDE.md`).
2. Borro lo de la sesión pasada y escribo qué estoy haciendo ahora.
3. Listo los 2-4 archivos del vault relevantes para esta sesión (no los leo todos).

Si el `Active Context` queda desactualizado (no se ha tocado en muchos días), tratarlo como **stale** — el código y los archivos del vault son autoritativos.

## Notas / pendientes que el usuario quiere recordar

- Furniture: retomar en **Fase 2 (Building mode)** — plan e implementación consolidados en [[10 - Furniture & Building]].
