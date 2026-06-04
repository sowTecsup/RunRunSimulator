---
tags: [memory-bank, active, session]
---

# 09 — Active Context

> Esta nota se actualiza CADA SESIÓN. Refleja qué estoy programando ahora mismo, qué archivos toco, y cuáles son los próximos pasos.

## Sesión actual

**Fecha**: 2026-06-03
**Foco**: **Diseño del corral de confinamiento** (base del breeding pen futuro). Solo teoría esta sesión — quedaron **dos propuestas (A vs B) documentadas en [[06 - Player & World]]** (sección "Corral de confinamiento — DISEÑO sin implementar"). Nada de código todavía. Sesión previa: Furniture Fase 2 — Building mode (código) + refactor DB furniture + fix CreatureGrid UITK ([[10 - Furniture & Building]]).

### Corral — qué quedó decidido y qué falta decidir

- **Decidido**: es un mueble furniture 2x2 (reúso total del sistema de furniture); entrada solo lanzándolo (trigger + ramas por `IsAirborne`/ocupante/intruso); aforo `capacity` configurable + rebote vía `Knock` cuando está lleno; salida solo al sujetarlo. Sin `GameEvents`/persistencia de ocupantes aún.
- **FALTA DECIDIR para retomar**: **Propuesta A** (todo NavMesh, sampling en bounds, evitación reactiva — confinamiento blando) **vs Propuesta B** (NavMeshObstacle carve + steering interno sin NavMesh — confinamiento duro, evitación proactiva). Detalle y tensión carve↔NavMesh-interior en [[06 - Player & World]].

### Qué se hizo (esta sesión) → todo consolidado en [[10 - Furniture & Building]]

- **Fix CreatureGrid UITK**: `CloudSyncService.OnSignedInComplete` dispara `GameEvents.RegistryReloaded` tras el load local (antes solo lo disparaba el cloud-pull → grilla vacía para jugador local/anon/offline/post-reset).
- **Refactor DB furniture**: `FurnitureDatabaseSO` → dict `[OdinSerialize]` (la key = id) con inline editor; `FurnitureDefinitionSO.Id` `[ReadOnly]` dictado por la DB; **Validate & Sync IDs** + **Populate from Buffer** (calca `PartDatabaseSO`).
- **Decisión de modelo**: grilla como base confirmada (libre sobre muebles grandes = fase futura). Memoria: `project_furniture_placement`.
- **Fase 2 Building mode (código ✅)**: action map `Building` (aditivo), `BuildingInputs` + `BuildModeController` (máquina Browsing/Placing/Editing/Deleting), hotbar 1-4, lift vía `TryLift`, dos máscaras (`floorMask`/`furnitureMask`) + `PlacedFurnitureMarker`, ghost verde/rojo. Edit = **E**.
- **`FurniturePivotAligner`** (helper de editor removible): bakea el pivote raíz al centro-base; runtime posiciona simple (sin auto-resolver — se probó y se descartó).

## Próximos pasos (retomar acá la próxima sesión)

**Furniture — siguiente:**
- **Setup de escena del Build mode** (tuyo): layers Floor/Furniture, asignar `floorMask`/`furnitureMask`, `ghostMaterial` transparente, lista Active Pieces, bakear pivotes con `FurniturePivotAligner`. Pasos en [[10 - Furniture & Building]].
- Probar place/edit/delete y afinar números (aimDistance, colores).
- Después: **Fase 3 (economía/tienda)** + **persistencia** (JSON+cloud del `FurnitureRegistrySO`).
- Pendiente conocido: selección por celda ancla (multi-celda apunta a la esquina); footprint no rectangular (L) = rectángulo contenedor por ahora.

**MoriMonchis** (de sesiones previas, sin avance esta sesión)
- Setup de escena Etapa 2.5 pendiente (NavMesh bake + 3 Areas + prefab + wiring spawner). Pulsar **Populate Defaults** en `PersonalityProfileTable`.

## Archivos en juego en la sesión actual

| Archivo | Por qué |
|---------|---------|
| `Systems/Furniture/BuildModeController.cs` · `Player/BuildingInputs.cs` | Build mode: máquina de estados + input map |
| `Systems/Furniture/FurnitureService.cs` | Hotbar (`activePieces`/`SelectPiece`) + `TryLift` + `TryPlace(def,…)` |
| `Systems/Furniture/FurnitureSpawner.cs` · `PlacedFurnitureMarker.cs` | Spawn + marker de celda ancla para selección |
| `Systems/Furniture/FurniturePivotAligner.cs` | Helper de editor de pivote (removible) |
| `Data/FurnitureDatabaseSO.cs` · `FurnitureDefinitionSO.cs` | DB dict-keyed + Id `[ReadOnly]` dictado por la DB |
| `Player/PlayerInputs.cs` · `PlayerController.cs` | `BuildToggled` (B) + estado `Building` |
| `Systems/Cloud/CloudSyncService.cs` | Fix: `RegistryReloaded` tras el load local |
| `InputSystem_Actions.inputactions` | Acción Build + action map `Building` |

## Cómo usar esta nota en sesiones futuras

Cuando arranque una sesión nueva:
1. Leo este archivo primero (después del `CLAUDE.md`).
2. Borro lo de la sesión pasada y escribo qué estoy haciendo ahora.
3. Listo los 2-4 archivos del vault relevantes para esta sesión (no los leo todos).

Si el `Active Context` queda desactualizado (no se ha tocado en muchos días), tratarlo como **stale** — el código y los archivos del vault son autoritativos.

## Notas / pendientes que el usuario quiere recordar

- Furniture: retomar en **Fase 2 (Building mode)** — plan e implementación consolidados en [[10 - Furniture & Building]].
