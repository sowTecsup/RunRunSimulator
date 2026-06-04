---
tags: [memory-bank, active, session]
---

# 09 — Active Context

> Esta nota se actualiza CADA SESIÓN. Refleja qué estoy programando ahora mismo, qué archivos toco, y cuáles son los próximos pasos.

## Sesión actual

**Fecha**: 2026-06-04
**Foco**: **Corral de confinamiento IMPLEMENTADO** (base del breeding pen) + **refactor de `MoriMochiAgent`** + **rebake de NavMesh en `FurnitureService`**. Todo consolidado en [[06 - Player & World]] (corral + agente) y [[10 - Furniture & Building]] (rebake). Sesión previa: diseño del corral (propuestas A/B → decisión rebake/areaMask).

### Qué se hizo (esta sesión)

- **Corral (`MoriMochiContainer.cs`, World/)** ✅: mueble furniture 2x2 con `BoxCollider` trigger + `NavMeshModifier` (pinta piso = Area `BreedingRoom`). Aforo `[Min(1)] capacity`, censo `occupants` + `OccupantDNAs` (`[ShowInInspector]`). `OnTriggerEnter` (admite si `IsAirborne` / `BounceOut` con `Knock` si lleno) + `OnTriggerStay` (atrapa al soltar adentro). Solo se sale al sujetar (`Release`).
- **Confinamiento por `areaMask`** (no por costo — `SamplePosition` lo ignora): libres `AllAreas & ~(1<<BreedingRoom)` (rodean), confinados `1<<BreedingRoom` + roam en bounds. `breedingAreaName` = campo serializado con dropdown Odin de áreas. Varios corrales con un solo Area type.
- **Inmunidad al tackle**: un confinado ignora `Knock` → no lo empujan otros lanzados.
- **Refactor `MoriMochiAgent`** (in-place, sin State pattern GoF): `Held` → **`Carried`/`Thrown`** (elimina `heldByPlayer`); 3 helpers de handoff (`DetachToPhysics`/`ApplyThrownPhysics`/`RejoinNavMesh`) que deduplican; `NextRoamDestination`. Comportamiento idéntico. Inspector agrupado en tabs Odin (Movement/Physics/Presentation).
- **Rebake NavMesh (`FurnitureService`)**: botón Odin + auto-rebake en `Start`/`OnFurnitureReloaded`, diferido a fin de frame. Campo `navSurface`.

## Próximos pasos (retomar acá la próxima sesión)

**Corral / breeding pen:**
- **Setup de escena** (tuyo): Area `BreedingRoom` en Navigation → Areas; prefab corral (trigger + `MoriMochiContainer` + `NavMeshModifier`); asignar `navSurface` en `FurnitureService`; elegir `breedingAreaName` en el prefab del MoriMochi; rebakear tras colocar. Pasos en [[06 - Player & World]] / [[10 - Furniture & Building]].
- Pendiente conocido: **bloquear `TryLift` de un corral ocupado** (build mode).
- Futuro: enganchar **breeding** (juntar 2 → cría, usando `OccupantDNAs`) + persistencia de ocupantes.

**Furniture — siguiente:**
- Setup de escena del Build mode (layers Floor/Furniture, máscaras, ghost, Active Pieces, pivotes). Después: **Fase 3 (economía/tienda)** + persistencia del `FurnitureRegistrySO`.

**MoriMonchis** (sesiones previas)
- Setup de escena Etapa 2.5 pendiente (NavMesh bake + Areas + prefab + wiring). Pulsar **Populate Defaults** en `PersonalityProfileTable`.

## Archivos en juego en la sesión actual

| Archivo | Por qué |
|---------|---------|
| `World/MoriMochiContainer.cs` (NEW) | Corral: trigger, aforo, censo, admit/bounce, `OccupantDNAs` |
| `World/MoriMochiAgent.cs` | Refactor (Carried/Thrown + helpers) + confinamiento (`EnterConfinement`, `IsAirborne`, `breedingAreaName`, tackle-immune, tabs Odin) |
| `Systems/Furniture/FurnitureService.cs` | Rebake NavMesh (botón + auto, `navSurface`) |

## Cómo usar esta nota en sesiones futuras

Cuando arranque una sesión nueva:
1. Leo este archivo primero (después del `CLAUDE.md`).
2. Borro lo de la sesión pasada y escribo qué estoy haciendo ahora.
3. Listo los 2-4 archivos del vault relevantes para esta sesión (no los leo todos).

Si el `Active Context` queda desactualizado (no se ha tocado en muchos días), tratarlo como **stale** — el código y los archivos del vault son autoritativos.

## Notas / pendientes que el usuario quiere recordar

- Furniture: retomar en **Fase 2 (Building mode)** — plan e implementación consolidados en [[10 - Furniture & Building]].
