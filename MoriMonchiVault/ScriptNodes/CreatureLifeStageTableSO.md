---
tags: [script, genetics]
---

# CreatureLifeStageTableSO.cs

**Ruta:** `Data/Breeding/CreatureLifeStageTableSO.cs`

**Responsabilidad:** ScriptableObject que mapea edad en días (`AgeDays`) a etapa de vida visible (`LifeStage`). `GetStage(ageDays)` devuelve la etapa más alta cuyo threshold se haya alcanzado. `Label(stage)` retorna string en español. Display-only — nunca parte del string genético. Referenciado por `BreedingController` (único owner), leído por `NameTag` via `BreedingController.Instance.LifeStageTable`.

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[BreedingController]], [[NameTag]], [[Enums]] (LifeStage)
