---
tags: [memory-bank, script, genetics]
---

# BreedingController.cs

**Ruta:** `Systems/Breeding/BreedingController.cs`

**Responsabilidad:** Singleton dueño de servicios de cría en escena. Las `BreedingContainer` le piden la tabla de afinidad y el `AsyncBreedingService`. Botones dev: Fill Random Breeders, Breed (local), Breed Timer, Hatch Egg, Cancel All Eggs, Show Eggs. `BreedCreatures()` para cría local (síncrona). `GetEggs()` devuelve hembras incubando. Delega `StartBreedingAsync`, `HatchAsync`, `CancelBreedingAsync`, `CancelAllBreedingAsync` al `AsyncBreedingService`.

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[BreedingContainer]], [[AsyncBreedingService]], [[BreedingAffinityTableSO]], [[InheritanceOddsTableSO]], [[CreatureRegistrySO]], [[CreatureDatabaseSO]], [[BreedingService]]
