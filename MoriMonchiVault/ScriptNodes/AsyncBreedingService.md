---
tags: [script, genetics]
---

# AsyncBreedingService.cs

**Ruta:** `Systems/Breeding/AsyncBreedingService.cs`

**Responsabilidad:** Orquesta breeding async server-side (Cloud Code). Cloud Code scripts: "start-breeding", "hatch-breeding", "cancel-breeding", "cancel-all-breeding". `StartBreedingAsync` valida padres local, llama CloudEndpoint, marca ambos `BusyReason.Breeding` + cachea `BreedReadyAt`/`BreedPartnerID`. `HatchAsync` verifica reloj del servidor; en "ready" ejecuta `HatchLocally` (limpia estado, minta cría via `BreedingService.Breed`, emite `BreedingCompleted` + `RegistryChanged`). `CancelBreedingAsync` y `CancelAllBreedingAsync` limpian servidor + local. Resuelve registry y database de GameManager.Instance. Obtiene odds vía BreedingController.Instance.InheritanceOdds.

**Vinculado a:** [[Index/02 - Genetics & Breeding]], [[Index/07 - Persistence & Identity]]

**Conexiones:** [[CloudEndpoint]], [[BreedingController]], [[BreedingService]], [[CreatureRegistrySO]], [[CreatureDatabaseSO]]
