---
tags: [memory-bank, active, session]
---

# 09 — Active Context

> Esta nota se actualiza CADA SESIÓN. Refleja qué estoy programando ahora mismo, qué archivos toco, y cuáles son los próximos pasos.

## Sesión actual

**Fecha**: 2026-06-04
**Foco**: **Sistema de Necesidades (Needs) IMPLEMENTADO** — 3 stats (Health/Energy/Affect) en `CreatureDNA.Needs`, estaciones de mundo (Feeder/RestZone/PlayZone), registry, FSM del agente (SeekingNeed/UsingStation + degradado) y persistencia diferida anti-saturación. Detalle en [[06 - Player & World]] (sistema) y [[07 - Persistence & Identity]] (flush). Sesión previa: corral de confinamiento + refactor del agente + rebake.

### Qué se hizo (esta sesión)

- **`NeedsState`** anidado en `CreatureDNA.Needs` (Opción A — DNA ya es el record persistido → cero plomería). Mutadores clampeados + `SpendEnergy`/`Restore`/`Get`.
- **Estaciones**: `NeedStation` (abstracta) + `Feeder`/`RestZone`/`PlayZone` (auto-registro, `usePoint`, recarga hasta 100, lock de un usuario). **`NeedStationRegistry`** estático (`GetClosest`).
- **`MoriMochiAgent`**: tab Odin **Needs** (decay, umbrales configurables, penalizaciones de afecto, degradado); `TickNeeds` en memoria sin eventos; estados `SeekingNeed`/`UsingStation`; degradado (lento sin energía / huye estresado); interrupción por grab (`ReleaseStation`); hooks de afecto en throw/knock/colisión.
- **Persistencia diferida**: `GameManager.FlushToCloud()` (público) + `OnApplicationPause(true)` + quit. Los needs **NO** disparan `OnRegistryChanged` (anti-saturación). Viajan en el flush porque viven en `CreatureDNA`.
- **Endpoints de energía**: `CombatManagerSO.EnergyCostToQueue` (gastado en `AsyncCombatService`) + `AsyncBreedingService.energyCostPerParent`.

## Próximos pasos (retomar acá la próxima sesión)

**Needs — siguiente:**
- **Setup de escena** (tuyo): `Feeder`/`RestZone`/`PlayZone` en prefabs de furniture (hijo `usePoint`, sobre NavMesh); tunear tab Needs del agente + `EnergyCostToQueue` + `energyCostPerParent`.
- **Cablear `FlushToCloud()` en el logout** de `CloudSyncService` (quedó público, sin enganchar).
- Futuro: petting directo (E sobre la criatura); recursos consumibles en estaciones; muerte por inanición; decay offline (timestamp al cargar).

**Corral / breeding pen** (sesión previa, sin avance):
- Setup de escena (Area `BreedingRoom` + prefab corral + `navSurface` + `breedingAreaName` + rebake). Pendiente: bloquear `TryLift` de corral ocupado. Futuro: breeding con `OccupantDNAs` + persistencia de ocupantes.

**Furniture / MoriMonchis** (previos): setup Build mode + Fase 3 economía; setup escena Etapa 2.5 (NavMesh + Areas + prefab + Populate Defaults).

## Archivos en juego en la sesión actual

| Archivo | Por qué |
|---------|---------|
| `Data/NeedsState.cs` (NEW) | 3 stats clampeados + endpoints |
| `World/NeedStation.cs` (NEW) + `Feeder`/`RestZone`/`PlayZone` (NEW) | Estaciones de recarga (furniture) |
| `World/NeedStationRegistry.cs` (NEW) | Índice estático `GetClosest` |
| `World/MoriMochiAgent.cs` | Tab Needs + decay + SeekingNeed/UsingStation + degradado + hooks + grab interrupt |
| `Data/CreatureDNA.cs` · `Core/Enums.cs` | Campo `Needs` · enum `NeedType` |
| `Core/GameManager.cs` | `FlushToCloud` + `OnApplicationPause` |
| `Data/CombatManagerSO.cs` · `Systems/Combat/AsyncCombatService.cs` | Costo de energía al encolar |
| `Systems/Breeding/AsyncBreedingService.cs` | Costo de energía por padre |

## Cómo usar esta nota en sesiones futuras

Cuando arranque una sesión nueva:
1. Leo este archivo primero (después del `CLAUDE.md`).
2. Borro lo de la sesión pasada y escribo qué estoy haciendo ahora.
3. Listo los 2-4 archivos del vault relevantes para esta sesión (no los leo todos).

Si el `Active Context` queda desactualizado (no se ha tocado en muchos días), tratarlo como **stale** — el código y los archivos del vault son autoritativos.

## Notas / pendientes que el usuario quiere recordar

- Furniture: retomar en **Fase 2 (Building mode)** — plan e implementación consolidados en [[10 - Furniture & Building]].
