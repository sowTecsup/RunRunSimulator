---
tags: [memory-bank, active, session]
---

# 09 - Active Context

**Session:** 2026-06-13 (Session 8)
**Focus:** BreedingContainer feedback + visual cortejo. BreedingController como única fuente de servicios. Cancel async server-side. NameTag corral. Recuperación de huevos huérfanos.

**Files Touched:**
- `World/BreedingContainer.cs`: implementa `IInteractable` (tap E → hatch); restore pasivo de needs; `ForceRoll` (botón Odin) que ignora cooldown; `TryRollPair(verbose, ignoreCooldown)` ahora es `async` y reporta éxito SOLO si el server inició la incubación (no miente: si el server rechaza con `already_breeding` lo dice y no quema cooldown); `ManageCourtship` posa la pareja incubando enfrentada en slots fijos (`courtGap`); `CancelBreeding` al retirar del corral (limpia local + llama server); `ReclaimBreedingOccupants` reengancha al corral los breeders mid-incubación tras recargar escena; `PairDiagnostics` + `lastRollInfo` para debug en inspector
- `World/MoriMochiContainer.cs`: `OccupantInfo` ahora {Name, Gender, Personality} (sin dump de DNA); `Claim()` compartido (Admit + reclaim); `Release()` public virtual; warning de NavMesh si confinamiento falla
- `World/MoriMochiAgent.cs`: estado `Courting` + `EnterCourtship`/`ExitCourtship`/`IsCourting`/`IsPenned`
- `World/NameTag.cs`: layout de corral (glifo género ♂/♀ + nombre + personalidad; ❤ + countdown si incubando) vs layout libre (status/intent/petHint)
- `Systems/Breeding/BreedingController.cs`: singleton `Instance`; dueño de `AsyncBreedingService` + `BreedingAffinityTableSO`; wrappers `GetAffinity`/`StartBreedingAsync`/`HatchAsync`/`CancelBreedingAsync`/`CancelAllBreedingAsync`; botón "Cancel All Eggs" en BoxGroup Breed Timer
- `Systems/Breeding/AsyncBreedingService.cs`: `CancelBreedingAsync(mother,father)` (endpoint cancel-breeding) y `CancelAllBreedingAsync()` (endpoint cancel-all-breeding, vacía la cola entera + `ClearAllLocalBreeding`)

**Cloud Code (nuevos, desplegar):**
- `CloudCode/cancel-breeding.js`: borra un huevo puntual (motherId+fatherId)
- `CloudCode/cancel-all-breeding.js`: vacía la key `breeding_eggs_<playerId>` entera (recupera huérfanos que el cliente no rastrea)

**Quirk técnico resuelto:** el bug de "¡Emparejados! pero no pasa nada" era desync con el server — un cancel cuyo endpoint no estaba desplegado dejaba el huevo vivo en el server; al reusar esos IDs, `start-breeding` devolvía `already_breeding` y el `BusyState` local nunca se seteaba. Fix doble: (1) `TryRollPair` async ahora verifica `BusyState` real antes de cantar éxito; (2) botón Cancel All Eggs + endpoint para limpiar huérfanos.

**Next Session Goal (diseño + testing):**
- **Corral de origen**: las parejas deben recargar en el MISMO corral donde breedearon. Crear data que empareje `furnitureId` ↔ MoriMonchis (hoy `ReclaimBreedingOccupants` reengancha a cualquier corral disponible, no al de origen).
- **Bug en testing (no reproducido)**: error "unknown" raro, posiblemente al pulsar "Cancel All Eggs" repetidas veces (sospecha: `async void` sin guard re-entrante → llamadas solapadas a CallEndpointAsync). Intentar reproducir; si confirma, agregar guard (estilo `isHatching`).
- Verificar visualmente la pose de cortejo y el reclaim tras recarga de escena.
