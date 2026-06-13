---
tags: [memory-bank, script, genetics]
---

# AsyncBreedingService.cs

**Ruta:** `Systems/Breeding/AsyncBreedingService.cs`

**Responsabilidad:** Breeding asíncrono server-side (UGS Cloud Code). Endpoints: `start-breeding`, `hatch-breeding`, `cancel-breeding`, `cancel-all-breeding`. `StartBreedingAsync` valida padres local, llama al server, marca ambos `BusyReason.Breeding` + cachea `BreedReadyAt`/`BreedPartnerID`. `HatchAsync` verifica reloj del server; en "ready" ejecuta `HatchLocally` (limpia estado, minta cría via `BreedingService.Breed`, emite `BreedingCompleted` + `RegistryChanged`). `CancelBreedingAsync` y `CancelAllBreedingAsync` limpian server + estado local. `ClearAllLocalBreeding` como finally en cancel-all. Energy cost por padre configurable.

**Vinculado a:** [[Index/02 - Genetics & Breeding]], [[Index/07 - Persistence & Identity]]

**Conexiones:** [[BreedingController]], [[BreedingService]], [[CreatureRegistrySO]], [[CreatureDatabaseSO]], [[InheritanceOddsTableSO]], [[CloudSyncService]]
