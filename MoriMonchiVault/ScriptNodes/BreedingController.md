---
tags: [script, genetics]
---

# BreedingController.cs

**Ruta:** `Systems/Breeding/BreedingController.cs`

**Responsabilidad:** Apex del sistema de cría (domain owner). Singleton: `static Instance`. Posee `inheritanceOddsTable`, `affinityTable`, `lifeStageTable` y `asyncBreedingService` (todos serializados). Getters públicos: `InheritanceOdds`, `LifeStageTable`. Public API: `GetAffinity()`, `BreedCreatures()` (devuelve ID hijo), wrappers async (StartBreedingAsync, HatchAsync, CancelBreedingAsync, CancelAllBreedingAsync). Las BreedingContainer piden servicios vía Instance. Resuelve registry y database de GameManager.Instance en Awake.

**Vinculado a:** [[Index/02 - Breeding]]

**Conexiones:** [[AsyncBreedingService]], [[BreedingService]], [[BreedingAffinityTableSO]], [[InheritanceOddsTableSO]], [[CreatureRegistrySO]], [[CreatureDatabaseSO]], [[CreatureLifeStageTableSO]], [[GameManager]], [[BreedingDevConsole]], [[BreedingContainer]]
