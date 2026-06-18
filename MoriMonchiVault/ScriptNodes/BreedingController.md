---
tags: [memory-bank, script, genetics]
---

# BreedingController.cs

**Ruta:** `Systems/Breeding/BreedingController.cs`

**Responsabilidad:** Singleton dueño de servicios de cría en escena. Las `BreedingContainer` le piden la tabla de afinidad, el `AsyncBreedingService` y la `CreatureLifeStageTableSO`. Botones dev: Fill Random Breeders, Breed (local), Breed Timer, Hatch Egg, Cancel All Eggs, Show Eggs. `BreedCreatures()` para cría local (síncrona). `GetEggs()` devuelve hembras incubando. Delega `StartBreedingAsync`, `HatchAsync`, `CancelBreedingAsync`, `CancelAllBreedingAsync` al `AsyncBreedingService`. Propiedad pública `LifeStageTable` expone la tabla de etapas de vida para que `NameTag` lea los thresholds.

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[BreedingContainer]], [[AsyncBreedingService]], [[BreedingAffinityTableSO]], [[InheritanceOddsTableSO]], [[CreatureRegistrySO]], [[CreatureDatabaseSO]], [[BreedingService]]
