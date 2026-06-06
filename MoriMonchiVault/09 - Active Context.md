---
tags: [memory-bank, active, session]
---

# 09 — Active Context

> Esta nota se actualiza CADA SESIÓN. Refleja qué estoy programando ahora mismo, qué archivos toco, y cuáles son los próximos pasos.

## Sesión actual

**Fecha**: 2026-06-06
**Foco**: Refinamientos del Build Mode (validez física + snap al piso) + spawn de MoriMonchis en modo "lanzados" + mejoras de Needs (multi-slot en estaciones, `CreatureCondition`, needs priorizan sobre reacción al jugador, eliminación del speed degradado).

### Qué se hizo (esta sesión)

**Build Mode — dos reglas nuevas de validez de colocación:**
- **`PlacementGrid.TrySampleFloor(anchor, fp, rot, out y, out flat)`**: raycast vertical al piso real bajo el footprint; devuelve la Y del suelo y si la normal es plana (< `maxSlopeAngle`). Nuevos campos: `floorMask`, `maxSlopeAngle` (deg), `floorProbeHeight`. El grid sigue siendo un plano lógico XZ; la Y del spawn/preview viene del terreno real → funciona en terreno irregular.
- **`BuildModeController.obstacleMask`**: nuevo campo. `OverlapsObstacle()` hace un `Physics.CheckBox` orientado al footprint (XZ del grid + altura del mesh del ghost) contra esa layer → rojo si choca con muros/escenografía no registrada en el grid. El ghost ya tiene colliders apagados y la pieza levantada está despawneada, sin auto-detección.
- **`PlacementValid()`**: fuente única de verdad = `CanPlace` + `floorFlat` + `!OverlapsObstacle`. La usa el tint, `OnPin` y `OnConfirm` (verde/rojo, fijado y guardado siempre coinciden). Pendiente inclinado → siempre inválido.
- **`FurnitureSpawner`**: llama `TrySampleFloor` al respawnear → la Y se re-lee del terreno real (Opción B: no se guarda, el terreno es la fuente de verdad).

**MoriMochiSpawner — modo "Launched":**
- Enum `SpawnMode { Placed, Launched }` con `[EnumToggleButtons]`.
- Tab Odin **"Placed (drop)"**: `spawnArea` + `spawnRadius` (comportamiento previo intacto).
- Tab Odin **"Launched (shoot out)"**: `launchPoint` (GameObject de origen), `launchForce` (rango min/max, `[MinMaxSlider]`), `launchUpBias` (arco vertical).
- En modo `Launched`: instancia en `launchPoint` y llama `agent.Launch(RandomLaunchImpulse())`.

**MoriMochiAgent — método `Launch(impulse)`:**
- Reutiliza el pipeline de física de `Knock` (DetachToPhysics + ApplyThrownPhysics + estado `Thrown` → bounce → settle → get-up → EnterRoaming al área preferida). **Sin penalización de affect** (nacer no es estresante). Sin chequeo de confinamiento (nunca está enjaulado al spawnear).

**NeedStation — capacidad multi-slot:**
- `usePoint` (singular) → `List<Transform> usePoints`. Capacidad = cantidad de puntos (sin puntos → 1 slot implícito en el transform).
- `TryReserve(agent, from, areaMask, sampleRadius, out usePos)`: reserva el slot libre más cercano y alcanzable (snap al NavMesh en el areaMask del agente); re-entrante (si ya tenía slot, lo conserva). Devuelve la posición donde pararse. `false` si lleno o ningún slot snaps.
- Gizmos: esfera+línea por slot, coloreados por need (verde/azul/rosa). En Play: slot ocupado → **rojo**.
- El agente llama `TryReserve` en `TryEnterNeedSeeking` (la reserva y la elección de punto son la misma operación atómica).

**`CreatureCondition` (nuevo enum en `Enums.cs`):**
- `Healthy` / `InNeed` (Energy o Affect crítica) / `Sick` (Health crítica — emergencia de supervivencia).
- Propiedad **calculada** en `MoriMochiAgent.Condition` (derivada de los thresholds, nunca guardada → siempre en sync). Visible en la tab Needs con `[EnumToggleButtons, ReadOnly]`.

**Needs priorizan sobre reacción al jugador:**
- `ReactIfPlayerNear` refactorizado: flee por estrés (Affect crítico) siempre activo. Reacciones amistosas (follow/approach/retreat) **solo si `Condition == Healthy`**. Un MoriMochi hambriento/cansado ignora al jugador.
- `BeginReaction(reaction)` extraído como helper.

**Eliminación del speed degradado:**
- `degradedSpeedMultiplier` + `ApplyDegradedSpeed()` eliminados. Un MoriMochi con need crítica se mueve a velocidad normal y puede alcanzar su estación. La "penalización" por no tener estación es solo la need sin satisfacer + ignorar al jugador.

## Próximos pasos (retomar acá la próxima sesión)

**Setup de escena (tuyo — código listo):**
- `Feeder`/`RestZone`/`PlayZone`: agregar hijos vacíos como use points en los prefabs (uno por lado); los gizmos de color muestran slots y ocupación en Play.
- `PlacementGrid`: asignar `Floor Mask` (layer del piso/terreno) + subir el transform del grid por encima del piso más alto + ajustar `Max Slope Angle`.
- `BuildModeController`: asignar `Obstacle Mask` (layers de muros/escenografía fija que bloquean por colisión física).
- `MoriMochiSpawner`: si usás `Launched`, asignar `launchPoint` (ligeramente sobre el piso).

**Pendientes anteriores:**
- Bloquear `TryLift` de un corral ocupado (en `BuildModeController`/`FurnitureService`).
- Cablear `FlushToCloud()` en el logout de `CloudSyncService`.
- Futuro: petting directo (E sobre criatura); recursos consumibles en estaciones; muerte por inanición (Health → 0); decay offline por timestamp.

## Archivos en juego en la sesión actual

| Archivo | Por qué |
|---------|---------|
| `Systems/Furniture/PlacementGrid.cs` | `TrySampleFloor` + `floorMask`/`maxSlopeAngle`/`floorProbeHeight` |
| `Systems/Furniture/BuildModeController.cs` | `obstacleMask` + `OverlapsObstacle` + `PlacementValid` + snap Y |
| `Systems/Furniture/FurnitureSpawner.cs` | Snap Y con `TrySampleFloor` en `SpawnOne` |
| `World/MoriMochiSpawner.cs` | SpawnMode enum + tabs Placed/Launched + `RandomLaunchImpulse` |
| `World/MoriMochiAgent.cs` | `Launch()` + readout de needs + `Condition` + `ReactIfPlayerNear` refactorizado + eliminación de degradado |
| `World/NeedStation.cs` | Multi-slot (`usePoints` list + `occupants[]`) + gizmos con ocupación |
| `Core/Enums.cs` | Nuevo enum `CreatureCondition` |

## Cómo usar esta nota en sesiones futuras

Cuando arranque una sesión nueva:
1. Leo este archivo primero (después del `CLAUDE.md`).
2. Borro lo de la sesión pasada y escribo qué estoy haciendo ahora.
3. Listo los 2-4 archivos del vault relevantes para esta sesión (no los leo todos).

Si el `Active Context` queda desactualizado (no se ha tocado en muchos días), tratarlo como **stale** — el código y los archivos del vault son autoritativos.

## Notas / pendientes que el usuario quiere recordar

- Furniture: retomar en **Fase 2 (Building mode)** — plan e implementación consolidados en [[10 - Furniture & Building]].
