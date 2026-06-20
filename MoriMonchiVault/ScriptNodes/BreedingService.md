---
tags: [memory-bank, script, genetics]
---

# BreedingService.cs

**Ruta:** `Systems/Breeding/BreedingService.cs`

**Responsabilidad:** Lógica local de cruce. Hereda partes desde el árbol genealógico (padres/abuelos/bisabuelos + mutación aleatoria), colores via `ColorGenetics.Inherit()`, `FurType` 50/50, stats con delta aleatorio, personalidad no heredada via `CreatureGenerator.RandomPersonality()`. Valida género, muerte, busy state, `MaxBreedCount` (4). Dispara `GameEvents.OnBreedingCompleted`.

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[CreatureDNA]], [[InheritanceOddsTableSO]], [[BreedingAffinityTableSO]], [[GameEvents]], [[BreedingContainer]], [[BreedingController]], [[CreatureRegistrySO]], [[CreatureDatabaseSO]], [[ColorGenetics]], [[FurType]], [[CreatureGenerator]]
