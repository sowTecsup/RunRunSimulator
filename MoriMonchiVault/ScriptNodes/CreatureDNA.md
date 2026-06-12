---
tags: [memory-bank, script, genetics]
---

# CreatureDNA.cs

**Ruta:** `Data/CreatureDNA.cs`

**Responsabilidad:** Modelo central: string genético (part IDs + color), identidad (UniqueID), linaje, género, personalidad, stats base, combat history, needs, busy state. `ToStringID()`/`FromID()` son el contrato de red.

**Vinculado a:** [[Index/02 - Genetics & Breeding]]

**Conexiones:** [[CreatureRegistrySO]], [[CreatureStats]], [[NeedsState]], [[CombatRecord]], [[MoriMochiAgent]], [[BreedingService]], [[PartDatabaseSO]]
