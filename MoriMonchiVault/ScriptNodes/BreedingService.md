---
tags: [script, genetics]
---

# BreedingService.cs

**Ruta:** `Systems/Breeding/BreedingService.cs`

**Responsabilidad:** Lógica local de cruce. Hereda partes desde el árbol genealógico (padres/abuelos/bisabuelos + mutación aleatoria), colores base via `ColorGenetics.Inherit(motherColor, fatherColor)` + color secundario derivado deterministico via `ColorGenetics.DeriveSecondary(childBase)`, `FurType` 50/50, stats base (Constitution/Attack/Speed) via `InheritStat()` que promedia padres y clampea a [StatMin..StatMax], personalidad no heredada via `CreatureGenerator.RandomPersonality()`. Valida género, muerte, busy state, `MaxBreedCount` (4). Valida herencia genealógica desde padres hasta bisabuelos; fallback a pool aleatorio si no hay ancestros.

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[CreatureDNA]], [[InheritanceOddsTableSO]], [[BreedingAffinityTableSO]], [[GameEvents]], [[BreedingContainer]], [[BreedingController]], [[CreatureRegistrySO]], [[CreatureDatabaseSO]], [[ColorGenetics]], [[FurType]], [[CreatureGenerator]], [[Enums]]
