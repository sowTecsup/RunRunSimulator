---
tags: [memory-bank, active, session]
---

# 09 — Active Context

> Esta nota se actualiza CADA SESIÓN. Refleja qué estoy programando ahora mismo, qué archivos toco, y cuáles son los próximos pasos.

**Sesión Actual:** 2026-06-11 (Sesión 7)
**Foco:** Fix stutter ragdoll→agente · `StoreContainer` · `BreedingContainer` + `BreedingAffinityTableSO`.

## Archivos Tocados
- `World/MoriMochiAgent.cs`: fix stutter get-up (lerp posición + rotación; `Warp` diferido al final de `TickRecovering`).
- `World/MoriMochiContainer.cs`: `Awake` → `protected virtual` para herencia limpia.
- `World/StoreContainer.cs` *(nuevo)*: vitrina que restaura needs de ocupantes a `restoreRate/s`.
- `World/BreedingContainer.cs` *(nuevo)*: corral con timer de dado + filtro de elegibilidad + breed híbrido (async/local).
- `Data/BreedingAffinityTableSO.cs` *(nuevo)*: matriz 6×6 de afinidad por personalidad, singleton `Current`, botón *Seed Defaults*.

## Siguiente Sesión (Goal)
**BreedingContainer — capa visual + singleton BreedingController**
1. `BreedingController` como singleton para que `BreedingContainer` resuelva `AsyncBreedingService` y `BreedingAffinityTableSO` desde él (sin doble asignación en inspector).
2. Cartelito / UI diegética encima de la pareja durante el apereamiento y al llegar al momento de hatchear la cría.
